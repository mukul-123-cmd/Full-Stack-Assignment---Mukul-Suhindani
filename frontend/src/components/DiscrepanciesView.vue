<script setup lang="ts">
import type { ManifestSummary } from '../api/types'

defineProps<{ manifests: ManifestSummary[] }>()
const emit = defineEmits<{ open: [id: number] }>()
</script>

<template>
  <section class="view card">
    <header class="view__head">
      <h1 class="view__title">Open discrepancies</h1>
      <span class="view__count">{{ manifests.length }} manifest(s) affected</span>
    </header>

    <p v-if="manifests.length === 0" class="empty">
      No open discrepancies. Every flagged bottle has been resolved.
    </p>

    <ul v-else class="list">
      <li v-for="m in manifests" :key="m.id" class="item">
        <div class="item__main">
          <span class="mono item__code">{{ m.code }}</span>
          <span class="item__clinic">{{ m.clinicName }}</span>
        </div>
        <span class="item__count">
          {{ m.counts.openDiscrepancies }} missing of {{ m.counts.total }}
        </span>
        <button type="button" class="link" @click="emit('open', m.id)">Review →</button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.view {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 18px 20px;
  overflow-y: auto;
}
.view__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-bottom: 8px;
}
.view__title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.view__count {
  color: var(--ink-3);
  font-size: 13px;
}
.empty {
  padding: 24px 4px;
  color: var(--ink-2);
  font-size: 14px;
}
.list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}
.item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 4px;
  border-bottom: 1px solid var(--line);
}
.item__main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
}
.item__code {
  font-weight: 600;
  font-size: 14px;
}
.item__clinic {
  color: var(--ink-2);
  font-size: 13px;
}
.item__count {
  color: var(--flagged);
  font-size: 13px;
  font-weight: 600;
}
.link {
  background: transparent;
  border: 0;
  color: var(--teal-deep);
  font-weight: 600;
  font-size: 13px;
}
.link:hover {
  text-decoration: underline;
}
</style>
