<script setup lang="ts">
import type { ManifestCounts } from '../api/types'

defineProps<{ counts: ManifestCounts }>()
</script>

<template>
  <div class="stats">
    <div class="stat">
      <span class="stat__label">Expected</span>
      <span class="stat__value mono">{{ counts.expected }}</span>
    </div>
    <div class="stat">
      <span class="stat__label">Received</span>
      <span class="stat__value stat__value--received mono">{{ counts.received }}</span>
    </div>
    <div class="stat">
      <span class="stat__label">Pending</span>
      <span class="stat__value mono">{{ counts.pending }}</span>
    </div>
    <div class="stat">
      <span class="stat__label">Flagged</span>
      <span class="stat__value stat__value--flagged mono">{{ counts.flagged }}</span>
    </div>
  </div>
</template>

<style scoped>
.stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 12px 8px;
  border: 1px solid var(--line);
  border-radius: var(--radius);
  background: var(--surface);
}

.stat__label {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.stat__value {
  font-size: 26px;
  font-weight: 600;
  letter-spacing: -0.02em;
}

.stat__value--received {
  color: var(--received);
}

.stat__value--flagged {
  color: var(--flagged);
}

@media (max-width: 520px) {
  .stats {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>
