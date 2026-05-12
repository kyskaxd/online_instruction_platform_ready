<template>
  <section class="card">
    <h1>Отчётность</h1>
    <div v-if="error" class="error">{{ error }}</div>

    <div class="form-grid">
      <label>
        Отдел
        <input v-model="filters.department" placeholder="Например: IT">
      </label>
      <label>
        Тест
        <select v-model="filters.testId">
          <option value="">Все тесты</option>
          <option v-for="test in tests" :key="test.id" :value="test.id">{{ test.title }}</option>
        </select>
      </label>
      <label>
        Сотрудник
        <select v-model="filters.employeeId">
          <option value="">Все сотрудники</option>
          <option v-for="employee in employees" :key="employee.id" :value="employee.id">
            {{ employee.lastName }} {{ employee.firstName }} - {{ employee.department }}
          </option>
        </select>
      </label>
      <div class="report-actions">
        <button @click="loadReport">Показать</button>
        <button class="secondary" @click="downloadExcel">Скачать Excel</button>
      </div>
    </div>
  </section>

  <section class="card">
    <h2>Результаты</h2>
    <table>
      <thead>
        <tr>
          <th>Сотрудник</th>
          <th>Отдел</th>
          <th>Тест</th>
          <th>Статус</th>
          <th>Балл</th>
          <th>Дата прохождения</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.assignmentId">
          <td>{{ row.employeeFullName }}<br><small>{{ row.position }}</small></td>
          <td>{{ row.department }}</td>
          <td>{{ row.testTitle }}</td>
          <td>{{ row.status }}</td>
          <td>{{ row.scorePercent ?? '-' }}</td>
          <td>{{ row.completedAt ? new Date(row.completedAt).toLocaleString() : '-' }}</td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { apiBlob, apiFetch } from '../api'

const rows = ref([])
const tests = ref([])
const employees = ref([])
const error = ref('')
const filters = reactive({ department: '', testId: '', employeeId: '' })

function buildQuery() {
  const params = new URLSearchParams()
  if (filters.department) params.set('department', filters.department)
  if (filters.testId) params.set('testId', filters.testId)
  if (filters.employeeId) params.set('employeeId', filters.employeeId)
  return params.toString()
}

async function loadDictionaries() {
  tests.value = await apiFetch('/api/tests')
  employees.value = await apiFetch('/api/employees')
}

async function loadReport() {
  error.value = ''
  try {
    const query = buildQuery()
    rows.value = await apiFetch(`/api/reports/test-results${query ? '?' + query : ''}`)
  } catch (e) {
    error.value = e.message
  }
}

async function downloadExcel() {
  error.value = ''
  try {
    const query = buildQuery()
    const blob = await apiBlob(`/api/reports/test-results.xlsx${query ? '?' + query : ''}`)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'test-results.xlsx'
    link.click()
    URL.revokeObjectURL(url)
  } catch (e) {
    error.value = e.message
  }
}

onMounted(async () => {
  await loadDictionaries()
  await loadReport()
})
</script>

<style scoped>
.report-actions {
  display: flex;
  align-items: end;
  gap: 10px;
}
</style>
