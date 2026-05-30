<template>
  <section class="card" v-if="test">
    <h1>{{ test.title }}</h1>
    <p>{{ test.description }}</p>
    <p>Проходной балл: <b>{{ test.passingScorePercent }}%</b></p>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="result" class="success">
      Результат: {{ result.scorePercent }}%. Правильных ответов: {{ result.correctAnswers }} из {{ result.totalQuestions }}.
      <b>{{ result.isPassed ? 'Сдано' : 'Не сдано' }}</b>
    </div>

    <form @submit.prevent="submit">
      <article v-for="question in test.questions" :key="question.id" class="question">
        <h3>{{ question.text }}</h3>

        <div v-if="question.type === 'Text'" class="field-row">
          <label>Ваш ответ</label>
          <textarea v-model="answers[question.id]" rows="3" required></textarea>
        </div>

        <div v-else class="options">
          <label v-for="option in question.options" :key="option.id" class="option-row">
            <input
              v-if="question.type === 'SingleChoice'"
              type="radio"
              :name="`q-${question.id}`"
              :value="option.id"
              v-model="answers[question.id]"
            />
            <input
              v-else
              type="checkbox"
              :value="option.id"
              v-model="answers[question.id]"
            />
            {{ option.text }}
          </label>
        </div>
      </article>
      <button :disabled="!!result">Завершить тест</button>
    </form>
  </section>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { apiFetch } from '../api'

const route = useRoute()
const test = ref(null)
const result = ref(null)
const error = ref('')
const answers = reactive({})

async function load() {
  error.value = ''
  try {
    test.value = await apiFetch(`/api/tests/${route.params.id}/take`)
    for (const question of test.value.questions) {
      answers[question.id] = question.type === 'SingleChoice' ? null : question.type === 'Text' ? '' : []
    }
  } catch (e) {
    error.value = e.message
  }
}

async function submit() {
  error.value = ''
  try {
    const payload = {
      answers: test.value.questions.map((question) => ({
        questionId: question.id,
        optionIds: question.type === 'SingleChoice'
          ? (answers[question.id] ? [answers[question.id]] : [])
          : question.type === 'MultipleChoice'
            ? answers[question.id]
            : [],
        answerText: question.type === 'Text' ? answers[question.id] : null
      }))
    }

    result.value = await apiFetch(`/api/tests/${route.params.id}/submit`, {
      method: 'POST',
      body: JSON.stringify(payload)
    })
  } catch (e) {
    error.value = e.message
  }
}

onMounted(load)
</script>
