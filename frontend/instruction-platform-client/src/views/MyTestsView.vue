<template>
  <section class="card">
    <h1>Мои тесты</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <table>
      <thead>
        <tr>
          <th>Тест</th>
          <th>Статус</th>
          <th>Балл</th>
          <th>Назначен</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in assignments" :key="item.assignmentId">
          <td><b>{{ item.testTitle }}</b><br><small>{{ item.description }}</small></td>
          <td><span :class="['status-text', statusClass(item.status)]">{{ statusLabel(item.status) }}</span></td>
          <td>{{ item.lastScorePercent ?? '-' }}</td>
          <td>{{ new Date(item.assignedAt).toLocaleString() }}</td>
          <td><router-link :to="`/tests/${item.testId}/take`">Пройти</router-link></td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { apiFetch } from '../api'

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
</style>
