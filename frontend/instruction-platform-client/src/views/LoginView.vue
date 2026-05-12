<template>
  <section class="card" style="max-width: 480px; margin: 70px auto;">
    <h1>Вход</h1>
    <p>После первого запуска уже создан администратор:</p>
    

    <div v-if="error" class="error">{{ error }}</div>

    <form @submit.prevent="login" class="form-grid">
      <label>
        Email
        <input v-model="form.email" type="email" autocomplete="username" required>
      </label>
      <label>
        Пароль
        <input v-model="form.password" type="password" autocomplete="current-password" required>
      </label>
      <button :disabled="loading">{{ loading ? 'Входим...' : 'Войти' }}</button>
    </form>
  </section>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { apiFetch, setSession } from '../api'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const form = reactive({ email: 'admin@local.test', password: 'Admin123!' })

async function login() {
  loading.value = true
  error.value = ''
  try {
    const response = await apiFetch('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(form)
    })
    setSession(response)
    router.push('/')
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}
</script>
