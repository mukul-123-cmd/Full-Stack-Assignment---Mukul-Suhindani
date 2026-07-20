<script setup lang="ts">
import { computed } from 'vue'
import type { ManifestDetail, VerifyMode } from '../api/types'
import StatCards from './StatCards.vue'
import SpecimenTable from './SpecimenTable.vue'

const props = defineProps<{
  detail: ManifestDetail | null
  loading: boolean
  mode: VerifyMode
  countMatches: boolean
  closing: boolean
  busySpecimenId: number | null
}>()

const emit = defineEmits<{
  receive: [specimenId: number]
  flag: [specimenId: number]
  close: []
  markReceivedAndClose: []
  switchToScan: []
}>()

const dateFormat = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
})

const isOpen = computed(() => props.detail?.status === 'Open')
const showRowActions = computed(() => props.mode === 'scan' && isOpen.value)
</script>

<template>
  <section class="detail card">
    <div v-if="loading && !detail" class="placeholder">Loading manifest…</div>

    <div v-else-if="!detail" class="placeholder">
      Select a manifest from the worklist to begin check-in.
    </div>

    <template v-else>
      <header class="detail__head">
        <div class="detail__title">
          <h1 class="detail__code mono">Manifest {{ detail.code }}</h1>
          <span class="mode-badge" :class="`mode-badge--${mode}`">
            {{ mode === 'fast' ? 'Fast Count' : 'Full Scan' }}
          </span>
        </div>

        <div v-if="isOpen" class="detail__actions">
          <button
            type="button"
            class="btn btn--danger-ghost"
            title="Switch to Full Scan to flag a specific bottle"
            @click="emit('switchToScan')"
          >
            Flag discrepancy
          </button>

          <button
            v-if="mode === 'fast'"
            type="button"
            class="btn btn--primary"
            :disabled="!countMatches || closing"
            @click="emit('markReceivedAndClose')"
          >
            {{ closing ? 'Closing…' : 'Mark Received & Close' }}
          </button>

          <button
            v-else
            type="button"
            class="btn btn--primary"
            :disabled="!detail.counts.canClose || closing"
            @click="emit('close')"
          >
            {{ closing ? 'Closing…' : 'Close manifest' }}
          </button>
        </div>
      </header>

      <p class="detail__sub">
        From {{ detail.clinicName }}
        <span aria-hidden="true">·</span> Sent {{ dateFormat.format(new Date(detail.sentAt)) }}
        <span aria-hidden="true">·</span> {{ detail.counts.expected }} specimens expected
        <span aria-hidden="true">·</span> {{ detail.courier }}
      </p>

      <StatCards :counts="detail.counts" />

      <div class="detail__specimens-head">
        <h2 class="detail__section">Specimens on manifest</h2>
        <span class="received-badge mono">{{ detail.counts.received }} received</span>
      </div>

      <p v-if="!isOpen" class="detail__locked">
        This manifest is closed. Check-in actions are locked; it is shown for reference only.
      </p>

      <SpecimenTable
        :specimens="detail.specimens"
        :busy-specimen-id="busySpecimenId"
        :show-actions="showRowActions"
        @receive="emit('receive', $event)"
        @flag="emit('flag', $event)"
      />
    </template>
  </section>
</template>

<style scoped>
.detail {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 18px 20px;
  overflow-y: auto;
}

.placeholder {
  display: grid;
  place-items: center;
  min-height: 240px;
  color: var(--ink-3);
  font-size: 14px;
}

.detail__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

.detail__title {
  display: flex;
  align-items: center;
  gap: 10px;
}

.detail__code {
  margin: 0;
  font-size: 19px;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.mode-badge {
  padding: 2px 9px;
  border-radius: 100px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.03em;
}

.mode-badge--fast {
  color: var(--teal-deep);
  background: var(--teal-wash);
}

.mode-badge--scan {
  color: var(--blue);
  background: var(--blue-wash);
}

.detail__actions {
  display: flex;
  gap: 8px;
}

.btn {
  padding: 8px 14px;
  border-radius: var(--radius-sm);
  font-size: 13px;
  font-weight: 600;
  border: 1px solid transparent;
  transition: background-color 120ms ease, border-color 120ms ease;
}

.btn--primary {
  background: var(--teal);
  color: #fff;
}

.btn--primary:hover:not(:disabled) {
  background: var(--teal-deep);
}

.btn--danger-ghost {
  background: transparent;
  border-color: var(--line-strong);
  color: var(--flagged);
}

.btn--danger-ghost:hover:not(:disabled) {
  border-color: var(--flagged);
  background: var(--flagged-wash);
}

.btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.detail__sub {
  margin: -6px 0 0;
  color: var(--ink-2);
  font-size: 13px;
  line-height: 1.5;
}

.detail__specimens-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 2px;
}

.detail__section {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
}

.received-badge {
  padding: 3px 10px;
  border: 1px solid var(--received);
  border-radius: 100px;
  color: var(--received);
  font-size: 12px;
  font-weight: 600;
}

.detail__locked {
  margin: 0;
  padding: 10px 14px;
  border-radius: var(--radius);
  background: var(--surface-sunk);
  color: var(--ink-2);
  font-size: 13px;
}
</style>
