<script setup lang="ts">
import type { ManifestSummary } from '../api/types'
import StatusPill from './StatusPill.vue'

defineProps<{ manifests: ManifestSummary[] }>()
const emit = defineEmits<{ open: [id: number] }>()

const dateFormat = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit'
})
</script>

<template>
  <section class="view card">
    <header class="view__head">
      <h1 class="view__title">All manifests</h1>
      <span class="view__count">{{ manifests.length }} total</span>
    </header>

    <div class="wrap">
      <table class="grid">
        <thead>
          <tr>
            <th class="eyebrow">Manifest</th>
            <th class="eyebrow">Clinic</th>
            <th class="eyebrow">Sent</th>
            <th class="eyebrow num">Expected</th>
            <th class="eyebrow num">Received</th>
            <th class="eyebrow">Status</th>
            <th class="eyebrow"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in manifests" :key="m.id">
            <td class="mono code">{{ m.code }}</td>
            <td>{{ m.clinicName }}</td>
            <td class="muted mono">{{ dateFormat.format(new Date(m.sentAt)) }}</td>
            <td class="num mono">{{ m.counts.expected }}</td>
            <td class="num mono">{{ m.counts.received }} / {{ m.counts.total }}</td>
            <td><StatusPill :status="m.status" /></td>
            <td class="open-cell">
              <button type="button" class="link" @click="emit('open', m.id)">Open →</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
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
.wrap {
  overflow-x: auto;
}
.grid {
  width: 100%;
  border-collapse: collapse;
  white-space: nowrap;
}
.grid th,
.grid td {
  text-align: left;
  padding: 10px 12px;
  border-bottom: 1px solid var(--line);
}
.grid th {
  padding-top: 0;
  border-bottom: 1px solid var(--line-strong);
}
.num {
  text-align: right;
}
.code {
  font-weight: 600;
}
.muted {
  color: var(--ink-2);
}
.open-cell {
  text-align: right;
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
