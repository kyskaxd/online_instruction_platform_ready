<template>
  <section class="card">
    <h1>Тестирование</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <h2>Импорт теста из JSON</h2>

    <textarea v-model="jsonText"></textarea>
    <div class="test-actions">
      <button @click="importFromText">Импортировать из текста</button>
      <label class="file-field">
        Загрузить JSON-файл
        <input type="file" accept="application/json" @change="importFromFile">
      </label>
    </div>
  </section>

  <section class="card">
    <h2>Список тестов</h2>
    <table>
      <thead>
        <tr>
          <th>Тест</th>
          <th>Вопросов</th>
          <th>Проходной балл</th>
          <th>Назначить</th>
          <th v-if="isAdmin"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="test in tests" :key="test.id">
          <td><b>{{ test.title }}</b><br><small>{{ test.description }}</small></td>
          <td>{{ test.questionsCount }}</td>
          <td>{{ test.passingScorePercent }}%</td>
          <td>
            <select v-model="assignEmployeeIds[test.id]" multiple class="assign-select">
              <option v-for="employee in employees" :key="employee.id" :value="employee.id">
                {{ employee.lastName }} {{ employee.firstName }} - {{ employee.department }}
              </option>
            </select>
            <br>
            <button class="secondary assign-button" @click="assign(test.id)">Назначить выбранным</button>
          </td>
          <td v-if="isAdmin">
            <button
              class="danger"
              :disabled="deletingId === test.id"
              @click="deleteTest(test)"
            >
              Удалить
            </button>
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
const employees = ref([])
const error = ref('')
const success = ref('')
const deletingId = ref(null)
const assignEmployeeIds = reactive({})
const isAdmin = computed(() => getCurrentUser()?.role === 'Admin')
const jsonText = ref(JSON.stringify({
  title: 'Инструктаж по охране труда',
  description: 'Первичный тест после изучения PDF',
  passingScorePercent: 80,
  trainingMaterialId: null,
  questions: [
    {
      text: 'Что нужно сделать при обнаружении пожара?',
      type: 'SingleChoice',
      options: [
        { text: 'Сообщить руководителю и вызвать пожарную охрану', isCorrect: true },
        { text: 'Продолжить работу', isCorrect: false },
        { text: 'Спрятаться', isCorrect: false }
      ]
    }
  ]
}, null, 2))

async function load() {
  tests.value = await apiFetch('/api/tests')
  employees.value = await apiFetch('/api/employees/lookup')
}

async function importFromText() {
  error.value = ''
  success.value = ''
  try {
    const parsed = JSON.parse(jsonText.value)
    await apiFetch('/api/tests/import-json', {
      method: 'POST',
      body: JSON.stringify(parsed)
    })
    success.value = 'Тест импортирован'
    await load()
  } catch (e) {
    error.value = e.message
  }
}

async function importFromFile(event) {
  error.value = ''
  success.value = ''
  try {
    const file = event.target.files[0]
    if (!file) {
      return
    }

    const data = new FormData()
    data.append('file', file)
    await apiFetch('/api/tests/import-file', { method: 'POST', body: data })
    success.value = 'Тест импортирован из файла'
    await load()
  } catch (e) {
    error.value = e.message
  } finally {
    event.target.value = ''
  }
}

async function assign(testId) {
  error.value = ''
  success.value = ''
  try {
    const ids = (assignEmployeeIds[testId] || []).map(Number)
    await apiFetch(`/api/tests/${testId}/assign`, {
      method: 'POST',
      body: JSON.stringify({ employeeIds: ids, deadline: null })
    })
    success.value = 'Тест назначен выбранным сотрудникам'
  } catch (e) {
    error.value = e.message
  }
}

async function deleteTest(test) {
  if (!confirm(`Удалить тест "${test.title}"?`)) {
    return
  }

  error.value = ''
  success.value = ''
  deletingId.value = test.id

  try {
    await apiFetch(`/api/tests/${test.id}`, { method: 'DELETE' })
    success.value = 'Тест удалён'
    delete assignEmployeeIds[test.id]
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
.test-actions {
  display: flex;
  gap: 12px;
  margin-top: 12px;
  flex-wrap: wrap;
}

.file-field {
  display: block;
  max-width: 320px;
}

.assign-select {
  min-width: 220px;
  min-height: 90px;
}

.assign-button {
  margin-top: 8px;
}
</style>
