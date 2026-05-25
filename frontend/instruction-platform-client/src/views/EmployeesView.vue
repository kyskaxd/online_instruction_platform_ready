<template>
  <section class="card">
    <h1>Сотрудники</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <form @submit.prevent="createEmployee" class="form-grid">
      <label>Фамилия<input v-model="form.lastName" required></label>
      <label>Имя<input v-model="form.firstName" required></label>
      <label>Отчество<input v-model="form.middleName"></label>
      <label>Отдел<input v-model="form.department" required></label>
      <label>Должность<input v-model="form.position" required></label>
      <label>Email<input v-model="form.email" type="email" required></label>
      <label>Дата найма<input v-model="form.hireDate" type="date"></label>
      <label>Пароль<input v-model="form.password" required></label>
      <label>
        Роль
        <select v-model="form.role">
          <option value="Employee">Сотрудник</option>
          <option value="Manager">Менеджер</option>
          <option value="Admin">Админ</option>
        </select>
      </label>
      <button :disabled="isSaving">Добавить</button>
    </form>
  </section>

  <section class="card">
    <div class="employees-header">
      <h2>Список сотрудников</h2>
      <label class="search-field">
        Поиск по имени
        <input v-model="searchQuery" placeholder="Введите ФИО">
      </label>
    </div>

    <div v-if="filteredEmployees.length === 0" class="empty-state">
      Сотрудники пока не добавлены.
    </div>

    <div v-else class="employee-grid">
      <article v-for="employee in filteredEmployees" :key="employee.id" class="employee-card">
        <div class="employee-card__top">
          <div>
            <h3>{{ employee.lastName }} {{ employee.firstName }} {{ employee.middleName || '' }}</h3>
            <p>{{ employee.email }}</p>
          </div>
          <span class="badge">{{ roleLabel(employee.role) }}</span>
        </div>

        <dl>
          <div>
            <dt>Отдел</dt>
            <dd>{{ employee.department }}</dd>
          </div>
          <div>
            <dt>Должность</dt>
            <dd>{{ employee.position }}</dd>
          </div>
          <div>
            <dt>Дата найма</dt>
            <dd>{{ formatDate(employee.hireDate) }}</dd>
          </div>
        </dl>

        <button
          v-if="canDelete(employee)"
          class="danger"
          :disabled="deletingId === employee.id"
          @click="deleteEmployee(employee)"
        >
          Удалить
        </button>
      </article>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { apiFetch, getCurrentUser } from '../api'

const employees = ref([])
const error = ref('')
const success = ref('')
const isSaving = ref(false)
const deletingId = ref(null)
const searchQuery = ref('')

const form = reactive({
  lastName: '',
  firstName: '',
  middleName: '',
  department: '',
  position: '',
  email: '',
  hireDate: '',
  password: '',
  role: 'Employee'
})

const roleLabels = {
  Admin: 'Админ',
  Manager: 'Менеджер',
  Employee: 'Сотрудник'
}

const filteredEmployees = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  if (!query) {
    return employees.value
  }

  return employees.value.filter((employee) => {
    const fullName = [
      employee.lastName,
      employee.firstName,
      employee.middleName
    ].filter(Boolean).join(' ').toLowerCase()

    return fullName.includes(query)
  })
})

async function loadEmployees() {
  employees.value = await apiFetch('/api/employees')
}

async function createEmployee() {
  error.value = ''
  success.value = ''
  isSaving.value = true

  try {
    await apiFetch('/api/employees', {
      method: 'POST',
      body: JSON.stringify({ ...form, hireDate: form.hireDate || null })
    })
    success.value = 'Сотрудник создан'
    Object.assign(form, {
      lastName: '',
      firstName: '',
      middleName: '',
      department: '',
      position: '',
      email: '',
      hireDate: '',
      password: '',
      role: 'Employee'
    })
    await loadEmployees()
  } catch (e) {
    error.value = e.message
  } finally {
    isSaving.value = false
  }
}

async function deleteEmployee(employee) {
  if (!confirm(`Удалить сотрудника ${employee.lastName} ${employee.firstName}?`)) {
    return
  }

  error.value = ''
  success.value = ''
  deletingId.value = employee.id

  try {
    await apiFetch(`/api/employees/${employee.id}`, { method: 'DELETE' })
    success.value = 'Сотрудник удалён'
    await loadEmployees()
  } catch (e) {
    error.value = e.message
  } finally {
    deletingId.value = null
  }
}

function canDelete(employee) {
  const currentUser = getCurrentUser()
  return currentUser?.role === 'Admin' && currentUser.employeeId !== employee.id
}

function roleLabel(role) {
  return roleLabels[role] || role
}

function formatDate(value) {
  if (!value) {
    return 'Не указана'
  }

  return new Date(value).toLocaleDateString('ru-RU')
}

onMounted(loadEmployees)
</script>

<style scoped>
.employees-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
}

.employees-header h2 {
  margin: 0;
}

.search-field {
  width: min(360px, 100%);
}

.employee-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 14px;
}

.employee-card {
  display: grid;
  gap: 16px;
  border: 1px solid #e6e9f2;
  border-radius: 8px;
  padding: 16px;
  background: #fff;
}

.employee-card__top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.employee-card__top .badge {
  flex: 0 0 auto;
  min-height: 28px;
  padding: 6px 12px;
  line-height: 1;
}

.employee-card h3 {
  margin: 0 0 6px;
  font-size: 18px;
}

.employee-card p {
  margin: 0;
  color: #667085;
}

.employee-card dl {
  display: grid;
  gap: 10px;
  margin: 0;
}

.employee-card dl div {
  display: grid;
  gap: 3px;
}

.employee-card dt {
  color: #667085;
  font-size: 13px;
  font-weight: 700;
}

.employee-card dd {
  margin: 0;
  color: #172033;
}

.employee-card button {
  justify-self: start;
}

.empty-state {
  color: #667085;
  padding: 10px 0;
}

@media (max-width: 720px) {
  .employees-header {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
