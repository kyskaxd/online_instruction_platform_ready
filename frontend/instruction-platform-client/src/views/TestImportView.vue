<template>
  <section class="card">
    <div class="header-row">
      <h1>Тесты</h1>
      <router-link v-if="canManageTests" to="/tests/create" class="button primary">Создать тест</router-link>
    </div>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <div v-if="canManageTests" class="import-panel">
      <div>
        <h2>Импорт теста из JSON</h2>
        <p>Выберите JSON-файл или вставьте содержимое вручную.</p>
      </div>

      <div class="import-grid">
        <label>
          JSON-файл
          <input type="file" accept=".json,application/json" @change="handleJsonFile" />
        </label>

        <label>
          Содержимое JSON
          <textarea
            v-model="jsonText"
            rows="10"
            placeholder='
"title": "Название",
"description": "Описание",
"category": "Категория",
"instructionType": "Тип инструктажа (Repeated, Primary, Specialized)",
"retrainingIntervalMonths": Срок повторного прохождения,
"passingScorePercent": Процент необходимый для прохождения,
"trainingMaterialId": id обучающего материала (если есть),
"questions": [
  {
    "text": "Вопрос",
    "type": "тип вопроса (SingleChoice, MultipleChoice, OpenEnded)",
    "options": [
      {
        "text": "Ответ",
        "isCorrect": тип ответа (true - правильный, false - неправильный)
      }
    ]
  },'
          ></textarea>
        </label>
      </div>

      <div class="import-actions">
        <button class="primary" :disabled="importing || !jsonText.trim()" @click="importJson">
          {{ importing ? 'Импорт...' : 'Импортировать JSON' }}
        </button>
        <button type="button" class="secondary" :disabled="importing || !jsonText" @click="clearJson">
          Очистить
        </button>
      </div>
    </div>

    <div class="tests-header">
      <h2>Список тестов</h2>
      <label class="category-filter">
        Направление
        <select v-model="selectedCategory">
          <option value="">Все направления</option>
          <option v-for="item in instructionCategories" :key="item.value" :value="item.value">
            {{ item.label }}
          </option>
        </select>
      </label>
    </div>

    <table>
      <thead>
        <tr>
          <th>Тест</th>
          <th>Создал</th>
          <th>Направление</th>
          <th>Вопросов</th>
          <th>Проходной балл</th>
          <th v-if="canAssignTests">Назначить</th>
          <th v-if="canManageTests || canDeleteTests"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="filteredTests.length === 0">
          <td :colspan="tableColumnCount" class="empty-row">Тесты не найдены.</td>
        </tr>
        <tr v-for="testItem in filteredTests" :key="testItem.id">
          <td><b>{{ testItem.title }}</b><br /><small>{{ testItem.description }}</small></td>
          
          <td>
            {{ categoryLabel(testItem.category) }}<br>
            <small>{{ instructionTypeLabel(testItem.instructionType) }}</small>
          </td>
          <td>{{ testItem.questionsCount }}</td>
          <td>{{ testItem.passingScorePercent }}%</td>
          <td v-if="canAssignTests">
            <select v-model="assignDepartmentIds[testItem.id]" multiple class="assign-select">
              <option v-for="department in departments" :key="department.id" :value="department.id">
                {{ department.name }}
              </option>
            </select>
            <br />
            <label class="assign-deadline">
              Срок прохождения
              <input v-model="assignDeadlines[testItem.id]" type="date">
            </label>
            <button style="margin-top: 10px;" class="secondary assign-button" @click="assign(testItem.id)">Назначить выбранным отделам</button>
          </td>
          <td v-if="canManageTests || canDeleteTests">
            <div class="action-buttons">
              <router-link v-if="canManageTests" :to="`/tests/${testItem.id}/edit`" class="action-btn edit">Редактировать</router-link>
              <button v-if="canDeleteTests" class="action-btn danger" :disabled="deletingId === testItem.id" @click="deleteTest(testItem)">Удалить</button>
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
import { categoryLabel, instructionCategories, instructionTypeLabel } from '../instructionLabels'

const tests = ref([])
const departments = ref([])
const error = ref('')
const success = ref('')
const deletingId = ref(null)
const importing = ref(false)
const jsonText = ref('')
const selectedCategory = ref('')
const assignDepartmentIds = reactive({})
const assignDeadlines = reactive({})
const currentRole = computed(() => getCurrentUser()?.role)
const canAssignTests = computed(() => ['Admin', 'HR'].includes(currentRole.value))
const canManageTests = computed(() => ['Admin', 'HR'].includes(currentRole.value))
const canDeleteTests = computed(() => currentRole.value === 'Admin')
const tableColumnCount = computed(() => 5 + (canAssignTests.value ? 1 : 0) + (canManageTests.value || canDeleteTests.value ? 1 : 0))

const filteredTests = computed(() => {
  if (!selectedCategory.value) {
    return tests.value
  }

  return tests.value.filter((testItem) => testItem.category === selectedCategory.value)
})

async function load() {
  tests.value = await apiFetch('/api/tests')
  if (canAssignTests.value) {
    departments.value = (await apiFetch('/api/departments'))
      .filter((department) => department.name !== 'Administration')
  } else {
    departments.value = []
  }
}

async function assign(testId) {
  error.value = ''
  success.value = ''
  try {
    const ids = (assignDepartmentIds[testId] || []).map(Number)
    const testItem = tests.value.find((item) => item.id === testId)
    await apiFetch(`/api/tests/${testId}/assign`, {
      method: 'POST',
      body: JSON.stringify({
        departmentIds: ids,
        deadline: assignDeadlines[testId] || null,
        instructionType: testItem?.instructionType || 'Repeated'
      })
    })
    success.value = 'Тест назначен выбранным отделам'
  } catch (e) {
    error.value = e.message
  }
}

function clearJson() {
  jsonText.value = ''
}

async function handleJsonFile(event) {
  error.value = ''
  success.value = ''

  const file = event.target.files?.[0]
  if (!file) {
    return
  }

  try {
    jsonText.value = await file.text()
  } catch {
    error.value = 'Не удалось прочитать JSON-файл'
  } finally {
    event.target.value = ''
  }
}

async function importJson() {
  error.value = ''
  success.value = ''

  let payload
  try {
    payload = JSON.parse(jsonText.value)
  } catch {
    error.value = 'JSON содержит ошибку. Проверьте формат файла.'
    return
  }

  importing.value = true

  try {
    const createdTest = await apiFetch('/api/tests/import-json', {
      method: 'POST',
      body: JSON.stringify(payload)
    })
    success.value = `Тест "${createdTest.title}" импортирован`
    clearJson()
    await load()
  } catch (e) {
    error.value = e.message
  } finally {
    importing.value = false
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
    await apiFetch(`/api/tests/${testItem.id}/delete`, { method: 'POST' })
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

.import-panel {
  display: grid;
  gap: 16px;
  border: 1px solid #e6e9f2;
  border-radius: 12px;
  padding: 16px;
  margin-bottom: 24px;
  background: #f8faff;
}

.import-panel h2 {
  margin: 0 0 6px;
}

.import-panel p {
  margin: 0;
  color: #667085;
}

.import-grid {
  display: grid;
  gap: 14px;
}

.import-actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.tests-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.tests-header h2 {
  margin: 0;
}

.category-filter {
  min-width: 260px;
}

.empty-row {
  color: #667085;
  text-align: center;
}

.creator-id {
  display: inline-flex;
  min-width: 36px;
  padding: 4px 8px;
  border-radius: 8px;
  background: #edf0ff;
  color: #2653ff;
  font-weight: 800;
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

.assign-deadline {
  display: block;
  margin-top: 8px;
}
</style>
