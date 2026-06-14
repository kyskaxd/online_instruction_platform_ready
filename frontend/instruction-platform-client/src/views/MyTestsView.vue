<template>
  <section class="card">
    <div class="tests-header">
      <h1>Мои инструктажи</h1>
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
        <tr v-if="filteredAssignments.length === 0">
          <td colspan="8" class="empty-row">Инструктажи не найдены.</td>
        </tr>
        <tr v-for="item in filteredAssignments" :key="item.assignmentId">
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
import { computed, onMounted, ref } from 'vue'
import { apiFetch } from '../api'
import {
  categoryLabel,
  formatDate,
  instructionCategories,
  instructionTypeLabel,
  isRetrainingOverdue
} from '../instructionLabels'

const assignments = ref([])
const error = ref('')
const selectedCategory = ref('')

const filteredAssignments = computed(() => {
  if (!selectedCategory.value) {
    return assignments.value
  }

  return assignments.value.filter((item) => item.category === selectedCategory.value)
})

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
.tests-header {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.tests-header h1 {
  margin: 0;
}

.category-filter {
  min-width: 260px;
}

.empty-row {
  color: #667085;
  text-align: center;
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

.overdue {
  color: #b42318;
  font-weight: 700;
}
</style>
