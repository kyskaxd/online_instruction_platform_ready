<template>
  <div>
    <header class="topbar" v-if="user">
      <router-link class="brand" to="/">Онлайн-инструктажи</router-link>
      <nav>
        <router-link v-if="canViewMaterials" to="/materials">Материалы</router-link>
        <router-link v-if="isEmployee" to="/my-tests">Мои тесты</router-link>
        <router-link v-if="isAdmin || isHR" to="/employees">Сотрудники</router-link>
        <router-link v-if="isManager" to="/tests/import">Тесты</router-link>
        <router-link v-if="isManager" to="/reports">Отчёты</router-link>
      </nav>
      <div class="userbox">
        <span>{{ user.email }} · {{ roleLabel }}</span>
        <button @click="logout">Выйти</button>
      </div>
    </header>

    <main class="container">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { currentUser, logout as logoutSession } from './api'

const router = useRouter()
const user = currentUser
const isEmployee = computed(() => user.value?.role === 'Employee')
const isAdmin = computed(() => user.value?.role === 'Admin')
const isHR = computed(() => user.value?.role === 'HR')
const isManager = computed(() => ['Admin', 'Manager'].includes(user.value?.role))
const canViewMaterials = computed(() => !isHR.value)
const roleLabel = computed(() => {
  const labels = {
    Admin: 'Админ',
    Manager: 'Менеджер',
    Employee: 'Сотрудник',
    HR: 'HR'
  }

  return labels[user.value?.role] || user.value?.role
})

async function logout() {
  await logoutSession()
  router.push('/login')
}
</script>
