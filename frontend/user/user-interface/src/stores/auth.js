import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { login, logout, getCurrentUser } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || null)
  const user = ref(null)
  const roles = ref([])

  const isAuthenticated = computed(() => !!token.value)
  const hasRole = (role) => roles.value.includes(role)
  const hasAnyRole = (roleList) => roleList.some(r => roles.value.includes(r))

  async function loginUser(credentials) {
    try {
      const response = await login(credentials)
      console.log('[AuthStore] Login response:', response)
      console.log('[AuthStore] Login response keys:', Object.keys(response))
      token.value = response.Token || response.token || response.roleCodes || response.roleCodes
      const extractedRoles = response.RoleCodes || response.roleCodes || response.roles || []
      console.log('[AuthStore] Extracted roles from login:', extractedRoles)
      user.value = {
        id: response.UserId || response.userId || null,
        login: response.Login || response.login,
        username: response.Login || response.login,
        fullName: response.FullName || response.fullName,
        lastName: response.LastName || response.lastName,
        email: response.Email || response.email,
        roles: extractedRoles
      }
      // Извлекаем role codes из массива (если это массив объектов)
      if (Array.isArray(extractedRoles)) {
        roles.value = extractedRoles.map(r => typeof r === 'string' ? r : (r?.Code || r?.code || ''))
      } else {
        roles.value = extractedRoles
      }
      console.log('[AuthStore] roles after login:', roles.value)
      localStorage.setItem('token', token.value)
      return response
    }
    catch (error) {
      console.error('[AuthStore] Login error:', error)
      throw error
    }
  }
  async function logoutUser() {
    await logout()
    token.value = null
    user.value = null
    roles.value = []
    localStorage.removeItem('token')
  }

  async function fetchCurrentUser() {
    if (!token.value) return
    try {
      const response = await getCurrentUser()
      console.log('[AuthStore] GetMe response:', response)
      console.log('[AuthStore] GetMe response keys:', Object.keys(response))
      // Извлекаем RoleCodes из Roles[] или roles[] (каждый Role содержит Code/code)
      const rolesSource = response.Roles || response.roles
      let roleCodes = []
      if (Array.isArray(rolesSource)) {
        roleCodes = rolesSource.map(r => r && (r.Code || r.code || ''))
      } else if (rolesSource) {
        roleCodes = rolesSource
      }
      console.log('[AuthStore] Extracted roleCodes from /me:', roleCodes)
      
      user.value = {
        id: response.UserId || response.userId || response.user?.id || null,
        login: response.Login || response.login || response.user?.login || '',
        username: response.Login || response.login || response.user?.username || '',
        fullName: response.FullName || response.fullName || response.user?.fullName || '',
        lastName: response.LastName || response.lastName || response.user?.lastName || '',
        email: response.Email || response.email || response.user?.email || '',
        roles: roleCodes
      }
      roles.value = roleCodes
      console.log('[AuthStore] roles after fetchCurrentUser:', roles.value)
    } catch (error) {
      console.error('[AuthStore] Failed to fetch current user', error)
      logoutUser()
    }
  }

  return {
    token,
    user,
    roles,
    isAuthenticated,
    hasRole,
    hasAnyRole,
    loginUser,
    logoutUser,
    fetchCurrentUser
  }
})