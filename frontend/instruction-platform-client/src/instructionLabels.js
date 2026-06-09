export const instructionCategories = [
  { value: 'FireSafety', label: 'Пожарная безопасность' },
  { value: 'ElectricalSafety', label: 'Электробезопасность' },
  { value: 'OccupationalSafety', label: 'Охрана труда' }
]

export const instructionTypes = [
  { value: 'Introductory', label: 'Вводный' },
  { value: 'Primary', label: 'Первичный на рабочем месте' },
  { value: 'Repeated', label: 'Повторный' },
  { value: 'Unscheduled', label: 'Внеплановый' },
  { value: 'Targeted', label: 'Целевой' }
]

export function categoryLabel(value) {
  return instructionCategories.find((item) => item.value === value)?.label || value
}

export function instructionTypeLabel(value) {
  return instructionTypes.find((item) => item.value === value)?.label || value
}

export function isRetrainingOverdue(dueAt) {
  if (!dueAt) return false
  return new Date(dueAt) < new Date()
}

export function formatDate(value) {
  if (!value) return '—'
  return new Date(value).toLocaleDateString('ru-RU')
}
