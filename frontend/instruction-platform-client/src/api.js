import { ref } from 'vue'

const OLD_TOKEN_KEY = 'instruction_platform_token'
const OLD_USER_KEY = 'instruction_platform_user'

export const currentUser = ref(null)

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

export async function refreshCurrentUser() {
  const user = await apiFetch('/api/auth/me', { redirectOnUnauthorized: false })
  setCurrentUser(user)
  return user
}

export async function logout() {
  try {
    await apiFetch('/api/auth/logout', { method: 'POST' })
  } finally {
    clearSession()
  }
}

export async function apiFetch(url, options = {}) {
  const { redirectOnUnauthorized = true, ...fetchOptions } = options
  const headers = fetchOptions.headers ? { ...fetchOptions.headers } : {}

  const isFormData = fetchOptions.body instanceof FormData
  if (!isFormData && fetchOptions.body && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json'
  }

  const response = await fetch(url, { ...fetchOptions, headers, credentials: 'include' })
  if (response.status === 401) {
    clearSession()
    if (redirectOnUnauthorized) {
      window.location.href = '/login'
    }
    throw new Error('Необходимо войти заново')
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
  const headers = options.headers ? { ...options.headers } : {}

  const response = await fetch(url, { ...options, headers, credentials: 'include' })
  if (!response.ok) {
    throw new Error(await response.text())
  }
  return response.blob()
}
