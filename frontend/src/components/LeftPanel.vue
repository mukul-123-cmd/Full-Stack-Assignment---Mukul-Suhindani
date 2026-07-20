<script setup lang="ts">
import type { ManifestDetail, ManifestSummary, VerifyMode } from '../api/types'
import FastCountPanel from './FastCountPanel.vue'
import ModeToggle from './ModeToggle.vue'

defineProps<{
  manifests: ManifestSummary[]
  selectedId: number | null
  loading: boolean
  search: string
  mode: VerifyMode
  detail: ManifestDetail | null
  countedTotal: number
  countMatches: boolean
}>()

const emit = defineEmits<{
  select: [id: number]
  'update:search': [value: string]
  setMode: [mode: VerifyMode]
  setCount: [value: number]
  viewAll: []
}>()

const dateFormat = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit'
})

/** The state badge each recent-manifest card carries, derived from its counts. */
function badge(m: ManifestSummary): { label: string; tone: string } {
  if (m.counts.openDiscrepancies > 0) {
    const n = m.counts.openDiscrepancies
    return { label: `${n} discrepancy${n > 1 ? '' : ''}`, tone: 'flagged' }
  }
  if (m.status === 'Closed' || m.status === 'ClosedWithDiscrepancy') {
    return { label: 'Received', tone: 'received' }
  }
  if (m.counts.received > 0) return { label: 'In transit', tone: 'blue' }
  return { label: 'Awaiting', tone: 'pending' }
}
</script>

<template>
  <aside class="panel card">
    <section class="panel__section">
      <div class="panel__head">
        <span class="eyebrow">Verification workflow</span>
        <span class="tag">Lab setting</span>
      </div>
      <ModeToggle :mode="mode" @change="emit('setMode', $event)" />
    </section>

    <section class="panel__section">
      <span class="eyebrow">Find manifest</span>
      <div class="find">
        <span class="find__icon" aria-hidden="true">⯐</span>
        <input
          class="find__input mono"
          type="search"
          placeholder="Scan or search manifest…"
          :value="search"
          @input="emit('update:search', ($event.target as HTMLInputElement).value)"
        />
      </div>
    </section>

    <FastCountPanel
      v-if="mode === 'fast' && detail && detail.status === 'Open'"
      :manifest="detail"
      :counted-total="countedTotal"
      :matches="countMatches"
      @set-count="emit('setCount', $event)"
    />

    <section class="panel__section panel__section--list">
      <span class="eyebrow">Recent manifests</span>

      <ol v-if="loading" class="list" aria-busy="true">
        <li v-for="n in 3" :key="n" class="skeleton" />
      </ol>

      <p v-else-if="manifests.length === 0" class="list__empty">
        No manifests match.
      </p>

      <ol v-else class="list">
        <li v-for="m in manifests" :key="m.id">
          <button
            type="button"
            class="row"
            :class="{ 'row--active': m.id === selectedId }"
            :aria-current="m.id === selectedId"
            @click="emit('select', m.id)"
          >
            <span class="row__top">
              <span class="row__code mono">{{ m.code }}</span>
              <span class="badge" :class="`badge--${badge(m).tone}`">{{ badge(m).label }}</span>
            </span>
            <span class="row__clinic">{{ m.clinicName }}</span>
            <span class="row__meta">
              <span>{{ dateFormat.format(new Date(m.sentAt)) }}</span>
              <span aria-hidden="true">·</span>
              <span class="mono">{{ m.counts.received }} / {{ m.counts.total }} received</span>
            </span>
          </button>
        </li>
      </ol>
    </section>

    <button type="button" class="panel__all" @click="emit('viewAll')">View all manifests →</button>
  </aside>
</template>

<style scoped>
.panel {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.panel__section {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
}

.panel__section--list {
  flex: 1;
  min-height: 0;
  overflow: hidden;
  gap: 8px;
}

.panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.tag {
  padding: 2px 7px;
  border: 1px solid var(--line-strong);
  border-radius: 4px;
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.find {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: var(--radius-sm);
  background: var(--surface);
}

.find:focus-within {
  border-color: var(--teal);
  box-shadow: 0 0 0 2px var(--teal-wash);
}

.find__icon {
  color: var(--ink-3);
  font-size: 14px;
}

.find__input {
  flex: 1;
  padding: 8px 0;
  border: 0;
  outline: none;
  background: transparent;
  color: var(--ink);
  font-size: 13px;
}

.list {
  list-style: none;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.list__empty {
  margin: 0;
  padding: 12px 4px;
  color: var(--ink-2);
  font-size: 13px;
}

.row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
  padding: 10px;
  text-align: left;
  background: transparent;
  border: 0;
  border-radius: var(--radius-sm);
  color: inherit;
}

.row:hover {
  background: var(--surface-sunk);
}

.row--active,
.row--active:hover {
  background: var(--teal-wash);
  box-shadow: inset 2px 0 0 var(--teal);
}

.row__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.row__code {
  font-size: 13px;
  font-weight: 600;
}

.row__clinic {
  color: var(--ink-2);
  font-size: 13px;
}

.row__meta {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--ink-3);
}

.badge {
  padding: 2px 8px;
  border-radius: 100px;
  font-size: 11px;
  font-weight: 600;
  white-space: nowrap;
}

.badge--received {
  color: var(--received);
  background: var(--received-wash);
}

.badge--flagged {
  color: var(--flagged);
  background: var(--flagged-wash);
}

.badge--blue {
  color: var(--blue);
  background: var(--blue-wash);
}

.badge--pending {
  color: var(--pending);
  background: var(--pending-wash);
}

.panel__all {
  padding: 12px 16px;
  background: transparent;
  border: 0;
  color: var(--teal-deep);
  font-weight: 600;
  font-size: 13px;
  text-align: left;
}

.panel__all:hover {
  background: var(--surface-sunk);
}

.skeleton {
  height: 62px;
  border-radius: var(--radius-sm);
  background: linear-gradient(90deg, var(--surface-sunk), var(--pending-wash), var(--surface-sunk));
  background-size: 200% 100%;
  animation: shimmer 1.2s linear infinite;
}

@keyframes shimmer {
  to {
    background-position: -200% 0;
  }
}
</style>
