<template>
  <section class="grid">
    <article class="card">
      <h2>Обучение</h2>
      <p>Обучающий материал необходимый для прохождения тестирования.</p>
      <router-link to="/materials">Открыть материалы</router-link>
    </article>

    <article class="card">
      <h2>Тестирование</h2>
      <p>Тесты с вопросами и вариантами ответов.</p>
      <router-link v-if="isManager" to="/tests/import">Управление тестами</router-link>
      <router-link v-else to="/my-tests">Мои тесты</router-link>
    </article>

    <article class="card" v-if="isManager">
      <h2>Отчётность</h2>
      <p>Отчёт показывает сотрудников, статус, балл и дату прохождения.</p>
      <router-link to="/reports">Открыть отчёты</router-link>
    </article>
  </section>
</template>

<script setup>
import { computed } from 'vue'
import { currentUser } from '../api'

const user = currentUser
const isManager = computed(() => ['Admin', 'Manager'].includes(user.value?.role))
</script>
