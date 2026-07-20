export type ManifestStatus = 'Open' | 'Closed' | 'ClosedWithDiscrepancy'
export type SpecimenStatus = 'Pending' | 'Received' | 'Flagged'

/** How the technician is working this shipment. Client-side only. */
export type VerifyMode = 'fast' | 'scan'

export interface ManifestCounts {
  expected: number
  total: number
  received: number
  flagged: number
  pending: number
  openDiscrepancies: number
  isReconciled: boolean
  canClose: boolean
}

export interface ManifestSummary {
  id: number
  code: string
  clinicName: string
  status: ManifestStatus
  sentAt: string
  counts: ManifestCounts
}

export interface Specimen {
  id: number
  code: string
  patient: string
  site: string
  provider: string
  status: SpecimenStatus
  receivedAt: string | null
  receivedBy: string | null
}

export interface ManifestDetail extends Omit<ManifestSummary, 'counts'> {
  courier: string
  closedAt: string | null
  counts: ManifestCounts
  specimens: Specimen[]
}

export interface Lab {
  id: number
  name: string
}

/** Mirrors the problem+json body the API returns for every handled failure. */
export class ApiError extends Error {
  constructor(
    readonly code: string,
    message: string,
    readonly status: number
  ) {
    super(message)
    this.name = 'ApiError'
  }
}
