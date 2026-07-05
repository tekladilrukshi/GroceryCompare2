# Grocery Compare App — Product Backlog

This backlog turns `architecture-plan.md` into buildable Product Backlog Items (PBIs), organized by phase and epic to match the roadmap in that document. It's written so a coding agent (e.g. Claude Code) can pick up PBIs roughly in order and implement them without needing the full planning conversation for context — each PBI stands alone with a user story, acceptance criteria, technical notes, and dependencies.

**How to use this with Claude Code:** work phase by phase, top to bottom within a phase. Point Claude Code at this file plus `architecture-plan.md` (for the data model, API shapes, and architecture diagram) at the start of a session, and ask it to implement one PBI (or one epic) at a time — that keeps changes reviewable. Dependencies are noted so out-of-order work can be avoided.

Numbering is sequential across the whole backlog (PBI-001 upward) so IDs stay stable as items move between "to do," "in progress," and "done."

---

## Phase 0 — Foundations

### Epic 0.1 — Repo & Solution Scaffolding

**PBI-001: Create the .NET solution and empty projects**
- Story: As a developer, I want the solution skeleton in place, so that all subsequent backend work has a consistent home.
- Acceptance criteria:
  - Solution `GroceryCompare.sln` with projects `GroceryCompare.Api`, `GroceryCompare.Domain`, `GroceryCompare.Infrastructure`, `GroceryCompare.Scraper`, matching the structure in architecture-plan.md §10.
  - `tests/GroceryCompare.Domain.Tests` and `tests/GroceryCompare.Api.Tests` projects created and referencing their targets.
  - Solution builds with `dotnet build` with zero projects, zero warnings.
- Technical notes: .NET 8 (or later LTS available at build time). Api references Domain and Infrastructure; Scraper references Domain and Infrastructure; Domain has no external dependencies.
- Depends on: none.

**PBI-002: Create the React SPA skeleton**
- Story: As a developer, I want a React + TypeScript app scaffolded, so that frontend work has a consistent starting point.
- Acceptance criteria:
  - `src/GroceryCompare.Web` created via Vite (React + TypeScript template).
  - Basic routing library installed (e.g. React Router) with placeholder routes for `/login`, `/dashboard`, `/lists/:id`.
  - `npm run build` and `npm run dev` both succeed.
- Technical notes: keep dependencies minimal at this stage; state management library decision can wait until PBI-028.
- Depends on: none.

**PBI-003: Local dev database + EF Core wiring**
- Story: As a developer, I want a local SQL Server instance and an EF Core `DbContext` wired up, so that I can run and test the API locally before any Azure resources exist.
- Acceptance criteria:
  - `docker-compose.yml` (or equivalent) spins up a local SQL Server for development.
  - `GroceryCompareDbContext` created in `GroceryCompare.Infrastructure`, registered in `Program.cs` via connection string from configuration/user secrets.
  - `dotnet ef migrations add InitialCreate` runs successfully against an empty `DbContext` (entities added in PBI-010).
- Technical notes: matches architecture-plan.md §2 (Azure SQL Database in production, same engine locally).
- Depends on: PBI-001.

**PBI-004: CI pipelines for API, SPA, and scraper**
- Story: As a developer, I want automated build/test on every push, so that regressions are caught before deployment.
- Acceptance criteria:
  - GitHub Actions workflow builds and runs tests for the .NET solution on push/PR.
  - Separate (or matrixed) workflow builds and lints the React app.
  - Both workflows fail the build on test failure or lint error.
- Technical notes: deployment steps can be stubbed/no-op until Azure resources exist (PBI-005).
- Depends on: PBI-001, PBI-002.

**PBI-005: Azure resource scaffolding (infra as code)**
- Story: As a developer, I want the core Azure resources defined as code, so that environments are reproducible and not clicked together manually.
- Acceptance criteria:
  - Bicep (or Terraform) templates under `infra/` provisioning: Azure SQL Database, App Service or Container Apps (API), Static Web Apps (SPA), Container Apps Jobs or Functions (scraper), Key Vault, Application Insights — per architecture-plan.md §9.
  - Templates parameterized for environment name (dev/prod) so the same code stands up multiple environments.
  - A README in `infra/` documents how to deploy.
- Technical notes: no Azure AD/Entra tenant needed — auth is direct Google Sign-In (architecture-plan.md §6).
- Depends on: PBI-001.

### Epic 0.2 — Authentication End-to-End

**PBI-006: Google Sign-In button in the SPA**
- Story: As a user, I want to sign in with my Google account, so that I don't need a separate password for this app.
- Acceptance criteria:
  - Google Identity Services library integrated; a "Sign in with Google" button renders on `/login`.
  - On successful Google auth, the SPA receives a Google ID token client-side.
  - Google OAuth client ID is read from build-time environment config, not hardcoded.
- Technical notes: architecture-plan.md §6, step 1.
- Depends on: PBI-002.

**PBI-007: `POST /api/auth/google` endpoint**
- Story: As the API, I need to validate a Google ID token and issue my own session token, so that the SPA can call authenticated endpoints without ever handling Google tokens again.
- Acceptance criteria:
  - Endpoint accepts `{ idToken: string }`, validates signature/audience/expiry via `Google.Apis.Auth`.
  - On first sign-in for a given Google subject claim, a new `User` row is created; on subsequent sign-ins, the existing row is matched.
  - Returns an app-issued JWT (and refresh token) on success; returns 401 on an invalid/expired Google token.
  - Unit tests cover: new user creation, existing user match, invalid token rejection.
- Technical notes: architecture-plan.md §6, step 3; `User` entity from PBI-010 is a dependency but can be stubbed with a minimal shape here if sequenced earlier.
- Depends on: PBI-006, PBI-010 (or a minimal `User` table added in this PBI if built before PBI-010).

**PBI-008: JWT bearer middleware + `GET /api/auth/me`**
- Story: As the API, I want to validate app-issued JWTs on protected endpoints, so that only authenticated users can access their data.
- Acceptance criteria:
  - JWT bearer authentication middleware configured in `Program.cs`, validating the app's own signing key (from Key Vault/config, not hardcoded).
  - `GET /api/auth/me` returns the current user's profile (id, email, display name) for a valid token, 401 otherwise.
  - All subsequent authenticated endpoints in this backlog use `[Authorize]`.
- Depends on: PBI-007.

**PBI-009: Refresh token issuance, rotation, and logout**
- Story: As a user, I want my session to stay alive without re-authenticating constantly, and to be able to sign out, so that the app is convenient and secure.
- Acceptance criteria:
  - `POST /api/auth/refresh` exchanges a valid refresh token for a new app JWT; rotates the refresh token on use.
  - `POST /api/auth/logout` invalidates the current refresh token.
  - Refresh tokens are stored server-side (hashed) so they can be revoked.
- Depends on: PBI-008.

---

## Phase 1 — MVP

### Epic 1.1 — Data Model & Migrations

**PBI-010: Core EF Core entities and initial migration**
- Story: As a developer, I need the core domain entities modeled, so that every other Phase 1 feature has a schema to build on.
- Acceptance criteria:
  - Entities implemented per architecture-plan.md §4: `User`, `Franchise`, `Store`, `UserStoreSelection`, `GroceryItem`, `ItemAlias`, `Price`, `ShoppingList`, `ShoppingListItem`, `ScrapeRun`.
  - Relationships/foreign keys match the plan (e.g. `UserStoreSelection` as a many-to-many join, `ItemAlias` linking `GroceryItem` to a `Store`/`Franchise` + external code).
  - Migration applies cleanly to a fresh database.
- Depends on: PBI-003.

**PBI-011: Seed Franchise reference data**
- Story: As a developer, I want the three franchises pre-populated, so that every other feature (stores, prices) has something to attach to.
- Acceptance criteria:
  - A seed/migration step inserts exactly three `Franchise` rows: Pak'nSave, Woolworths NZ, New World.
  - Re-running the seed is idempotent (no duplicates).
- Depends on: PBI-010.

### Epic 1.2 — Store Directory Scraping (all three franchises)

**PBI-012: `IStoreSource` interface + Pak'nSave store-locator adapter**
- Story: As the system, I need to fetch Pak'nSave's real store locations, so that users can select real stores instead of test data.
- Acceptance criteria:
  - `IStoreSource` interface defined in `GroceryCompare.Domain` (or Scraper) with a method returning a list of stores (name, address, suburb, region, lat/long, external store code).
  - `PaknSaveStoreSource` implementation fetches Pak'nSave's store locator data and maps it to that shape.
  - Adapter has unit tests using a recorded/mocked HTTP response (not a live network call in CI).
- Technical notes: architecture-plan.md §7, "adapter pattern" and "store directory sync." This PBI covers Pak'nSave only; New World and Woolworths NZ are separate PBIs so one retailer's site changes don't block the others.
- Depends on: PBI-001.

**PBI-013: New World store-locator adapter**
- Story: same as PBI-012, for New World.
- Acceptance criteria: same shape as PBI-012, implemented as `NewWorldStoreSource`. Given both Pak'nSave and New World are run by Foodstuffs, check whether the underlying endpoint/format is shared with PBI-012 and factor out common code if so.
- Depends on: PBI-012 (for the shared-code check; can be built in parallel if no sharing turns out to apply).

**PBI-014: Woolworths NZ store-locator adapter**
- Story: same as PBI-012, for Woolworths NZ.
- Acceptance criteria: same shape as PBI-012, implemented as `WoolworthsStoreSource`.
- Depends on: PBI-001.

**PBI-015: Store-sync worker + `POST /api/admin/stores/sync`**
- Story: As an admin (developer, at this stage), I want to trigger a store-directory refresh across all three franchises, so that the `Store` table reflects real, current locations.
- Acceptance criteria:
  - A sync routine calls all three `IStoreSource` implementations and upserts results into `Store` (match on `ExternalStoreCode` + `FranchiseId`; update `LastSyncedAt`).
  - `POST /api/admin/stores/sync` triggers this on demand (this is a manual/on-demand trigger for Phase 1 — a scheduled version comes in Phase 3, PBI-044).
  - Sync is logged (count added/updated per franchise) for basic visibility even without the full `ScrapeRun` reporting UI.
- Depends on: PBI-012, PBI-013, PBI-014, PBI-010.

**PBI-016: Store data quality pass**
- Story: As a user, I want store names/addresses to be clean and de-duplicated, so that the store picker isn't confusing.
- Acceptance criteria:
  - Manual review of the first sync's output; obvious duplicates or malformed entries (closed stores, test entries) are filtered or flagged `IsActive = false`.
  - Document any per-franchise quirks discovered (e.g. inconsistent region naming) in a short `docs/store-sync-notes.md`.
- Depends on: PBI-015.

### Epic 1.3 — Store Selection

**PBI-017: `GET /api/franchises` and `GET /api/stores`**
- Story: As a user, I want to browse all known stores, so that I can find the ones near me.
- Acceptance criteria:
  - `GET /api/franchises` returns the three franchises.
  - `GET /api/stores?franchise=&region=` returns matching active stores, paginated if the list is large.
- Depends on: PBI-015.

**PBI-018: `GET`/`PUT /api/users/me/stores`**
- Story: As a user, I want to select and change my personal set of stores at any time, so that comparisons are relevant to where I actually shop.
- Acceptance criteria:
  - `GET /api/users/me/stores` returns the current user's selected stores.
  - `PUT /api/users/me/stores` replaces the full selection set with the given store IDs (supports any mix of franchises/locations, including zero or many).
  - Both endpoints require authentication and operate only on the calling user's own selection.
- Depends on: PBI-008, PBI-017.

**PBI-019: Frontend store picker**
- Story: As a user, I want a UI to browse and select my stores, so that I can set this up without needing the API directly.
- Acceptance criteria:
  - A page lists stores (filterable by franchise/region), with multi-select checkboxes reflecting current selection.
  - Saving calls `PUT /api/users/me/stores`; the page can be revisited and changed at any time.
- Depends on: PBI-018, PBI-028.

### Epic 1.4 — Grocery Item Catalog & Manual Pricing (MVP scope)

**PBI-020: Seed `GroceryItem` catalog via CSV import**
- Story: As a developer, I want a starter catalog of common grocery items, so that the app has something real to search and price for the MVP.
- Acceptance criteria:
  - A CSV format defined for `GroceryItem` (canonical name, brand, size/unit, category, optional barcode).
  - An import tool/script loads a CSV (~50–100 common items is enough for MVP) into `GroceryItem`.
  - Import is re-runnable without creating duplicates.
- Depends on: PBI-010.

**PBI-021: Seed `ItemAlias` + manual `Price` rows**
- Story: As a developer, I want manually-entered prices for the seeded items at the synced stores, so that price comparison works end-to-end before any real scraping exists.
- Acceptance criteria:
  - A CSV/seed format for `ItemAlias` (linking a `GroceryItem` to a franchise/store) and `Price` (per store, current price).
  - Seed data covers enough items × stores to meaningfully exercise the comparison and recommendation features.
  - Documented as clearly temporary/manual in code comments, to be replaced by Phase 2/3 scraping.
- Depends on: PBI-020, PBI-016.

**PBI-022: Basic `GET /api/items/search`**
- Story: As a user, I want to find items by typing part of the name, so that I can add them to my list.
- Acceptance criteria:
  - Endpoint does a simple `LIKE`/`CONTAINS`-style match against `GroceryItem.CanonicalName`, returning up to N results.
  - Explicitly out of scope here: true type-ahead debouncing/ranking and full-text indexing — that's Phase 3 (PBI-041/042). This PBI just needs to return correct, if unoptimized, results.
- Depends on: PBI-020.

### Epic 1.5 — Shopping Lists

**PBI-023: Shopping list CRUD endpoints**
- Story: As a user, I want to create shopping lists and add/remove items with quantities, so that I can build up what I intend to buy.
- Acceptance criteria:
  - `GET /api/lists`, `POST /api/lists` (create), `POST /api/lists/{id}/items`, `DELETE /api/lists/{id}/items/{itemId}` all implemented and authenticated.
  - A user can only see/modify their own lists (403/404 otherwise).
- Depends on: PBI-008, PBI-010.

**PBI-024: Frontend shopping list UI**
- Story: As a user, I want to manage my shopping list in the browser, so that I don't need to call the API directly.
- Acceptance criteria:
  - A list page: create a list, search items (using PBI-022) and add them with a quantity, remove items, see current contents.
- Depends on: PBI-023, PBI-022, PBI-028.

### Epic 1.6 — Price Comparison & Cheapest-Store Recommendation

**PBI-025: `GET /api/lists/{id}/prices`**
- Story: As a user, I want to see, for every item on my list, the price at each of my selected stores, so that I can compare directly.
- Acceptance criteria:
  - Returns a structure keyed by list item, each with the price at every one of the user's selected stores that stocks it (via `ItemAlias`/`Price`), and a clear marker for stores that don't stock a given item.
  - Only ever reads the latest `Price` row per item/store (per architecture-plan.md §4 — no history logic here).
- Depends on: PBI-021, PBI-023, PBI-018.

**PBI-026: `GET /api/lists/{id}/recommendation` (single cheapest store only)**
- Story: As a user, I want to know which single store is cheapest for my whole list, so that I can decide where to shop.
- Acceptance criteria:
  - For each of the user's selected stores, sums `price × quantity` across all list items; stores missing an item are either excluded or clearly flagged as incomplete, per architecture-plan.md §8.
  - Returns the cheapest complete-coverage store and its total.
  - Split-by-store logic is explicitly deferred to Phase 2 (PBI-035) — this endpoint returns only the single-store recommendation for now.
  - Unit tests cover: all stores stock everything, some stores missing items, tie-breaking behavior.
- Depends on: PBI-025.

**PBI-027: Frontend price comparison + recommendation view**
- Story: As a user, I want to see prices side-by-side and a clear "cheapest store" callout, so that the value of the app is obvious at a glance.
- Acceptance criteria:
  - Table of items × selected stores with prices (from PBI-025).
  - A prominent banner/callout showing the cheapest store and total (from PBI-026).
- Depends on: PBI-025, PBI-026, PBI-028.

### Epic 1.7 — Frontend Foundation

**PBI-028: App shell, routing, auth guard, API client**
- Story: As a developer, I want a consistent app shell with auth-aware routing and a shared API client, so that feature pages don't each reinvent this.
- Acceptance criteria:
  - Route guard redirects unauthenticated users to `/login`.
  - Shared API client attaches the app JWT to requests and calls `/api/auth/refresh` on 401 (using PBI-009), retrying once.
  - Basic navigation shell (header, sign-out) present on authenticated pages.
- Depends on: PBI-007, PBI-009, PBI-002.

**PBI-029: Baseline styling/layout**
- Story: As a user, I want the app to look coherent rather than unstyled, so that it's pleasant to use even at MVP stage.
- Acceptance criteria: a shared layout/theme (component library or lightweight custom CSS) applied consistently across the pages built in Phase 1.
- Depends on: PBI-028.

---

## Phase 2 — Real price data, Woolworths NZ

### Epic 2.1 — Woolworths NZ Price Scraper

**PBI-030: `IPriceSource` interface + Woolworths NZ price adapter**
- Story: As the system, I need real, current Woolworths NZ prices, so that comparisons stop relying on manually-seeded data.
- Acceptance criteria:
  - `IPriceSource` interface defined (mirrors the `IStoreSource` pattern from Phase 1): given a store + product identifier, returns current price and sale status.
  - `WoolworthsPriceSource` implementation fetches product/price data from Woolworths NZ's own site/app endpoints for a given store.
  - Unit tests use recorded/mocked responses.
- Technical notes: architecture-plan.md §7 — Woolworths NZ is the confirmed first adapter.
- Depends on: PBI-010, PBI-016.

**PBI-031: Scheduled sync job + `ScrapeRun` logging**
- Story: As the system, I want Woolworths NZ prices refreshed on a schedule automatically, so that data doesn't go stale without anyone noticing.
- Acceptance criteria:
  - A Container Apps Job (or Azure Function timer trigger) runs the Woolworths adapter on a schedule (e.g. nightly) across all synced Woolworths stores.
  - Each run writes a `ScrapeRun` row (start/end time, status, items updated, error summary).
  - New prices are inserted as new `Price` rows (not overwritten), consistent with the timestamped design in architecture-plan.md §4.
- Depends on: PBI-030.

**PBI-032: Resilience — stale-data fallback and "as of" indicator**
- Story: As a user, I want to still see (slightly old) prices if a scrape run fails, rather than a broken page, so that the app stays useful.
- Acceptance criteria:
  - If a scheduled run fails or partially fails, the API continues serving the last successfully captured price per item/store.
  - `GET /api/lists/{id}/prices` (PBI-025) includes a "prices as of" timestamp per store so staleness is visible to the user.
- Depends on: PBI-031, PBI-025.

### Epic 2.2 — Item Matching / Reconciliation

**PBI-033: Fuzzy matching workflow for Woolworths SKUs**
- Story: As a developer/curator, I need a way to link Woolworths NZ's own product codes to our canonical `GroceryItem` catalog, so that scraped prices land against the right item.
- Acceptance criteria:
  - A matching routine proposes candidate matches (fuzzy name + brand + size) between Woolworths search results and `GroceryItem` rows.
  - Confirmed matches are persisted as `ItemAlias` rows.
  - Unmatched/low-confidence candidates are surfaced for manual review rather than auto-linked.
- Depends on: PBI-030, PBI-020.

**PBI-034: Manual override/admin endpoint for item matches**
- Story: As a developer/curator, I want to correct a wrong or missing match by hand, so that bad matches don't silently produce wrong prices.
- Acceptance criteria: an authenticated admin-only endpoint (or internal tool) to create/edit/delete `ItemAlias` rows directly.
- Depends on: PBI-033.

### Epic 2.3 — Split-by-Store Suggestion

**PBI-035: Split-by-store calculation**
- Story: As a user, I want to know if splitting my shopping across stores would save money, so that I can choose convenience vs. savings.
- Acceptance criteria:
  - Extends `GET /api/lists/{id}/recommendation` (PBI-026) with a split option: cheapest store per item, summed, per architecture-plan.md §8.
  - Only surfaces the split suggestion when it beats the single-store total by a configurable minimum margin.
  - Unit tests cover: split beats single-store, split doesn't clear the threshold (not suggested), ties.
- Depends on: PBI-026, PBI-031.

**PBI-036: Frontend split-suggestion display**
- Story: As a user, I want to see the split option clearly alongside the single-cheapest-store option, so that I can decide which to act on.
- Acceptance criteria: recommendation view (PBI-027) shows both options when a split is suggested, including the per-store breakdown and total savings.
- Depends on: PBI-035, PBI-027.

### Epic 2.4 — Observability

**PBI-037: `GET /api/admin/scrape-runs`**
- Story: As a developer, I want to see recent scrape run history, so that I can debug data-freshness issues without querying the database directly.
- Acceptance criteria: authenticated admin endpoint listing recent `ScrapeRun` rows with status/error summary, filterable by franchise.
- Depends on: PBI-031.

---

## Phase 3 — Full price data, autocomplete, polish

### Epic 3.1 — Remaining Scraper Adapters

**PBI-038: Pak'nSave price adapter**
- Story: same pattern as PBI-030, for Pak'nSave.
- Acceptance criteria: `PaknSavePriceSource` implementing `IPriceSource`, wired into the scheduled sync job, with recorded-response unit tests.
- Depends on: PBI-030, PBI-012.

**PBI-039: New World price adapter**
- Story: same pattern as PBI-030, for New World.
- Acceptance criteria: `NewWorldPriceSource` implementing `IPriceSource`; check for shared code with PBI-038 since both are Foodstuffs, per architecture-plan.md §7.
- Depends on: PBI-038, PBI-013.

**PBI-040: Extend item-matching workflow to all three franchises**
- Story: As a developer/curator, I want the matching workflow from PBI-033 to work across all franchises, so that item coverage is consistent everywhere.
- Acceptance criteria: matching routine generalized (not Woolworths-specific); existing Woolworths `ItemAlias` data unaffected.
- Depends on: PBI-033, PBI-038, PBI-039.

### Epic 3.2 — Type-ahead Search

**PBI-041: Full-text/trigram index on item names**
- Story: As the system, I need fast fuzzy search over item names, so that type-ahead feels instant even as the catalog grows.
- Acceptance criteria: index added on `GroceryItem.CanonicalName` and `ItemAlias.ExternalName` per architecture-plan.md §4; query plan verified to use it.
- Depends on: PBI-022.

**PBI-042: Upgraded `GET /api/items/search`**
- Story: As a user, I want relevant suggestions after typing just a few letters, so that finding items is fast.
- Acceptance criteria: endpoint reworked to use the index from PBI-041, ranked by relevance, responds well under load-tested latency targets (define a target, e.g. p95 < 200ms at expected catalog size). Note in code whether/when Azure AI Search would become necessary (architecture-plan.md §2) — only adopt it if plain SQL search proves insufficient.
- Depends on: PBI-041.

**PBI-043: Frontend type-ahead component**
- Story: As a user, I want suggestions to appear as I type, so that adding items is quick.
- Acceptance criteria: debounced input (after 2–3 characters) calling PBI-042, with keyboard navigation of suggestions.
- Depends on: PBI-042, PBI-024.

### Epic 3.3 — Scheduled Store-Directory Auto-Sync

**PBI-044: Convert manual store sync to a scheduled job**
- Story: As the system, I want store directories to refresh automatically, so that new/closed stores are reflected without a manual trigger.
- Acceptance criteria: the Phase 1 manual sync (PBI-015) is wrapped in a recurring schedule (e.g. weekly) across all three franchises, with the same upsert/logging behavior.
- Depends on: PBI-015.

### Epic 3.4 — Admin Visibility

**PBI-045: Admin scrape-health dashboard**
- Story: As a developer, I want a simple page showing scrape health at a glance, so that I don't have to query `ScrapeRun` manually.
- Acceptance criteria: an internal frontend page (or simple server-rendered view) showing last sync time and status per franchise for both store-sync and price-sync jobs, using PBI-037.
- Depends on: PBI-037, PBI-044.

---

## Phase 4 — Hardening & scale

### Epic 4.1 — Price History

**PBI-046: Price history query endpoints**
- Story: As a user, I want to see how a price has changed over time, so that I can judge if now is a good time to buy.
- Acceptance criteria: new endpoint(s) returning historical `Price` rows for an item/store over a date range — this is the first place the app reads more than the latest price row, since `Price` has been timestamped since Phase 1.
- Depends on: PBI-031 (enough history accumulated to be useful).

**PBI-047: Frontend price trend view**
- Story: As a user, I want a simple chart of price history per item/store, so that trends are easy to read.
- Acceptance criteria: a small chart/sparkline on the item detail view, backed by PBI-046.
- Depends on: PBI-046.

### Epic 4.2 — Performance & Scale

**PBI-048: Load-test and cache the recommendation engine**
- Story: As the system, I want recommendation/price-comparison queries to stay fast as data grows, so that the app doesn't degrade over time.
- Acceptance criteria: load test established for `GET /api/lists/{id}/prices` and `.../recommendation`; caching (e.g. short-lived in-memory or distributed cache) added if the load test shows it's needed.
- Depends on: PBI-026, PBI-035.

**PBI-049: Index/query review**
- Story: As a developer, I want database indexes tuned to real usage patterns, so that queries stay efficient.
- Acceptance criteria: review actual query patterns after Phase 1–3 usage; add/adjust indexes beyond the initial set in architecture-plan.md §4 as needed, with before/after query-plan evidence.
- Depends on: PBI-048.

### Epic 4.3 — Monitoring & Alerting

**PBI-050: Alerts on scrape failures / stale data**
- Story: As a developer, I want to be notified automatically when scraping breaks, so that I don't find out from stale prices in the UI.
- Acceptance criteria: Application Insights alert rules fire when a `ScrapeRun` fails or when a franchise's data hasn't refreshed within an expected window.
- Depends on: PBI-031, PBI-037.

### Epic 4.4 — Legal/ToS Revisit

**PBI-051: Re-evaluate scraping posture if the app grows**
- Story: As the app owner, I want to revisit the legal/ToS risk deprioritized in Phase 1 (architecture-plan.md §7/§12), so that the approach stays appropriate if usage grows beyond a small friends-and-family group.
- Acceptance criteria: not a coding task — a documented decision point/checklist to revisit before any wider release (e.g. public sign-ups, commercial use), covering each retailer's terms of use and request-rate posture.
- Depends on: none (trigger is scale, not a prior PBI).

---

## Summary

51 PBIs across 5 phases: Phase 0 (9), Phase 1 (20), Phase 2 (8), Phase 3 (8), Phase 4 (6). Phase 0 and Phase 1 are ready to hand to Claude Code now; Phases 2–4 are fully specced but depend on earlier phases landing first, and may need light adjustment once real scraper behavior (Phase 2) reveals anything the plan didn't anticipate.
