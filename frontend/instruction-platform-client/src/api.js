import { ref } from 'vue'

const OLD_TOKEN_KEY = 'instruction_platform_token'
const OLD_USER_KEY = 'instruction_platform_user'

// Support both VITE_API_URL and VITE_API_BASE for compatibility.
// Normalize to remove trailing slashes so concatenation is predictable.
const RAW_API_BASE = import.meta.env.VITE_API_URL ?? import.meta.env.VITE_API_BASE ?? ''
const API_BASE = RAW_API_BASE.replace(/\/+$/, '')

function buildFullUrl(url) {
  // If already absolute, return as-is
  if (/^https?:\/\//i.test(url)) return url

  const path = url.startsWith('/') ? url : '/' + url
  if (!API_BASE) return path // use relative path (Vite dev proxy) if no base is set
  return API_BASE + path
}

export const currentUser = ref(null)

let refreshPromise = null

export function getCurrentUser() {
  return currentUser.value
}

function setCurrentUser(user) {
  currentUser.value = user
}

export function setSession(response) {
  clearLegacyStorage()
  setCurrentUser({
    userId: response.userId,
    email: response.email,
    role: response.role,
    employeeId: response.employeeId
  })
}

export function clearSession() {
  clearLegacyStorage()
  setCurrentUser(null)
}

function clearLegacyStorage() {
  localStorage.removeItem(OLD_TOKEN_KEY)
  localStorage.removeItem(OLD_USER_KEY)
}

async function tryRefreshSession() {
  if (!refreshPromise) {
    const fullUrl = buildFullUrl('/api/auth/refresh')
    refreshPromise = fetch(fullUrl, {
      method: 'POST',
      credentials: 'include'
    }).finally(() => {
      refreshPromise = null
    })
  }

  const response = await refreshPromise
  if (!response.ok) {
    return false
  }

  const data = await response.json()
  setSession(data)
  return true
}

export async function refreshCurrentUser() {
  try {
    const user = await apiFetch('/api/auth/me', { redirectOnUnauthorized: false })
    setCurrentUser(user)
    return user
  } catch {
    const refreshed = await tryRefreshSession()
    if (!refreshed) {
      clearSession()
      return null
    }

    const user = await apiFetch('/api/auth/me', { redirectOnUnauthorized: false })
    setCurrentUser(user)
    return user
  }
}

export async function logout() {
  try {
    await apiFetch('/api/auth/logout', { method: 'POST' })
  } finally {
    clearSession()
    localStorage.clear()
    sessionStorage.clear()
  }
}

export async function apiFetch(url, options = {}) {
  const { redirectOnUnauthorized = true, skipRefresh = false, ...fetchOptions } = options
  const headers = fetchOptions.headers ? { ...fetchOptions.headers } : {}

  const isFormData = fetchOptions.body instanceof FormData
  if (!isFormData && fetchOptions.body && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json'
  }

  // Build full URL (supports absolute urls or base from env)
  const fullUrl = buildFullUrl(url)

  const response = await fetch(fullUrl, { ...fetchOptions, headers, credentials: 'include' })

  // Handle 401 with refresh attempt (except refresh endpoint itself)
  if (response.status === 401 && !skipRefresh && url !== '/api/auth/refresh') {
    const refreshed = await tryRefreshSession()
    if (refreshed) {
      return apiFetch(url, { ...options, skipRefresh: true })
    }
  }

  if (response.status === 401) {
    clearSession()
    if (redirectOnUnauthorized) {
      window.location.href = '/login'
    }
    throw new Error('Сессия истекла. Войдите снова.')
  }

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Ошибка запроса: ${response.status}`)
  }

  if (response.status === 204) {
    return null
  }

  const contentType = response.headers.get('content-type') || ''
  return contentType.includes('application/json') ? response.json() : response.text()
}

export async function apiBlob(url, options = {}) {
  const fullUrl = buildFullUrl(url)
  const headers = options.headers ? { ...options.headers } : {}
  const { skipRefresh = false, ...fetchOptions } = options

  let response = await fetch(fullUrl, { ...fetchOptions, headers, credentials: 'include' })

  if (response.status === 401 && !skipRefresh) {
    const refreshed = await tryRefreshSession()
    if (refreshed) {
      response = await fetch(fullUrl, { ...fetchOptions, headers, credentials: 'include' })
    }
  }

  if (response.status === 401) {
    clearSession()
    window.location.href = '/login'
    throw new Error('Сессия истекла. Войдите снова.')
  }

  if (!response.ok) {
    throw new Error(await response.text())
  }
  return response.blob()
}