<script setup lang="ts">
import { onMounted } from 'vue'
import AppTabs from './components/AppTabs.vue'
import DiscrepanciesView from './components/DiscrepanciesView.vue'
import ErrorBanner from './components/ErrorBanner.vue'
import LeftPanel from './components/LeftPanel.vue'
import ManifestDetail from './components/ManifestDetail.vue'
import ManifestsView from './components/ManifestsView.vue'
import TopBar from './components/TopBar.vue'
import type { Tab } from './composables/useCheckIn'
import { useCheckIn } from './composables/useCheckIn'
import type { VerifyMode } from './api/types'

const {
  lab,
  labId,
  techName,
  manifests,
  filteredManifests,
  discrepancyManifests,
  openDiscrepancyTotal,
  detail,
  selectedId,
  loadingWorklist,
  loadingDetail,
  busySpecimenId,
  closing,
  error,
  activeTab,
  mode,
  search,
  countedTotal,
  countMatchesExpected,
  setCountedTotal,
  loadWorklist,
  selectManifest,
  receive,
  flag,
  close,
  markReceivedAndClose,
  switchLab,
  dismissError
} = useCheckIn()

// Two seeded labs. The switcher exists so tenant isolation is visible, not just asserted.
const labs = [
  { id: 1, name: 'Northgate Pathology' },
  { id: 2, name: 'Ridgeview Diagnostics' }
]

function goTab(tab: Tab) {
  activeTab.value = tab
}
function setSearch(value: string) {
  search.value = value
}
function setMode(next: VerifyMode) {
  mode.value = next
}
function reviewDiscrepancy(id: number) {
  mode.value = 'scan'
  selectManifest(id)
}

onMounted(loadWorklist)
</script>

<template>
  <div class="shell">
    <TopBar :lab="lab" :lab-id="labId" :tech-name="techName" :labs="labs" @switch-lab="switchLab" />

    <AppTabs :active="activeTab" :discrepancy-count="openDiscrepancyTotal" @select="goTab" />

    <main class="body">
      <ErrorBanner v-if="error" class="body__error" :error="error" @dismiss="dismissError" />

      <div v-if="activeTab === 'checkin'" class="checkin">
        <LeftPanel
          class="checkin__left"
          :manifests="filteredManifests"
          :selected-id="selectedId"
          :loading="loadingWorklist"
          :search="search"
          :mode="mode"
          :detail="detail"
          :counted-total="countedTotal"
          :count-matches="countMatchesExpected"
          @select="selectManifest"
          @update:search="setSearch"
          @set-mode="setMode"
          @set-count="setCountedTotal"
          @view-all="goTab('manifests')"
        />

        <ManifestDetail
          class="checkin__detail"
          :detail="detail"
          :loading="loadingDetail"
          :mode="mode"
          :count-matches="countMatchesExpected"
          :closing="closing"
          :busy-specimen-id="busySpecimenId"
          @receive="receive"
          @flag="flag"
          @close="close"
          @mark-received-and-close="markReceivedAndClose"
          @switch-to-scan="setMode('scan')"
        />
      </div>

      <ManifestsView
        v-else-if="activeTab === 'manifests'"
        :manifests="manifests"
        @open="selectManifest"
      />

      <DiscrepanciesView
        v-else-if="activeTab === 'discrepancies'"
        :manifests="discrepancyManifests"
        @open="reviewDiscrepancy"
      />

      <section v-else class="history card">
        <h1 class="history__title">Scan history</h1>
        <p class="history__body">
          A per-scan audit trail lives outside this check-in slice. Today the manifest detail
          shows the current state of every bottle — who received it and when — which is the
          part the receiving desk acts on.
        </p>
      </section>

      <p class="footnote">
        Scoped to {{ lab?.name ?? 'this lab' }} by the server. Manifests from other labs are not
        hidden in the client — they are never sent.
      </p>
    </main>
  </div>
</template>

<style scoped>
.shell {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.body {
  flex: 1;
  width: 100%;
  max-width: 1280px;
  margin: 0 auto;
  padding: 16px 20px 24px;
}

.body__error {
  margin-bottom: 14px;
}

.checkin {
  display: grid;
  grid-template-columns: 340px 1fr;
  gap: 16px;
  align-items: start;
}

.checkin__left {
  position: sticky;
  top: 16px;
  max-height: calc(100vh - 140px);
}

.checkin__detail {
  min-height: 360px;
  max-height: calc(100vh - 140px);
}

.history {
  padding: 22px 24px;
}

.history__title {
  margin: 0 0 8px;
  font-size: 18px;
  font-weight: 700;
}

.history__body {
  margin: 0;
  max-width: 60ch;
  color: var(--ink-2);
  font-size: 14px;
  line-height: 1.6;
}

.footnote {
  margin: 16px 2px 0;
  font-size: 12px;
  color: var(--ink-3);
}

@media (max-width: 860px) {
  .checkin {
    grid-template-columns: 1fr;
  }

  .checkin__left,
  .checkin__detail {
    position: static;
    max-height: none;
  }
}
</style>
