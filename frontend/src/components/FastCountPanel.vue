<script setup lang="ts">
import { computed } from 'vue'
import type { ManifestDetail } from '../api/types'

const props = defineProps<{
  manifest: ManifestDetail
  countedTotal: number
  matches: boolean
}>()

const emit = defineEmits<{ setCount: [value: number] }>()

const delta = computed(() => props.countedTotal - props.manifest.counts.expected)

const message = computed(() => {
  if (props.matches) return `Matches ${props.manifest.counts.expected} expected — ready to close.`
  if (delta.value > 0) return `${delta.value} more than expected. Switch to Full Scan to reconcile.`
  return `${Math.abs(delta.value)} short of ${props.manifest.counts.expected} expected.`
})
</script>

<template>
  <div class="verify">
    <span class="eyebrow">Verify &amp; receive</span>
    <p class="verify__hint">Total bottles counted by lab tech</p>

    <div class="stepper">
      <button
        type="button"
        class="stepper__btn"
        aria-label="One fewer"
        :disabled="countedTotal <= 0"
        @click="emit('setCount', countedTotal - 1)"
      >
        −
      </button>
      <span class="stepper__value mono">{{ countedTotal }}</span>
      <button
        type="button"
        class="stepper__btn"
        aria-label="One more"
        @click="emit('setCount', countedTotal + 1)"
      >
        +
      </button>
    </div>

    <p class="verify__state" :class="matches ? 'verify__state--ok' : 'verify__state--warn'">
      {{ message }}
    </p>
  </div>
</template>

<style scoped>
.verify {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 14px 16px;
  border-top: 1px solid var(--line);
  border-bottom: 1px solid var(--line);
}

.verify__hint {
  margin: 0;
  color: var(--ink-2);
  font-size: 13px;
}

.stepper {
  display: flex;
  align-items: center;
  gap: 10px;
  align-self: flex-start;
}

.stepper__btn {
  width: 34px;
  height: 34px;
  border: 1px solid var(--line-strong);
  border-radius: var(--radius-sm);
  background: var(--surface);
  color: var(--ink);
  font-size: 20px;
  line-height: 1;
}

.stepper__btn:hover:not(:disabled) {
  border-color: var(--teal);
  color: var(--teal);
}

.stepper__btn:disabled {
  opacity: 0.4;
}

.stepper__value {
  min-width: 40px;
  text-align: center;
  font-size: 22px;
  font-weight: 600;
}

.verify__state {
  margin: 0;
  font-size: 13px;
  font-weight: 600;
}

.verify__state--ok {
  color: var(--received);
}

.verify__state--warn {
  color: var(--flagged);
}
</style>
