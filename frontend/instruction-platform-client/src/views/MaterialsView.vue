<template>
  <section class="card">
    <h1>Обучающие материалы</h1>
    <div v-if="error" class="error">{{ error }}</div>

    <form v-if="isManager" @submit.prevent="upload" class="form-grid">
      <label>Название<input v-model="form.title" required></label>
      <label>Описание<input v-model="form.description"></label>
      <label>PDF-файл<input type="file" accept="application/pdf" @change="onFileChange" required></label>
      <button>Загрузить PDF</button>
    </form>
  </section>

  <section class="card">
    <h2>Список материалов</h2>
    <table>
      <thead>
        <tr>
          <th>Название</th>
          <th>Файл</th>
          <th>Дата загрузки</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="material in materials" :key="material.id">
          <td>
            <b>{{ material.title }}</b><br>
            <small>{{ material.description }}</small>
          </td>
          <td>{{ material.originalFileName }}</td>
          <td>{{ new Date(material.uploadedAt).toLocaleString() }}</td>
          <td><button class="secondary" @click="openPdf(material.id)">Открыть</button></td>
        </tr>
      </tbody>
    </table>
  </section>

  <section v-if="pdfUrl" class="card">
    <h2>Просмотр PDF</h2>
    <iframe class="pdf-frame" :src="pdfUrl"></iframe>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { apiBlob, apiFetch, currentUser } from '../api'

const materials = ref([])
const pdfUrl = ref('')
const error = ref('')
const file = ref(null)
const user = currentUser
const isManager = computed(() => ['Admin', 'Manager'].includes(user.value?.role))
const form = reactive({ title: '', description: '' })

function onFileChange(event) {
  file.value = event.target.files[0]
}

async function loadMaterials() {
  materials.value = await apiFetch('/api/training-materials')
}

async function upload() {
  error.value = ''
  try {
    const data = new FormData()
    data.append('title', form.title)
    data.append('description', form.description || '')
    data.append('file', file.value)
    await apiFetch('/api/training-materials', { method: 'POST', body: data })
    form.title = ''
    form.description = ''
    file.value = null
    await loadMaterials()
  } catch (e) {
    error.value = e.message
  }
}

async function openPdf(id) {
  error.value = ''
  try {
    if (pdfUrl.value) URL.revokeObjectURL(pdfUrl.value)
    const blob = await apiBlob(`/api/training-materials/${id}/file`)
    pdfUrl.value = URL.createObjectURL(blob)
  } catch (e) {
    error.value = e.message
  }
}

onMounted(loadMaterials)
</script>
