import { useState } from 'react'
import { GoogleLogin } from '@react-oauth/google'

export default function LoginPage() {
  const [idToken, setIdToken] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (!import.meta.env.VITE_GOOGLE_CLIENT_ID) {
    return (
      <main>
        <h1>Sign in</h1>
        <p>
          Google Sign-In is not configured. Set <code>VITE_GOOGLE_CLIENT_ID</code> in{' '}
          <code>.env.local</code> (see <code>.env.example</code>).
        </p>
      </main>
    )
  }

  return (
    <main>
      <h1>Sign in</h1>
      <GoogleLogin
        onSuccess={(response) => {
          setError(null)
          setIdToken(response.credential ?? null)
        }}
        onError={() => setError('Google sign-in failed. Please try again.')}
      />
      {idToken && <p>Google ID token received ({idToken.length} chars).</p>}
      {error && <p role="alert">{error}</p>}
    </main>
  )
}
