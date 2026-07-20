<script setup lang="ts">
import type { Specimen } from '../api/types'
import StatusPill from './StatusPill.vue'

defineProps<{
  specimens: Specimen[]
  busySpecimenId: number | null
  /** Per-row Receive/Flag are shown only in Full Scan mode on an open manifest. */
  showActions: boolean
}>()

const emit = defineEmits<{
  receive: [specimenId: number]
  flag: [specimenId: number]
}>()

const timeFormat = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' })
</script>

<template>
  <div class="wrap">
    <table class="specimens">
      <caption class="sr-only">Bottles listed on this manifest</caption>
      <thead>
        <tr>
          <th scope="col" class="eyebrow">Status</th>
          <th scope="col" class="eyebrow">Specimen ID</th>
          <th scope="col" class="eyebrow">Patient</th>
          <th scope="col" class="eyebrow">Site</th>
          <th scope="col" class="eyebrow">Provider</th>
          <th scope="col" class="eyebrow">Received by</th>
          <th scope="col" class="eyebrow">At</th>
          <th scope="col" class="eyebrow th-actions">
            <span v-if="showActions">Action</span>
          </th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="s in specimens" :key="s.id">
          <td><StatusPill :status="s.status" /></td>
          <td class="mono cell-code">{{ s.code }}</td>
          <td>{{ s.patient }}</td>
          <td class="cell-muted">{{ s.site }}</td>
          <td class="cell-muted">{{ s.provider }}</td>
          <td class="cell-muted">{{ s.receivedBy ?? '—' }}</td>
          <td class="mono cell-muted">
            {{ s.receivedAt ? timeFormat.format(new Date(s.receivedAt)) : '—' }}
          </td>
          <td class="cell-actions">
            <template v-if="showActions">
              <button
                type="button"
                class="btn btn--primary"
                :disabled="busySpecimenId === s.id || s.status === 'Received'"
                @click="emit('receive', s.id)"
              >
                Receive
              </button>
              <button
                type="button"
                class="btn btn--ghost"
                :disabled="busySpecimenId === s.id || s.status === 'Received' || s.status === 'Flagged'"
                @click="emit('flag', s.id)"
              >
                Flag
              </button>
            </template>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.wrap {
  overflow-x: auto;
}

.specimens {
  width: 100%;
  border-collapse: collapse;
  white-space: nowrap;
}

.specimens th,
.specimens td {
  text-align: left;
  padding: 10px 12px;
  border-bottom: 1px solid var(--line);
  vertical-align: middle;
}

.specimens th {
  padding-top: 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--line-strong);
}

.specimens tbody tr:last-child td {
  border-bottom: 0;
}

.cell-code {
  font-weight: 600;
  font-size: 13px;
}

.cell-muted {
  color: var(--ink-2);
}

.th-actions,
.cell-actions {
  text-align: right;
}

.cell-actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
}

.btn {
  padding: 5px 11px;
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

.btn--ghost {
  background: transparent;
  border-color: var(--line-strong);
  color: var(--ink-2);
}

.btn--ghost:hover:not(:disabled) {
  border-color: var(--flagged);
  color: var(--flagged);
}

.btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
}
</style>
