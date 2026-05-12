<template>
  <section class="card">
    <h1>Тестирование</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <h2>Импорт теста из JSON</h2>
    <p>Можно вставить JSON вручную или загрузить файл. Пример лежит в папке <code>docs/sample-test-import.json</code>.</p>
    <textarea v-model="jsonText"></textarea>
    <div style="display:flex; gap:12px; margin-top:12px; flex-wrap:wrap;">
      <button @click="importFromText">Импортировать из текста</button>
      <label style="display:block; max-width:320px;">
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
        </tr>
      </thead>
      <tbody>
        <tr v-for="test in tests" :key="test.id">
          <td><b>{{ test.title }}</b><br><small>{{ test.description }}</small></td>
          <td>{{ test.questionsCount }}</td>
          <td>{{ test.passingScorePercent }}%</td>
          <td>
            <select v-model="assignEmployeeIds[test.id]" multiple style="min-width:220px; min-height:90px;">
              <option v-for="employee in employees" :key="employee.id" :value="employee.id">
                {{ employee.lastName }} {{ employee.firstName }} — {{ employee.department }}
              </option>
            </select>
            <br>
            <button class="secondary" style="margin-top:8px;" @click="assign(test.id)">Назначить выбранным</button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { apiFetch } from '../api'

const tests = ref([])
const employees = ref([])
const error = ref('')
const success = ref('')
const assignEmployeeIds = reactive({})
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
  employees.value = await apiFetch('/api/employees')
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
    const data = new FormData()
    data.append('file', event.target.files[0])
    await apiFetch('/api/tests/import-file', { method: 'POST', body: data })
    success.value = 'Тест импортирован из файла'
    await load()
  } catch (e) {
    error.value = e.message
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

onMounted(load)
</script>
