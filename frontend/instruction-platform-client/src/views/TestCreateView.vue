<template>
  <section class="card">
    <h1>{{ isEditing ? 'Редактирование теста' : 'Создание нового теста' }}</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>
    <div v-if="loading" class="info">Загрузка...</div>

    <form v-if="!loading" @submit.prevent="saveTest" class="test-builder">
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

      <div class="field-row">
        <label>Направление</label>
        <select v-model="test.category" required>
          <option v-for="item in instructionCategories" :key="item.value" :value="item.value">
            {{ item.label }}
          </option>
        </select>
      </div>

      <div class="field-row">
        <label>Вид инструктажа</label>
        <select v-model="test.instructionType" required>
          <option v-for="item in instructionTypes" :key="item.value" :value="item.value">
            {{ item.label }}
          </option>
        </select>
      </div>

      <div class="field-row">
        <label>Срок повторного инструктажа (мес.)</label>
        <input type="number" v-model.number="test.retrainingIntervalMonths" min="1" max="60" />
      </div>

      <div class="field-row">
        <label>Обучающий материал (PDF)</label>
        <select v-model="test.trainingMaterialId">
          <option :value="null">Без привязки к материалу</option>
          <option v-for="material in filteredMaterials" :key="material.id" :value="material.id">
            ID {{ material.id }} — {{ material.title }}
          </option>
        </select>
        <small v-if="materials.length > 0 && filteredMaterials.length === 0">
          Нет материалов с выбранным направлением и видом инструктажа
        </small>
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

      <div class="button-row">
        <button type="submit" class="primary create-button">{{ isEditing ? 'Сохранить изменения' : 'Создать тест' }}</button>
        <router-link to="/tests/import" class="button secondary">Отмена</router-link>
      </div>
    </form>
  </section>
</template>

<script setup>
import { reactive, ref, computed, onMounted } from 'vue'
import { apiFetch } from '../api'
import { useRouter, useRoute } from 'vue-router'
import {
  categoryLabel,
  instructionCategories,
  instructionTypes
} from '../instructionLabels'

const router = useRouter()
const route = useRoute()
const error = ref('')
const success = ref('')
const loading = ref(false)

const testId = computed(() => route.params.id ? parseInt(route.params.id) : null)
const isEditing = computed(() => !!testId.value)

const questionIdSeed = ref(1)
const optionIdSeed = ref(1)

const materials = ref([])
const test = reactive({
  title: '',
  description: '',
  passingScorePercent: 80,
  category: 'OccupationalSafety',
  instructionType: 'Repeated',
  retrainingIntervalMonths: 12,
  trainingMaterialId: null,
  questions: []
})

// Фильтруем материалы по категории и виду инструктажа теста
const filteredMaterials = computed(() => {
  return materials.value.filter(
    material =>
      material.category === test.category &&
      material.instructionType === test.instructionType
  )
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

async function loadTest() {
  if (!isEditing.value) return
  
  loading.value = true
  error.value = ''
  
  try {
    const testData = await apiFetch(`/api/tests/${testId.value}`)
    test.title = testData.title
    test.description = testData.description
    test.passingScorePercent = testData.passingScorePercent
    test.category = testData.category
    test.instructionType = testData.instructionType
    test.retrainingIntervalMonths = testData.retrainingIntervalMonths
    test.trainingMaterialId = testData.trainingMaterialId
    test.questions = testData.questions.map((q, qIdx) => ({
      id: qIdx,
      text: q.text,
      type: q.type,
      expectedAnswer: q.expectedAnswer || '',
      options: q.options.map((o, oIdx) => ({
        id: oIdx,
        text: o.text,
        isCorrect: o.isCorrect
      }))
    }))
    questionIdSeed.value = testData.questions.length + 1
  } catch (e) {
    error.value = 'Ошибка при загрузке теста: ' + e.message
  } finally {
    loading.value = false
  }
}

async function saveTest() {
  error.value = ''
  success.value = ''

  try {
    const request = {
      title: test.title,
      description: test.description,
      passingScorePercent: test.passingScorePercent,
      category: test.category,
      instructionType: test.instructionType,
      retrainingIntervalMonths: test.retrainingIntervalMonths,
      trainingMaterialId: test.trainingMaterialId,
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

    if (isEditing.value) {
      // Редактирование
      await apiFetch(`/api/tests/${testId.value}`, {
        method: 'PUT',
        body: JSON.stringify(request)
      })
      success.value = 'Тест обновлён успешно!'
    } else {
      // Создание
      await apiFetch('/api/tests/import-json', {
        method: 'POST',
        body: JSON.stringify(request)
      })
      success.value = 'Тест создан успешно!'
    }

    setTimeout(() => {
      router.push('/tests/import')
    }, 1000)
  } catch (e) {
    error.value = e.message
  }
}

async function loadMaterials() {
  materials.value = await apiFetch('/api/training-materials')
}

onMounted(async () => {
  await loadMaterials()
  if (isEditing.value) {
    await loadTest()
  } else {
    test.questions.push(createQuestion())
  }
})
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

.button-row {
  display: flex;
  gap: 12px;
  margin-top: 12px;
}

.button {
  display: inline-block;
  padding: 10px 16px;
  border-radius: 4px;
  text-decoration: none;
  font-size: 14px;
  cursor: pointer;
}

.secondary {
  background: #f0f0f0;
  border: 1px solid #ddd;
  color: #333;
}

.secondary:hover {
  background: #e8e8e8;
}
</style>
