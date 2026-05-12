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
          <td>{{ item.status }}</td>
          <td>{{ item.lastScorePercent ?? '—' }}</td>
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

async function load() {
  try {
    assignments.value = await apiFetch('/api/tests/my')
  } catch (e) {
    error.value = e.message
  }
}

onMounted(load)
</script>
