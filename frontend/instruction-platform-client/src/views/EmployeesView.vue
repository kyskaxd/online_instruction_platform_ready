<template>
  <section v-if="canManageEmployees" class="card">
    <h1>Управление персоналом</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <div class="section-divider">
      <h2>Добавление отдела</h2>
      <form @submit.prevent="createDepartment" class="form-grid">
        <label style="grid-column: 1 / -1;">Название отдела<input v-model="departmentForm.name" required></label>
        <button style="grid-column: 1 / -1;">Добавить отдел</button>
      </form>
    </div>

    <div class="section-divider">
      <h2>Добавление должности в отдел</h2>
      <form @submit.prevent="createPosition" class="form-grid">
        <label>
          Отдел
          <select v-model.number="positionForm.departmentId" required>
            <option value="0" disabled>Выберите отдел</option>
            <option v-for="department in departmentsForEmployeeForm" :key="department.id" :value="department.id">
              {{ department.name }}
            </option>
          </select>
        </label>
        <label style="grid-column: 1 / -1;">Название должности<input v-model="positionForm.name" required></label>
        <button style="grid-column: 1 / -1;" :disabled="isPositionSaving">Добавить должность</button>
      </form>
    </div>

    <div class="section-divider">
      <h2>Добавление сотрудника</h2>
      <form @submit.prevent="createEmployee" class="form-grid">
        <label>Фамилия<input v-model="form.lastName" required></label>
        <label>Имя<input v-model="form.firstName" required></label>
        <label>Отчество<input v-model="form.middleName"></label>
        <label>
          Отдел
          <select v-model.number="form.departmentId" required>
            <option value="0" disabled>Выберите отдел</option>
            <option v-for="department in departmentsForEmployeeForm" :key="department.id" :value="department.id">
              {{ department.name }}
            </option>
          </select>
        </label>
        <label>
          Должность
          <select v-model.number="form.positionId" :disabled="!form.departmentId || positions.length === 0" required>
            <option value="0" disabled>Выберите должность</option>
            <option v-for="position in positions" :key="position.id" :value="position.id">
              {{ position.name }}
            </option>
          </select>
          <small v-if="form.departmentId && positions.length === 0">Нет должностей для этого отдела</small>
        </label>
        <label>Email<input v-model="form.email" type="email" required></label>
        <label>Дата найма<input v-model="form.hireDate" type="date"></label>
        <label>Пароль<input v-model="form.password" required></label>
        <label>
          Роль
          <select v-model="form.role">
            <option value="Employee">Сотрудник</option>
            <option value="Manager" v-if="isAdmin">Менеджер</option>
            <option value="HR" v-if="isAdmin">HR</option>
          </select>
        </label>
        <button style="height: 45px; align-self:end" :disabled="isSaving">Добавить</button>
      </form>
    </div>
  </section>

  <section class="card">
    <div class="employees-header">
      <h2>Список сотрудников</h2>
      <div class="filter-group">
        <label class="search-field">
          Поиск по имени
          <input v-model="searchQuery" placeholder="Введите ФИО">
        </label>
        <label class="department-filter">
          Отдел
          <select v-model="departmentFilter">
            <option value="">Все отделы</option>
            <option v-for="department in departmentsForFilter" :key="department.id" :value="department.name">
              {{ department.name }}
            </option>
          </select>
        </label>
      </div>
    </div>

    <div v-if="filteredEmployees.length === 0" class="empty-state">
      Сотрудники пока не добавлены.
    </div>

    <div v-else class="employee-grid">
      <article v-for="employee in filteredEmployees" :key="employee.id" class="employee-card">
        <div class="employee-card__top">
          <div>
            <h3>
              <router-link class="employee-name-link" :to="`/employees/${employee.id}`">
                {{ employee.lastName }} {{ employee.firstName }} {{ employee.middleName || '' }}
              </router-link>
            </h3>
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
          <div>
            <dt>Статус</dt>
            <dd>{{ employee.isActive ? 'Активен' : 'Неактивен' }}</dd>
          </div>
        </dl>

        <button
          v-if="canDelete(employee)"
          :class="['employee-action', employee.isActive ? 'employee-action--freeze' : 'employee-action--unfreeze']"
          :disabled="deletingId === employee.id"
          @click="toggleActive(employee)"
        >
          {{ employee.isActive ? 'Заморозить' : 'Разморозить' }}
        </button>
      </article>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { apiFetch, getCurrentUser } from '../api'

const employees = ref([])
const departments = ref([])
const positions = ref([])
const departmentFilter = ref('')
const error = ref('')
const success = ref('')
const isSaving = ref(false)
const isPositionSaving = ref(false)
const deletingId = ref(null)
const searchQuery = ref('')

const departmentForm = reactive({
  name: ''
})

const positionForm = reactive({
  name: '',
  departmentId: 0
})

const form = reactive({
  lastName: '',
  firstName: '',
  middleName: '',
  departmentId: 0,
  positionId: 0,
  email: '',
  hireDate: '',
  password: '',
  role: 'Employee'
})

const roleLabels = {
  Admin: 'Админ',
  Manager: 'Менеджер',
  Employee: 'Сотрудник',
  HR: 'HR'
}

const currentUser = getCurrentUser()
const isAdmin = computed(() => currentUser?.role === 'Admin')
const isHR = computed(() => currentUser?.role === 'HR')
const canManageEmployees = computed(() => currentUser?.role === 'Admin' || currentUser?.role === 'HR')

const filteredEmployees = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return employees.value.filter((employee) => {
    const fullName = [
      employee.lastName,
      employee.firstName,
      employee.middleName
    ].filter(Boolean).join(' ').toLowerCase()

    const matchesQuery = !query || fullName.includes(query)
    const matchesDepartment = !departmentFilter.value || employee.department === departmentFilter.value

    return matchesQuery && matchesDepartment
  })
})

const departmentsForFilter = computed(() => departments.value.filter(d => d.name !== 'Administration'))
const departmentsForEmployeeForm = computed(() => departments.value.filter(d => d.name !== 'Administration'))

async function loadEmployees() {
  employees.value = await apiFetch('/api/employees')
}

async function createDepartment() {
  error.value = ''
  success.value = ''

  try {
    await apiFetch('/api/departments', {
      method: 'POST',
      body: JSON.stringify({
        name: departmentForm.name
      })
    })
    success.value = 'Отдел создан'
    departmentForm.name = ''
    await loadDepartments()
  } catch (e) {
    error.value = e.message
  }
}

async function createEmployee() {
  error.value = ''
  success.value = ''
  isSaving.value = true

  try {
    await apiFetch('/api/employees', {
      method: 'POST',
      body: JSON.stringify({
        ...form,
        hireDate: form.hireDate || null
      })
    })
    success.value = 'Сотрудник создан'
    Object.assign(form, {
      lastName: '',
      firstName: '',
      middleName: '',
      departmentId: 0,
      positionId: 0,
      email: '',
      hireDate: '',
      password: '',
      role: 'Employee'
    })
    positions.value = []
    await loadEmployees()
  } catch (e) {
    error.value = e.message
  } finally {
    isSaving.value = false
  }
}

async function createPosition() {
  error.value = ''
  success.value = ''
  isPositionSaving.value = true

  try {
    await apiFetch('/api/positions', {
      method: 'POST',
      body: JSON.stringify({
        name: positionForm.name,
        departmentId: positionForm.departmentId
      })
    })
    success.value = 'Должность создана'
    positionForm.name = ''
    positionForm.departmentId = 0
    await loadDepartments()
    if (form.departmentId) {
      await loadPositionsForDepartment(form.departmentId)
    }
  } catch (e) {
    error.value = e.message
  } finally {
    isPositionSaving.value = false
  }
}

async function loadDepartments() {
  departments.value = await apiFetch('/api/departments')
  if (form.departmentId) {
    await loadPositionsForDepartment(form.departmentId)
  }
}

async function loadPositionsForDepartment(departmentId) {
  if (!departmentId) {
    positions.value = []
    return
  }

  positions.value = await apiFetch(`/api/positions/by-department/${departmentId}`)
}

watch(
  () => form.departmentId,
  async (departmentId) => {
    form.positionId = 0
    positions.value = []
    if (departmentId) {
      await loadPositionsForDepartment(departmentId)
    }
  }
)

async function toggleActive(employee) {
  const action = employee.isActive ? 'Заморозить' : 'Разморозить'
  if (!confirm(`${action} сотрудника ${employee.lastName} ${employee.firstName}?`)) {
    return
  }

  error.value = ''
  success.value = ''
  deletingId.value = employee.id

  try {
    if (employee.isActive) {
      await apiFetch(`/api/employees/${employee.id}/delete`, { method: 'POST' })
      success.value = 'Сотрудник заблокирован'
    } else {
      await apiFetch(`/api/employees/${employee.id}/activate`, { method: 'POST' })
      success.value = 'Сотрудник разморожен'
    }
    await loadEmployees()
  } catch (e) {
    error.value = e.message
  } finally {
    deletingId.value = null
  }
}

function canDelete(employee) {
  const currentUser = getCurrentUser()
  return (currentUser?.role === 'Admin' || currentUser?.role === 'HR') && currentUser.employeeId !== employee.id
}

function roleLabel(role) {
  return roleLabels[role] || role
}

function buttonClass(employee) {
  return employee.isActive ? 'danger' : 'success'
}

function formatDate(value) {
  if (!value) {
    return 'Не указана'
  }

  return new Date(value).toLocaleDateString('ru-RU')
}

onMounted(async () => {
  await Promise.all([loadEmployees(), loadDepartments()])
})
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

.employee-name-link {
  color: #172033;
}

.employee-name-link:hover {
  color: #2653ff;
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

.employee-action--freeze {
  background: #d92d20;
}

.employee-action--unfreeze {
  background: #039855;
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

.section-divider {
  margin-bottom: 32px;
}

.section-divider h2 {
  margin: 0 0 16px 0;
  font-size: 18px;
  font-weight: 600;
}
</style>
