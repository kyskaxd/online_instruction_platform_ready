<template>
  <section class="card">
    <div class="header-row">
      <h1>Тесты</h1>
      <router-link to="/tests/create" class="button primary">Создать тест</router-link>
    </div>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <h2>Список тестов</h2>
    <table>
      <thead>
        <tr>
          <th>Тест</th>
          <th>Вопросов</th>
          <th>Проходной балл</th>
          <th>Назначить</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="testItem in tests" :key="testItem.id">
          <td><b>{{ testItem.title }}</b><br /><small>{{ testItem.description }}</small></td>
          <td>{{ testItem.questionsCount }}</td>
          <td>{{ testItem.passingScorePercent }}%</td>
          <td>
            <select v-model="assignDepartmentIds[testItem.id]" multiple class="assign-select">
              <option v-for="department in departments" :key="department.id" :value="department.id">
                {{ department.name }}
              </option>
            </select>
            <br />
            <button class="secondary assign-button" @click="assign(testItem.id)">Назначить выбранным отделам</button>
          </td>
          <td>
            <div class="action-buttons">
              <router-link :to="`/tests/${testItem.id}/edit`" class="action-btn edit">Редактировать</router-link>
              <button class="action-btn danger" :disabled="deletingId === testItem.id" @click="deleteTest(testItem)">Удалить</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { apiFetch, getCurrentUser } from '../api'

const tests = ref([])
const departments = ref([])
const error = ref('')
const success = ref('')
const deletingId = ref(null)
const assignDepartmentIds = reactive({})
const isAdmin = computed(() => getCurrentUser()?.role === 'Admin')

async function load() {
  tests.value = await apiFetch('/api/tests')
  departments.value = (await apiFetch('/api/departments'))
    .filter((department) => department.name !== 'Administration')
}

async function assign(testId) {
  error.value = ''
  success.value = ''
  try {
    const ids = (assignDepartmentIds[testId] || []).map(Number)
    await apiFetch(`/api/tests/${testId}/assign`, {
      method: 'POST',
      body: JSON.stringify({ departmentIds: ids, deadline: null })
    })
    success.value = 'Тест назначен выбранным отделам'
  } catch (e) {
    error.value = e.message
  }
}

async function deleteTest(testItem) {
  if (!confirm(`Удалить тест "${testItem.title}"?`)) {
    return
  }

  error.value = ''
  success.value = ''
  deletingId.value = testItem.id

  try {
    await apiFetch(`/api/tests/${testItem.id}`, { method: 'DELETE' })
    success.value = 'Тест удалён'
    await load()
  } catch (e) {
    error.value = e.message
  } finally {
    deletingId.value = null
  }
}

onMounted(load)
</script>

<style scoped>
.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.button {
  display: inline-block;
  padding: 10px 16px;
  border-radius: 4px;
  text-decoration: none;
  font-size: 14px;
  cursor: pointer;
  border: none;
  font-weight: 500;
}

.primary {
  background: #007bff;
  color: white;
}

.primary:hover {
  background: #0056b3;
}

.action-buttons {
  display: flex;
  gap: 8px;
}

.action-btn {
  display: inline-block;
  padding: 10px 16px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  text-decoration: none;
  transition: all 0.2s;
  color: white;
}

.action-btn.edit {
  background: #007bff;
}

.action-btn.edit:hover {
  background: #0056b3;
}

.action-btn.danger {
  background: #dc3545;
}

.action-btn.danger:hover:not(:disabled) {
  background: #c82333;
}

.action-btn.danger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
