import { ApiError, type Lab, type ManifestDetail, type ManifestSummary } from './types'

const BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

/**
 * The lab id and technician name are sent as headers, standing in for the claims a real
 * token would carry. The server never trusts them blindly: it checks the lab exists, then
 * scopes every query to it. Changing these in devtools gets you another lab's data only if
 * you are actually entitled to it — which, once this is behind auth, you are not.
 */
const LAB_HEADER = 'X-Lab-Id'
const TECH_HEADER = 'X-Lab-Tech'
const LAB_STORAGE_KEY = 'ipipro.labId'

const TECH_NAMES: Record<number, string> = {
  1: 'Lab Tech 1',
  2: 'Lab Tech 2'
}

export function getLabId(): number {
  const stored = window.localStorage.getItem(LAB_STORAGE_KEY)
  return stored ? Number(stored) : 1
}

export function setLabId(labId: number): void {
  window.localStorage.setItem(LAB_STORAGE_KEY, String(labId))
}

export function getTechName(): string {
  return TECH_NAMES[getLabId()] ?? 'Lab Tech 1'
}

async function request<T>(path: string, method: 'GET' | 'POST' = 'GET', body?: unknown): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${BASE}/api${path}`, {
      method,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        [LAB_HEADER]: String(getLabId()),
        [TECH_HEADER]: getTechName()
      },
      body: body === undefined ? undefined : JSON.stringify(body)
    })
  } catch {
    // fetch only rejects on network-level failure, never on a 4xx/5xx.
    throw new ApiError('network_error', 'Cannot reach the check-in service.', 0)
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      problem?.code ?? 'unknown_error',
      problem?.detail ?? 'The request could not be completed.',
      response.status
    )
  }

  return response.json() as Promise<T>
}

export const api = {
  currentLab: () => request<Lab>('/labs/current'),
  listManifests: () => request<ManifestSummary[]>('/manifests'),
  getManifest: (id: number) => request<ManifestDetail>(`/manifests/${id}`),
  receive: (id: number, specimenId: number) =>
    request<ManifestDetail>(`/manifests/${id}/specimens/${specimenId}/receive`, 'POST'),
  flag: (id: number, specimenId: number) =>
    request<ManifestDetail>(`/manifests/${id}/specimens/${specimenId}/flag`, 'POST'),
  fastCount: (id: number, countedTotal: number) =>
    request<ManifestDetail>(`/manifests/${id}/fast-count`, 'POST', { countedTotal }),
  close: (id: number) => request<ManifestDetail>(`/manifests/${id}/close`, 'POST')
}
