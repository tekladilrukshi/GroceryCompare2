import { useState } from 'react'
import { GoogleLogin } from '@react-oauth/google'
import { useNavigate } from 'react-router-dom'

interface AuthResponse {
  accessToken: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export default function LoginPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  async function exchangeGoogleToken(idToken: string) {
    setError(null)
    const response = await fetch('/api/auth/google', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ idToken }),
    })
    if (!response.ok) {
      setError('Sign-in was rejected by the server. Please try again.')
      return
    }
    // Interim storage until the shared API client lands in PBI-028.
    const tokens = (await response.json()) as AuthResponse
    localStorage.setItem('accessToken', tokens.accessToken)
    localStorage.setItem('refreshToken', tokens.refreshToken)
    navigate('/dashboard')
  }

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
          if (response.credential) {
            void exchangeGoogleToken(response.credential)
          } else {
            setError('Google did not return a credential. Please try again.')
          }
        }}
        onError={() => setError('Google sign-in failed. Please try again.')}
      />
      {error && <p role="alert">{error}</p>}
    </main>
  )
}
