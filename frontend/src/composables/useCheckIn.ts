import { computed, ref, watch } from 'vue'
import { api, getLabId, getTechName, setLabId } from '../api/client'
import {
  ApiError,
  type Lab,
  type ManifestDetail,
  type ManifestSummary,
  type VerifyMode
} from '../api/types'

export type Tab = 'checkin' | 'manifests' | 'discrepancies' | 'history'

/**
 * One store for the whole screen. Every mutating endpoint returns the full manifest, so the
 * client never recomputes counts or "ready to close" locally — the server is the only place
 * those rules live, and the two can therefore never disagree.
 */
export function useCheckIn() {
  const lab = ref<Lab | null>(null)
  const manifests = ref<ManifestSummary[]>([])
  const detail = ref<ManifestDetail | null>(null)

  const selectedId = ref<number | null>(null)
  const loadingWorklist = ref(false)
  const loadingDetail = ref(false)
  const busySpecimenId = ref<number | null>(null)
  const closing = ref(false)
  const error = ref<ApiError | null>(null)

  const labId = ref(getLabId())
  const techName = ref(getTechName())

  const activeTab = ref<Tab>('checkin')
  const mode = ref<VerifyMode>('fast')
  const search = ref('')

  // The technician's physical tally for Fast Count. Seeded from the manifest's expected
  // count when one is opened, then adjusted with the stepper.
  const countedTotal = ref(0)

  const isEmpty = computed(() => !loadingWorklist.value && manifests.value.length === 0)

  const filteredManifests = computed(() => {
    const q = search.value.trim().toLowerCase()
    if (!q) return manifests.value
    return manifests.value.filter(
      (m) => m.code.toLowerCase().includes(q) || m.clinicName.toLowerCase().includes(q)
    )
  })

  const discrepancyManifests = computed(() =>
    manifests.value.filter((m) => m.counts.openDiscrepancies > 0)
  )

  const openDiscrepancyTotal = computed(() =>
    manifests.value.reduce((sum, m) => sum + m.counts.openDiscrepancies, 0)
  )

  // Fast Count reconciles only on an exact match, mirroring the server's rule so the button
  // state and the endpoint never disagree.
  const countMatchesExpected = computed(
    () => !!detail.value && countedTotal.value === detail.value.counts.expected
  )

  // Reopening a manifest resets the tally to what the clinic declared.
  watch(detail, (d) => {
    if (d) countedTotal.value = d.counts.expected
  })

  function dismissError() {
    error.value = null
  }

  function setCountedTotal(value: number) {
    countedTotal.value = Math.max(0, value)
  }

  async function guard<T>(fn: () => Promise<T>): Promise<T | null> {
    try {
      error.value = null
      return await fn()
    } catch (e) {
      error.value = e instanceof ApiError ? e : new ApiError('unknown_error', String(e), 0)
      return null
    }
  }

  async function loadWorklist() {
    loadingWorklist.value = true
    await guard(async () => {
      lab.value = await api.currentLab()
      manifests.value = await api.listManifests()

      const stillThere = manifests.value.some((m) => m.id === selectedId.value)
      const next = stillThere
        ? selectedId.value
        : (manifests.value.find((m) => m.status === 'Open') ?? manifests.value[0])?.id ?? null

      if (next !== null) await selectManifest(next)
      else detail.value = null
    })
    loadingWorklist.value = false
  }

  async function selectManifest(id: number) {
    selectedId.value = id
    activeTab.value = 'checkin'
    loadingDetail.value = true
    const result = await guard(() => api.getManifest(id))
    if (result) detail.value = result
    loadingDetail.value = false
  }

  /** Mutations replace the detail wholesale and patch the matching worklist row. */
  function apply(updated: ManifestDetail) {
    detail.value = updated
    const row = manifests.value.find((m) => m.id === updated.id)
    if (row) {
      row.counts = updated.counts
      row.status = updated.status
    }
  }

  async function receive(specimenId: number) {
    if (!detail.value) return
    busySpecimenId.value = specimenId
    const updated = await guard(() => api.receive(detail.value!.id, specimenId))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    busySpecimenId.value = null
  }

  async function flag(specimenId: number) {
    if (!detail.value) return
    busySpecimenId.value = specimenId
    const updated = await guard(() => api.flag(detail.value!.id, specimenId))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    busySpecimenId.value = null
  }

  async function close() {
    if (!detail.value) return
    closing.value = true
    const updated = await guard(() => api.close(detail.value!.id))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    closing.value = false
  }

  /** Fast Count: bulk-receive on a matching tally, then close in one motion. */
  async function markReceivedAndClose() {
    if (!detail.value) return
    closing.value = true
    const counted = await guard(() => api.fastCount(detail.value!.id, countedTotal.value))
    if (counted) {
      apply(counted)
      const closed = await guard(() => api.close(detail.value!.id))
      if (closed) apply(closed)
    } else {
      await refreshDetailQuietly()
    }
    closing.value = false
  }

  /**
   * A rejected action means our copy disagrees with the server's. Re-fetch without clobbering
   * the error the technician still needs to read.
   */
  async function refreshDetailQuietly() {
    if (!detail.value) return
    try {
      detail.value = await api.getManifest(detail.value.id)
    } catch {
      /* the visible error already explains the problem */
    }
  }

  async function switchLab(id: number) {
    setLabId(id)
    labId.value = id
    techName.value = getTechName()
    selectedId.value = null
    detail.value = null
    manifests.value = []
    search.value = ''
    activeTab.value = 'checkin'
    await loadWorklist()
  }

  return {
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
    isEmpty,
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
  }
}
