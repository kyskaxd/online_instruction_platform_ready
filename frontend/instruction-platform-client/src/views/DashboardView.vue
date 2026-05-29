<template>
  <section class="profile-card">
    <article class="card">
      <h1>Личный кабинет</h1>
      <p>Здесь отображается информация о вашем профиле и доступные действия.</p>
      <div v-if="error" class="error">{{ error }}</div>
      <div v-if="isLoading">Загрузка данных...</div>
      <div v-else>
        <dl class="profile-info">
          <div>
            <dt>ФИО</dt>
            <dd>{{ employee.fullName }}</dd>
          </div>
          <div>
            <dt>Email</dt>
            <dd>{{ employee.email }}</dd>
          </div>
          <div>
            <dt>Роль</dt>
            <dd>{{ roleLabel(employee.role) }}</dd>
          </div>
          <div>
            <dt>Отдел</dt>
            <dd>{{ employee.department || 'Не указан' }}</dd>
          </div>
          <div>
            <dt>Должность</dt>
            <dd>{{ employee.position || 'Не указана' }}</dd>
          </div>
          <div>
            <dt>Дата найма</dt>
            <dd>{{ formatDate(employee.hireDate) }}</dd>
          </div>
          <div>
            <dt>Статус</dt>
            <dd>{{ employee.isActive ? 'Активен' : 'Неактивен' }}</dd>
          </div>
        </dl>
      </div>
    </article>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { apiFetch } from '../api'

const employee = ref({
  fullName: '',
  email: '',
  role: '',
  department: '',
  position: '',
  hireDate: null,
  isActive: false
})
const error = ref('')
const isLoading = ref(true)

const roleLabel = (role) => {
  const labels = {
    Admin: 'Админ',
    Manager: 'Менеджер',
    Employee: 'Сотрудник',
    HR: 'HR'
  }

  return labels[role] || role
}

function formatDate(value) {
  if (!value) {
    return 'Не указана'
  }

  return new Date(value).toLocaleDateString('ru-RU')
}

async function loadProfile() {
  try {
    const data = await apiFetch('/api/employees/me')
    employee.value = {
      fullName: [data.lastName, data.firstName, data.middleName].filter(Boolean).join(' '),
      email: data.email,
      role: data.role,
      department: data.department,
      position: data.position,
      hireDate: data.hireDate,
      isActive: data.isActive
    }
  } catch (err) {
    error.value = err.message || 'Не удалось загрузить профиль.'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadProfile)
</script>

<style scoped>
.profile-card {
  display: grid;
  gap: 20px;
}

.profile-info {
  display: grid;
  gap: 14px;
  margin-top: 16px;
}

.profile-info dt {
  font-weight: 700;
}

.profile-info dd {
  margin: 0 0 12px;
}

</style>
