# Grocery Compare App — Architecture & Delivery Plan

## 1. Requirements Recap

- Compare grocery prices across Pak'nSave, Woolworths NZ, and New World.
- Users log in with a Google account.
- The app maintains a database of all franchise store locations, refreshed periodically.
- Users pick a personal subset of stores (any mix of franchises/locations) and can change it anytime.
- Users search for grocery items with type-ahead suggestions and add them to a shopping list.
- Each shopping list item shows the price at every one of the user's selected stores.
- The app tells the user which single store is cheapest for the whole list.
- The app also suggests a cheaper alternative: splitting the list across multiple stores.

## 2. Tech Stack

| Layer | Choice | Notes |
|---|---|---|
| Frontend | React (TypeScript) + Vite | SPA, hosted on Azure Static Web Apps |
| Backend API | ASP.NET Core Web API (.NET 8/9) | REST, layered architecture |
| Background/ETL | .NET Worker Service (Azure Container Apps Jobs or Azure Functions timer triggers) | Runs the retailer scrapers on a schedule |
| Database | Azure SQL Database (SQL Server) | Relational fit for stores/prices/lists; EF Core as ORM |
| Auth | Direct Google Sign-In | SPA uses Google Identity Services to get a Google ID token; API validates it with `Google.Apis.Auth` and mints its own short-lived JWT |
| Search/autocomplete | SQL Server full-text/trigram-style search initially; Azure AI Search if catalog grows large | Start simple, upgrade only if needed |
| Hosting | Azure (App Service or Container Apps for API, Static Web Apps for React, Azure SQL, Container Apps Jobs/Functions for scraper, Key Vault, Application Insights) | |
| CI/CD | GitHub Actions → Azure | Separate pipelines for API, SPA, scraper |

This stack directly answers your ".NET REST API + React" framing: the React SPA talks only to the ASP.NET Core API over HTTPS/JSON; the API is the sole owner of the database and the only thing the scraper writes through (or writes to the DB directly under its own service identity, depending on Phase — see §7).

## 3. High-Level Architecture

```
┌─────────────────┐        HTTPS/JSON        ┌──────────────────────────┐
│  React SPA       │ ───────────────────────▶ │  ASP.NET Core Web API     │
│ (Azure Static     │ ◀─────────────────────── │  (Azure App Service /     │
│  Web Apps)        │   App-issued JWT          │  Container Apps)          │
└─────────────────┘                           └───────────┬──────────────┘
        │                                                    │ EF Core
        │ Google Identity Services                           ▼
        ▼  (Google ID token)                        ┌──────────────────┐
┌─────────────────┐                                │  Azure SQL DB     │
│  Google OAuth     │──validated by API──────────▶  │ (Stores, Prices,  │
│  (Sign-In)         │  (Google.Apis.Auth)           │ Users, Lists)     │
└─────────────────┘                                └──────────────────┘
                                                            ▲
                                                            │ writes stores + prices
                                                  ┌──────────────────────┐
                                                  │ Scraper/ETL Worker    │
                                                  │ (Container Apps Job   │
                                                  │  or Azure Functions,  │
                                                  │  timer-triggered)     │
                                                  │  - PaknSave adapter   │
                                                  │  - New World adapter  │
                                                  │  - Woolworths adapter │
                                                  └──────────────────────┘
```

Each retailer gets its own adapter behind a common `IPriceSource` interface, so a retailer changing its site structure only breaks one adapter, and swapping in an official API later (if one ever appears) is a drop-in replacement rather than a rewrite.

## 4. Data Model (core entities)

- **User** — Id, GoogleSubjectId, Email, DisplayName, CreatedAt.
- **Franchise** — Id, Name (Pak'nSave / Woolworths / New World).
- **Store** — Id, FranchiseId, Name, Address, Suburb, Region, Latitude/Longitude, ExternalStoreCode (the retailer's own store id, used by the scraper), IsActive, LastSyncedAt.
- **UserStoreSelection** — UserId, StoreId, CreatedAt (many-to-many; this is how a user's chosen subset of stores, across any mix of franchises/locations, is stored).
- **GroceryItem** — Id, CanonicalName, Brand, Size/Unit, Barcode (nullable), Category. This is the app's own canonical product catalog, since each retailer names/codes the "same" product differently.
- **ItemAlias** — GroceryItemId, StoreId or FranchiseId, ExternalProductCode, ExternalName. Maps a canonical item to each retailer's own SKU/search result, and is what the matching/reconciliation logic (§7) populates.
- **Price** — ItemAliasId, StoreId, Price, CapturedAt, IsOnSale. Kept as a time-stamped row (not just overwritten) so price history is available later for free; the MVP itself only ever queries the latest row per item/store and doesn't build any trend/history features yet.
- **ShoppingList** — Id, UserId, Name, CreatedAt.
- **ShoppingListItem** — ShoppingListId, GroceryItemId, Quantity.
- **ScrapeRun** — Id, FranchiseId, StartedAt, FinishedAt, Status, ItemsUpdated, ErrorSummary — operational log for the ETL job, useful for debugging data freshness.

Indexes worth calling out up front: a trigram/full-text index on `GroceryItem.CanonicalName` (and `ItemAlias.ExternalName`) for the type-ahead search, and a composite index on `Price(ItemAliasId, StoreId, CapturedAt DESC)` since "current price per item per store" is the hottest query in the app.

## 5. API Design (representative endpoints)

Auth
- `POST /api/auth/google` — accepts the Google ID token from the SPA, validates it, upserts the user, returns the app's own JWT.
- `GET /api/auth/me` — current user profile.

Stores
- `GET /api/franchises` — list franchises.
- `GET /api/stores?franchise=&region=` — browse/search all known stores.
- `GET /api/users/me/stores` — the user's selected stores.
- `PUT /api/users/me/stores` — replace the user's selected store set (supports "change anytime").

Items & search
- `GET /api/items/search?q=` — type-ahead suggestions (debounced on the client, e.g. after 2–3 characters).

Shopping lists
- `GET /api/lists` / `POST /api/lists` — manage lists.
- `POST /api/lists/{id}/items` / `DELETE /api/lists/{id}/items/{itemId}` — add/remove items.
- `GET /api/lists/{id}/prices` — for every item in the list, the price at each of the user's selected stores (the core comparison view).
- `GET /api/lists/{id}/recommendation` — returns both: (a) the single cheapest store for the whole list and its total, and (b) the optimal split-by-store combination and its total + savings vs. the single-store option.

Admin/ops (internal, not end-user facing)
- `POST /api/admin/stores/sync` — manually trigger a store-directory refresh.
- `GET /api/admin/scrape-runs` — observability into the scraper.

## 6. Authentication Flow (decided: direct Google Sign-In)

1. React SPA uses Google Identity Services (the "Sign in with Google" button/One Tap) to authenticate the user directly with Google and obtain a Google ID token — no Azure identity tenant involved.
2. SPA sends that ID token to `POST /api/auth/google`.
3. The API validates the token server-side with `Google.Apis.Auth` (checks signature, audience/client ID, expiry), upserts a `User` row keyed by the token's stable Google subject claim, and mints its own short-lived app JWT (plus a refresh token).
4. SPA stores the app JWT and sends it as `Authorization: Bearer <token>` on subsequent API calls; the API validates its own JWTs with standard ASP.NET Core JWT bearer middleware.

This keeps things simple for a small app: no Entra tenant to provision or pay for, and the API owns its own session lifecycle end to end. The trade-off — you own refresh-token issuance/rotation and revocation yourself rather than getting it for free from a managed identity platform — is a reasonable one at this scale and can be revisited if the app ever needs enterprise-grade session management.

## 7. Price Data Pipeline (the hard part)

None of the three retailers publish a public price API. Community projects (e.g. an open-source Flask scraper covering Countdown/Woolworths NZ, New World, and Pak'nSave — [github.com/jesmcc/GroceryCompare](https://github.com/jesmcc/GroceryCompare)) confirm this is workable by calling each retailer's own site/app JSON endpoints, but it's inherently fragile: Foodstuffs (which runs both Pak'nSave and New World) recently removed price-sort from its websites entirely — a live example of how these sites can change underneath a scraper with no notice ([Consumer NZ coverage](https://www.consumer.org.nz/articles/consumer-nz-questions-foodstuffs-removal-of-online-price-sorting-tool)). Design implications:

- **Adapter pattern**: one `IPriceSource` implementation per franchise, isolating breakage. **Woolworths NZ is the confirmed first adapter to build** (Phase 2), on the strength of the community precedent above; Pak'nSave and New World follow in Phase 3 and likely share code since both are Foodstuffs.
- **Scheduled, rate-limited runs**: nightly (or a few times a day) rather than on-demand, with backoff/retry and per-run logging (`ScrapeRun` table) so failures are visible rather than silently stale data.
- **Resilience over completeness**: if an adapter fails, serve the last successfully captured prices rather than erroring the whole app, and surface a "prices as of [date]" indicator to the user.
- **Item matching/reconciliation**: the trickiest non-obvious piece. Each retailer names and codes products differently, so a step is needed (fuzzy name + brand + size matching, with manual override capability) to populate `ItemAlias` and link retailer SKUs to your canonical `GroceryItem` catalog. Expect this to need ongoing curation, not a one-time script.
- **Legal/ToS posture**: since this is a small app for personal use among a few friends rather than a public/commercial product, legal/ToS risk is deprioritized for now — noted here for the record rather than acted on. Still worth keeping scrape traffic light out of basic good-citizenship (reasonable schedule, no hammering endpoints), but no formal ToS review is planned at this stage. Revisit if the app ever grows beyond that small-scale use.
- **Store directory sync**: same adapter idea, and built in **Phase 1** for all three franchises (see roadmap) rather than deferred — a job per franchise that pulls the store locator and populates/refreshes the `Store` table. This is separate from (and much less frequent than) the price sync, which starts in Phase 2.

## 8. Cheapest-Store / Split-Savings Logic

- **Single cheapest store**: for the user's selected stores, sum `price × quantity` for every list item at each store (only counting stores that stock every item, or clearly flagging missing items); the lowest total wins.
- **Split-by-store suggestion**: for each item, pick the cheapest of the user's selected stores; sum those per-store subtotals; compare the grand total (plus, presumably, a caveat about the cost/hassle of visiting multiple stores) against the single-store total, and only surface the suggestion when it beats the single-store option by some minimum margin (a configurable threshold, e.g. don't bother suggesting a split to save $0.50 across three stores).
- Both calculations run server-side in the API (`GET /api/lists/{id}/recommendation`) so the logic lives in one place and is unit-testable independent of the UI.

## 9. Azure Resource Layout

- Azure Static Web Apps — React SPA hosting + CDN.
- Azure App Service (or Container Apps) — ASP.NET Core API.
- Azure Container Apps Jobs (or Azure Functions, timer-triggered) — scraper/ETL, store-directory sync.
- Azure SQL Database — primary data store.
- No Azure identity tenant needed — auth is direct Google Sign-In, validated in the API itself (see §6).
- Azure Key Vault — connection strings, Google client ID/secret, JWT signing key.
- Application Insights — API + scraper telemetry, especially scrape success/failure rates.
- GitHub Actions — build/test/deploy pipelines per component.

## 10. Suggested Repo/Solution Structure

```
grocery-compare/
├── src/
│   ├── GroceryCompare.Api/            # ASP.NET Core Web API
│   ├── GroceryCompare.Domain/         # Entities, business logic (recommendation engine)
│   ├── GroceryCompare.Infrastructure/ # EF Core, DbContext, migrations
│   ├── GroceryCompare.Scraper/        # Worker service, one adapter per franchise
│   └── GroceryCompare.Web/            # React app
├── tests/
│   ├── GroceryCompare.Domain.Tests/
│   └── GroceryCompare.Api.Tests/
└── infra/                             # Bicep/Terraform for Azure resources
```

## 11. Phased Roadmap

**Phase 0 — Foundations (1–2 weeks)**
Repo scaffolding, Azure resource provisioning, CI/CD skeleton, auth wired end-to-end (login works, no app features yet).

**Phase 1 — MVP**
Store directory populated by **scraping all three franchises' store locators** (Pak'nSave, Woolworths NZ, New World) — one `IPriceSource`-style store-sync adapter per franchise, run manually/on-demand at this stage rather than fully scheduled; user can select stores from that real data. Grocery item catalog still seeded manually/via CSV for a limited set of common items, with manually-entered current prices (real item-level price scraping is still Phase 2/3 — see §7). Shopping list CRUD; single-cheapest-store recommendation, using only the current price (no history features yet). Goal: prove the full user journey with real store data, before investing in the harder item-price scraping.

**Phase 2 — Real price data, one franchise**
Build the first item-price scraper adapter for **Woolworths NZ** (confirmed starting point), scheduled sync job, `ScrapeRun` logging, item-matching workflow. Add the split-by-store suggestion logic.

**Phase 3 — Full price data, autocomplete, polish**
Add item-price scraper adapters for the remaining two franchises (Pak'nSave, New World — likely shareable code since both are Foodstuffs). Type-ahead search (upgrade to Azure AI Search only if plain SQL search proves too slow). Scheduled (not just manual) store-directory auto-sync. Basic admin visibility into scrape health.

**Phase 4 — Hardening & scale**
Price history/trends (data model already supports it — deferred by design until this phase), performance tuning, monitoring/alerting on scrape failures, and revisit the legal/ToS posture if the app ever grows beyond a small friends-and-family user base.

## 12. Decisions Confirmed

- **Auth**: direct Google Sign-In (§6) — no Entra/Azure identity tenant.
- **First franchise to scrape**: Woolworths NZ (Phase 2), with store-directory scraping for all three franchises brought forward into Phase 1.
- **Legal/ToS risk**: deprioritized for now — this is a small app for personal use among a few friends, not a public/commercial product. Revisit if that changes.
- **Price history**: schema keeps timestamped price rows so history is available later for free, but no history/trend features are built until Phase 4 — MVP and Phase 2/3 logic only ever look at the current price.

---
Sources referenced above:
- [GroceryCompare (open-source NZ grocery scraper)](https://github.com/jesmcc/GroceryCompare)
- [Consumer NZ: Foodstuffs removes online price-sorting tool](https://www.consumer.org.nz/articles/consumer-nz-questions-foodstuffs-removal-of-online-price-sorting-tool)
- [Microsoft Learn: Google external login setup in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-10.0)
- [Microsoft Learn: Use Identity to secure a Web API backend for SPAs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-8.0)
