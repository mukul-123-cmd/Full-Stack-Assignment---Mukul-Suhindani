<script setup lang="ts">
import type { Lab } from '../api/types'

defineProps<{
  lab: Lab | null
  labId: number
  techName: string
  labs: { id: number; name: string }[]
}>()

const emit = defineEmits<{ switchLab: [id: number] }>()

function initials(name: string) {
  return name
    .split(' ')
    .map((p) => p[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
}
</script>

<template>
  <header class="ops">
    <div class="ops__brand">
      <span class="ops__mark">IPI</span>
      <span class="ops__badge">UAT</span>
      <span class="ops__field">
        <span class="ops__label">Mode</span>
        <span class="ops__value">Check-In</span>
      </span>
      <span class="ops__divider" aria-hidden="true"></span>
      <span class="ops__field">
        <span class="ops__label">Location</span>
        <span class="ops__value">{{ lab?.name ?? '—' }} · Receiving</span>
      </span>
    </div>

    <div class="ops__identity">
      <label class="ops__lab">
        <span class="ops__label">Signed in as</span>
        <select
          :value="labId"
          @change="emit('switchLab', Number(($event.target as HTMLSelectElement).value))"
        >
          <option v-for="option in labs" :key="option.id" :value="option.id">
            {{ option.name }}
          </option>
        </select>
      </label>
      <span class="ops__tech">{{ techName }}</span>
      <span class="ops__avatar" aria-hidden="true">{{ initials(techName) }}</span>
    </div>
  </header>
</template>

<style scoped>
.ops {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 0 20px;
  height: 52px;
  background: var(--navy);
  color: var(--on-navy);
}

.ops__brand,
.ops__identity {
  display: flex;
  align-items: center;
  gap: 14px;
}

.ops__mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 3px 8px;
  border-radius: 5px;
  background: var(--teal);
  color: #fff;
  font-weight: 700;
  font-size: 13px;
  letter-spacing: 0.02em;
}

.ops__badge {
  padding: 2px 7px;
  border: 1px solid var(--navy-line);
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.06em;
  color: var(--on-navy-dim);
}

.ops__field {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.ops__label {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--on-navy-dim);
}

.ops__value {
  font-size: 13px;
  font-weight: 600;
}

.ops__divider {
  width: 1px;
  height: 20px;
  background: var(--navy-line);
}

.ops__lab {
  display: flex;
  align-items: center;
  gap: 8px;
}

.ops__lab select {
  padding: 4px 8px;
  border: 1px solid var(--navy-line);
  border-radius: var(--radius-sm);
  background: var(--navy-deep);
  color: var(--on-navy);
  font: inherit;
  font-size: 13px;
}

.ops__tech {
  font-size: 13px;
  font-weight: 600;
}

.ops__avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: var(--teal);
  color: #fff;
  font-size: 11px;
  font-weight: 700;
}

@media (max-width: 820px) {
  .ops__brand .ops__field:last-of-type,
  .ops__divider,
  .ops__tech {
    display: none;
  }
}
</style>
