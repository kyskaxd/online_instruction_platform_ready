<template>
  <section class="card">
    <h1>Обучающие материалы</h1>
    <div v-if="error" class="error">{{ error }}</div>
    <div v-if="success" class="success">{{ success }}</div>

    <form v-if="isManager" @submit.prevent="upload" class="form-grid">
      <label>Название<input v-model="form.title" required></label>
      <label>Описание<input v-model="form.description"></label>
      <label>
        Направление
        <select v-model="form.category" required>
          <option v-for="item in instructionCategories" :key="item.value" :value="item.value">
            {{ item.label }}
          </option>
        </select>
      </label>
      <label>
        Вид инструктажа
        <select v-model="form.instructionType" required>
          <option v-for="item in instructionTypes" :key="item.value" :value="item.value">
            {{ item.label }}
          </option>
        </select>
      </label>
      <label>
        Срок повторного инструктажа (мес.)
        <input v-model.number="form.retrainingIntervalMonths" type="number" min="1" max="60" required>
      </label>
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
          <th>Направление</th>
          <th>Файл</th>
          <th v-if="isEmployee">Статус изучения</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="material in materials" :key="material.id">
          <td>
            <b>{{ material.title }}</b><br>
            <small>{{ material.description }}</small>
          </td>
          <td>
            <span class="category-badge">{{ categoryLabel(material.category) }}</span><br>
            <small>{{ instructionTypeLabel(material.instructionType) }}</small>
          </td>
          <td>{{ material.originalFileName }}</td>
          <td v-if="isEmployee">
            <span v-if="material.studyStatus?.isAcknowledged" class="status-text--passed">Ознакомлен</span>
            <span v-else-if="material.studyStatus?.hasViewed" class="status-text--assigned">Просмотрен</span>
            <span v-else>Не изучен</span>
          </td>
          <td>
            <button class="secondary" @click="openPdf(material)">Открыть</button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>

  <section v-if="activeMaterial" class="card">
    <h2>Просмотр: {{ activeMaterial.title }}</h2>
    <p v-if="isEmployee && !activeMaterial.studyStatus?.isAcknowledged">
      Изучите материал и подтвердите ознакомление — без этого тест будет недоступен.
    </p>
    <iframe class="pdf-frame" :src="pdfUrl"></iframe>
    <div v-if="isEmployee" class="ack-row">
      <button
        :disabled="acknowledging"
        @click="acknowledge(activeMaterial.id)"
      >
        {{ activeMaterial.studyStatus?.isAcknowledged ? 'Ознакомление подтверждено' : 'Подтверждаю ознакомление с материалом' }}
      </button>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { apiBlob, apiFetch, currentUser } from '../api'
import {
  categoryLabel,
  instructionCategories,
  instructionTypeLabel,
  instructionTypes
} from '../instructionLabels'

const materials = ref([])
const pdfUrl = ref('')
const activeMaterial = ref(null)
const error = ref('')
const success = ref('')
const acknowledging = ref(false)
const file = ref(null)
const user = currentUser
const isManager = computed(() => ['Admin', 'Manager'].includes(user.value?.role))
const isEmployee = computed(() => user.value?.role === 'Employee')
const form = reactive({
  title: '',
  description: '',
  category: 'OccupationalSafety',
  instructionType: 'Repeated',
  retrainingIntervalMonths: 12
})

function onFileChange(event) {
  file.value = event.target.files[0]
}

async function loadMaterials() {
  materials.value = await apiFetch('/api/training-materials')
}

async function upload() {
  error.value = ''
  success.value = ''
  try {
    const data = new FormData()
    data.append('title', form.title)
    data.append('description', form.description || '')
    data.append('category', form.category)
    data.append('instructionType', form.instructionType)
    data.append('retrainingIntervalMonths', String(form.retrainingIntervalMonths))
    data.append('file', file.value)
    await apiFetch('/api/training-materials', { method: 'POST', body: data })
    form.title = ''
    form.description = ''
    file.value = null
    success.value = 'Материал загружен'
    await loadMaterials()
  } catch (e) {
    error.value = e.message
  }
}

async function openPdf(material) {
  error.value = ''
  try {
    if (isEmployee.value) {
      const status = await apiFetch(`/api/training-materials/${material.id}/view`, { method: 'POST' })
      material.studyStatus = status
    }

    if (pdfUrl.value) URL.revokeObjectURL(pdfUrl.value)
    const blob = await apiBlob(`/api/training-materials/${material.id}/file`)
    pdfUrl.value = URL.createObjectURL(blob)
    activeMaterial.value = material
  } catch (e) {
    error.value = e.message
  }
}

async function acknowledge(materialId) {
  acknowledging.value = true
  error.value = ''
  success.value = ''
  try {
    const status = await apiFetch(`/api/training-materials/${materialId}/acknowledge`, { method: 'POST' })
    const material = materials.value.find((item) => item.id === materialId)
    if (material) {
      material.studyStatus = status
    }
    if (activeMaterial.value?.id === materialId) {
      activeMaterial.value.studyStatus = status
    }
    success.value = 'Ознакомление с материалом подтверждено'
  } catch (e) {
    error.value = e.message
  } finally {
    acknowledging.value = false
  }
}

onMounted(loadMaterials)
</script>

<style scoped>
.category-badge {
  font-weight: 700;
}

.status-text--passed {
  color: #027a48;
  font-weight: 700;
}

.status-text--assigned {
  color: #2653ff;
  font-weight: 700;
}

.ack-row {
  margin-top: 16px;
}
</style>
