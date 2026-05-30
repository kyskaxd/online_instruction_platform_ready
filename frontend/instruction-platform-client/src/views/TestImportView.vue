<template>
  <section class="card">
    <h1>Создание теста</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <form @submit.prevent="createTest" class="test-builder">
      <div class="field-row">
        <label>Название теста</label>
        <input v-model="test.title" required />
      </div>

      <div class="field-row">
        <label>Описание</label>
        <textarea v-model="test.description" rows="2"></textarea>
      </div>

      <div class="field-row">
        <label>Проходной балл (%)</label>
        <input type="number" v-model.number="test.passingScorePercent" min="0" max="100" />
      </div>

      <div class="questions">
        <h2>Вопросы</h2>
        <div v-if="test.questions.length === 0" class="empty-state">
          Добавьте первый вопрос.
        </div>

        <article v-for="(question, qIndex) in test.questions" :key="question.id" class="question-card">
          <div class="question-header">
            <h3>Вопрос {{ qIndex + 1 }}</h3>
            <button type="button" class="danger" @click="removeQuestion(qIndex)">Удалить</button>
          </div>

          <div class="field-row">
            <label>Текст вопроса</label>
            <textarea v-model="question.text" rows="2" required></textarea>
          </div>

          <div class="field-row">
            <label>Тип вопроса</label>
            <select v-model="question.type">
              <option value="SingleChoice">Один вариант</option>
              <option value="MultipleChoice">Несколько вариантов</option>
              <option value="Text">Текстовый ответ</option>
            </select>
          </div>

          <div v-if="question.type === 'Text'" class="field-row">
            <label>Правильный ответ</label>
            <input v-model="question.expectedAnswer" placeholder="Введите ожидаемый ответ" />
          </div>

          <div v-if="question.type !== 'Text'" class="options-block">
            <h4>Варианты ответа</h4>
            <div v-for="(option, oIndex) in question.options" :key="option.id" class="option-row">
              <input
                type="text"
                v-model="option.text"
                placeholder="Текст варианта"
                required
              />
              <label class="checkbox-label">
                <input type="checkbox" v-model="option.isCorrect" /> Правильный
              </label>
              <button type="button" class="danger small" @click="removeOption(question, oIndex)">×</button>
            </div>
            <button type="button" class="secondary" @click="addOption(question)">Добавить вариант</button>
          </div>
        </article>

        <button type="button" class="primary" @click="addQuestion">Добавить вопрос</button>
      </div>

      <button type="submit" class="primary create-button">Сохранить тест</button>
    </form>
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
          <td v-if="isAdmin">
            <button class="danger" :disabled="deletingId === testItem.id" @click="deleteTest(testItem)">Удалить</button>
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

const questionIdSeed = ref(1)
const optionIdSeed = ref(1)

const test = reactive({
  title: '',
  description: '',
  passingScorePercent: 80,
  questions: []
})

function createOption() {
  return { id: optionIdSeed.value++, text: '', isCorrect: false }
}

function createQuestion() {
  return {
    id: questionIdSeed.value++,
    text: '',
    type: 'SingleChoice',
    expectedAnswer: '',
    options: [createOption(), createOption()]
  }
}

function addQuestion() {
  test.questions.push(createQuestion())
}

function removeQuestion(index) {
  test.questions.splice(index, 1)
}

function addOption(question) {
  question.options.push(createOption())
}

function removeOption(question, index) {
  if (question.options.length <= 2) {
    return
  }
  question.options.splice(index, 1)
}

async function load() {
  tests.value = await apiFetch('/api/tests')
  departments.value = (await apiFetch('/api/departments'))
    .filter((department) => department.name !== 'Administration')
}

async function createTest() {
  error.value = ''
  success.value = ''

  try {
    const request = {
      title: test.title,
      description: test.description,
      passingScorePercent: test.passingScorePercent,
      questions: test.questions.map((question) => ({
        text: question.text,
        type: question.type,
        expectedAnswer: question.type === 'Text' ? question.expectedAnswer : null,
        options: question.type === 'Text'
          ? []
          : question.options.map((option) => ({
              text: option.text,
              isCorrect: option.isCorrect
            }))
      }))
    }

    await apiFetch('/api/tests/import-json', {
      method: 'POST',
      body: JSON.stringify(request)
    })

    success.value = 'Тест создан.'
    test.title = ''
    test.description = ''
    test.passingScorePercent = 80
    test.questions = []
    await load()
  } catch (e) {
    error.value = e.message
  }
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
    delete assignEmployeeIds[testItem.id]
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
.test-builder {
  display: grid;
  gap: 16px;
}

.field-row {
  display: grid;
  gap: 6px;
}

.questions {
  display: grid;
  gap: 16px;
}

.question-card {
  border: 1px solid #ddd;
  padding: 16px;
  border-radius: 8px;
  background: #fafafa;
}

.question-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.options-block {
  display: grid;
  gap: 12px;
}

.option-row {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: 12px;
  align-items: center;
}

.checkbox-label {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

button.primary,
button.secondary,
button.danger,
button.small {
  cursor: pointer;
}

button.create-button {
  margin-top: 16px;
}

.test-actions,
.assign-select {
  display: flex;
  gap: 12px;
}
</style>
