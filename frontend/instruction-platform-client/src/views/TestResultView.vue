<template>
  <section class="card">
    <h1>Результат инструктажа</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="result">
      <h2>{{ result.testTitle }}</h2>
      <p>{{ result.description }}</p>
      <dl class="result-info">
        <div>
          <dt>Направление</dt>
          <dd>{{ categoryLabel(result.category) }}</dd>
        </div>
        <div>
          <dt>Вид инструктажа</dt>
          <dd>{{ instructionTypeLabel(result.instructionType) }}</dd>
        </div>
        <div>
          <dt>Статус</dt>
          <dd><span :class="['status-text', statusClass(result.status)]">{{ statusLabel(result.status) }}</span></dd>
        </div>
        <div>
          <dt>Лучший балл</dt>
          <dd>{{ result.bestScorePercent ?? '—' }}% (проходной {{ result.passingScorePercent }}%)</dd>
        </div>
        <div>
          <dt>Следующий инструктаж</dt>
          <dd>{{ formatDate(result.nextRetrainingDueAt) }}</dd>
        </div>
      </dl>

      <h3>Попытки</h3>
      <table>
        <thead>
          <tr>
            <th>№</th>
            <th>Балл</th>
            <th>Результат</th>
            <th>Дата</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="attempt in result.attempts" :key="attempt.attemptId">
            <td>{{ attempt.attemptNumber }}</td>
            <td>{{ attempt.scorePercent }}%</td>
            <td>
              <span :class="attempt.isPassed ? 'status-text--passed' : 'status-text--failed'">
                {{ attempt.isPassed ? 'Сдано' : 'Не сдано' }}
              </span>
            </td>
            <td>{{ new Date(attempt.finishedAt).toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>

      <div class="actions">
        <router-link v-if="result.canRetake" :to="`/tests/${result.testId}/take`" class="button primary">
          Пройти снова
        </router-link>
        <router-link to="/my-tests" class="button secondary">К списку инструктажей</router-link>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { apiFetch } from '../api'
import { categoryLabel, formatDate, instructionTypeLabel } from '../instructionLabels'

const route = useRoute()
const result = ref(null)
const error = ref('')

const statusLabels = {
  Assigned: 'Назначен',
  InProgress: 'В процессе',
  Passed: 'Пройден',
  Failed: 'Не пройден'
}

function statusLabel(status) {
  return statusLabels[status] || status
}

function statusClass(status) {
  return {
    Assigned: 'status-text--assigned',
    InProgress: 'status-text--assigned',
    Passed: 'status-text--passed',
    Failed: 'status-text--failed'
  }[status] || ''
}

async function load() {
  error.value = ''
  try {
    result.value = await apiFetch(`/api/tests/${route.params.id}/result`)
  } catch (e) {
    error.value = e.message
  }
}

onMounted(load)
</script>

<style scoped>
.result-info {
  display: grid;
  gap: 12px;
  margin: 16px 0 24px;
}

.result-info dt {
  font-weight: 700;
}

.result-info dd {
  margin: 0;
}

.actions {
  display: flex;
  gap: 12px;
  margin-top: 20px;
}

.button {
  display: inline-block;
  padding: 10px 16px;
  border-radius: 12px;
  text-decoration: none;
  font-weight: 600;
}

.primary {
  background: #2653ff;
  color: #fff;
}

.secondary {
  background: #e7ebf6;
  color: #172033;
}

.status-text {
  font-weight: 700;
}

.status-text--assigned {
  color: #2653ff;
}

.status-text--passed {
  color: #027a48;
}

.status-text--failed {
  color: #b42318;
}
</style>
