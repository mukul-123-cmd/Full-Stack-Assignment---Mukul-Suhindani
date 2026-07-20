<script setup lang="ts">
import type { Tab } from '../composables/useCheckIn'

defineProps<{
  active: Tab
  discrepancyCount: number
}>()

const emit = defineEmits<{ select: [tab: Tab] }>()

const tabs: { id: Tab; label: string }[] = [
  { id: 'checkin', label: 'Check-In' },
  { id: 'history', label: 'Scan History' },
  { id: 'manifests', label: 'Manifests' },
  { id: 'discrepancies', label: 'Discrepancies' }
]
</script>

<template>
  <nav class="tabs" aria-label="Sections">
    <button
      v-for="tab in tabs"
      :key="tab.id"
      type="button"
      class="tabs__tab"
      :class="{ 'tabs__tab--active': tab.id === active }"
      :aria-current="tab.id === active"
      @click="emit('select', tab.id)"
    >
      {{ tab.label }}
      <span v-if="tab.id === 'discrepancies' && discrepancyCount > 0" class="tabs__count">
        {{ discrepancyCount }}
      </span>
    </button>
  </nav>
</template>

<style scoped>
.tabs {
  display: flex;
  gap: 4px;
  padding: 0 20px;
  border-bottom: 1px solid var(--line);
  background: var(--surface);
}

.tabs__tab {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 12px 12px 13px;
  background: transparent;
  border: 0;
  border-bottom: 2px solid transparent;
  color: var(--ink-2);
  font-size: 14px;
  font-weight: 600;
}

.tabs__tab:hover {
  color: var(--ink);
}

.tabs__tab--active {
  color: var(--teal-deep);
  border-bottom-color: var(--teal);
}

.tabs__count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  border-radius: 100px;
  background: var(--flagged-wash);
  color: var(--flagged);
  font-size: 11px;
  font-weight: 700;
}
</style>
