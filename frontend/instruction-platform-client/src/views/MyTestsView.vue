<template>
  <section class="card">
    <h1>Мои инструктажи</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <table>
      <thead>
        <tr>
          <th>Инструктаж</th>
          <th>Направление</th>
          <th>Статус</th>
          <th>Материал</th>
          <th>Лучший балл</th>
          <th>Следующий инструктаж</th>
          <th>Попытки</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in assignments" :key="item.assignmentId">
          <td>
            <b>{{ item.testTitle }}</b><br>
            <small>{{ instructionTypeLabel(item.instructionType) }}</small>
          </td>
          <td>{{ categoryLabel(item.category) }}</td>
          <td><span :class="['status-text', statusClass(item.status)]">{{ statusLabel(item.status) }}</span></td>
          <td>
            <span v-if="!item.materialStudyRequired">—</span>
            <span v-else-if="item.materialStudyCompleted" class="status-text--passed">Изучен</span>
            <router-link v-else to="/materials">Изучить PDF</router-link>
          </td>
          <td>{{ item.bestScorePercent ?? item.lastScorePercent ?? '-' }}</td>
          <td :class="{ overdue: isRetrainingOverdue(item.nextRetrainingDueAt) }">
            {{ formatDate(item.nextRetrainingDueAt) }}
          </td>
          <td>{{ item.attemptCount }}/{{ item.maxAttempts }}</td>
          <td>
            <router-link :to="`/tests/${item.testId}/result`">Результат</router-link>
            <template v-if="item.canRetake && canTake(item)">
              · <router-link :to="`/tests/${item.testId}/take`">Пройти</router-link>
            </template>
            <template v-else-if="item.canRetake && item.materialStudyRequired && !item.materialStudyCompleted">
              <br><span>Сначала изучите материал</span>
            </template>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { apiFetch } from '../api'
import {
  categoryLabel,
  formatDate,
  instructionTypeLabel,
  isRetrainingOverdue
} from '../instructionLabels'

const assignments = ref([])
const error = ref('')

const statusLabels = {
  Assigned: 'Назначен',
  InProgress: 'В процессе',
  Passed: 'Пройден',
  Failed: 'Не пройден'
}

async function load() {
  try {
    assignments.value = await apiFetch('/api/tests/my')
  } catch (e) {
    error.value = e.message
  }
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

function canTake(item) {
  return !item.materialStudyRequired || item.materialStudyCompleted
}

onMounted(load)
</script>

<style scoped>
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

.overdue {
  color: #b42318;
  font-weight: 700;
}
</style>
