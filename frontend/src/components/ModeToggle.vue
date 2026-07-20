<script setup lang="ts">
import type { VerifyMode } from '../api/types'

defineProps<{ mode: VerifyMode }>()
const emit = defineEmits<{ change: [mode: VerifyMode] }>()

const options: { id: VerifyMode; label: string }[] = [
  { id: 'fast', label: 'Fast Count' },
  { id: 'scan', label: 'Full Scan' }
]
</script>

<template>
  <div class="toggle" role="group" aria-label="Verification workflow">
    <button
      v-for="option in options"
      :key="option.id"
      type="button"
      class="toggle__opt"
      :class="{ 'toggle__opt--on': option.id === mode }"
      :aria-pressed="option.id === mode"
      @click="emit('change', option.id)"
    >
      {{ option.label }}
    </button>
  </div>
</template>

<style scoped>
.toggle {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px;
  padding: 4px;
  background: var(--surface-sunk);
  border: 1px solid var(--line);
  border-radius: var(--radius);
}

.toggle__opt {
  padding: 7px 10px;
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--ink-2);
  font-size: 13px;
  font-weight: 600;
}

.toggle__opt--on {
  background: var(--teal);
  color: #fff;
}
</style>
