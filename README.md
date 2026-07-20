# Specimen Check-In — IPI Pro

A vertical slice of the specimen receiving desk: a technician opens a manifest and verifies it
one of two ways — a **Fast Count** (count the bottles in the box, and if the total matches what
the manifest declared, receive them all at once) or a **Full Scan** (work down the list, marking
each bottle received and flagging the ones that never arrived). Either way the manifest closes
only once every bottle is accounted for. Two pathology labs share the deployment and neither can
see the other's work.

- **Frontend** — Vue 3 + TypeScript + Vite
- **Backend** — ASP.NET Core 8 Web API, EF Core
- **Database** — SQLite (chosen so `dotnet run` is the entire setup; see [Database choice](#database-choice))

---

## Running it

Two terminals. The API first.

```bash
cd backend/src/IpiPro.Api
dotnet run
```

It creates `ipipro.db`, applies the migration, seeds two labs, and listens on
`http://localhost:5080`. Swagger UI is at `/swagger`.

```bash
cd frontend
cp .env.example .env      # Windows cmd: copy .env.example .env
npm install
npm run dev
```

Open `http://localhost:5173`. Use the lab selector in the top right to switch between
**Northgate Pathology** and **Ridgeview Diagnostics** — the worklist changes completely,
because the second lab's manifests were never sent to the browser.

### Tests

```bash
cd backend
dotnet test
```

Nineteen tests: eight on the reconciliation rules (idempotency, discrepancy lifecycle, close
gating), six on the tenant boundary itself, and five on the Fast Count path (count match receives
all, count mismatch mutates nothing, a repeated count double-counts nothing, and the tech name is
stamped onto every bottle it receives).

---

## How the pieces fit

```
X-Lab-Id  +  X-Lab-Tech headers
      │
      ▼
TenantMiddleware ──► TenantContext (scoped, write-once)
                            │
                            ▼
                     AppDbContext
                     ├── global query filters   (reads)
                     └── SaveChanges guard      (writes)
                            │
                            ▼
                      CheckInService  ──► rules, no LabId anywhere
                            │
                            ▼
                      ManifestsController
```

`X-Lab-Id` stands in for authentication. In production the lab id comes from a validated token
claim; `TenantMiddleware` is the only file that would change, because nothing else reads the
request — everything reads `ITenantContext`. `X-Lab-Tech` is the second stand-in: it carries the
signed-in technician's name, which the service stamps onto every bottle it receives (the
**Received by** column). In production it is another token claim, resolved the same way — the
point of putting it on `ITenantContext` now is that the audit actor flows through one seam, not
through every endpoint signature.

### API

| Method | Route | Notes |
| --- | --- | --- |
| `GET` | `/api/labs/current` | Which lab this request is scoped to |
| `GET` | `/api/manifests` | Worklist, open manifests first |
| `GET` | `/api/manifests/{id}` | Detail. 404 if it belongs to another lab |
| `POST` | `/api/manifests/{id}/fast-count` | Body `{ countedTotal }`. Receives all bottles if it matches the declared count |
| `POST` | `/api/manifests/{id}/specimens/{specimenId}/receive` | Idempotent; stamps the tech as receiver |
| `POST` | `/api/manifests/{id}/specimens/{specimenId}/flag` | Raises one open discrepancy |
| `POST` | `/api/manifests/{id}/close` | Rejected unless reconciled |

Every mutation returns the whole manifest, counts included. The client never derives
"ready to close" for itself, so the button and the endpoint cannot disagree.

Failures come back as RFC 7807 `problem+json` with a stable `code`:
`manifest_not_reconciled`, `manifest_closed`, `manifest_already_closed`,
`specimen_already_received`, `fast_count_mismatch`, `not_found`, `missing_tenant`,
`unknown_tenant`.

### Rules worth stating

- **Reconciled** means no bottle is still `Pending` — each one has been received or flagged.
- **Receive is idempotent.** Scanning a bottle twice, or retrying a request that timed out,
  is a success that moves no counter. Receiving a bottle that was flagged as missing resolves
  its discrepancy: it turned up.
- **Flag is idempotent too.** It raises exactly one open discrepancy however often it is called.
  Flagging an already-received bottle is refused (`specimen_already_received`) — un-receiving is
  a supervisor action and is out of scope here.
- **Closing** is refused while anything is pending. A manifest that reconciles with bottles still
  missing closes into `ClosedWithDiscrepancy`: the shipment leaves the technician's desk while the
  discrepancies stay open for whoever chases the clinic.

### Fast Count vs Full Scan

The reference shows a **Verification workflow** toggle, and the two modes are a real product
distinction, not two skins on one screen. A `Manifest` now declares an `ExpectedCount` — the
number of bottles the clinic says it packed — and the front desk decides how much scrutiny a box
gets:

- **Fast Count** is the common case. The tech counts the bottles in the box and enters the total.
  `POST /manifests/{id}/fast-count` compares it to `ExpectedCount`. If they agree, every pending
  bottle is received in one transaction and the manifest is closeable; if a bottle was standing
  flagged from an earlier pass, receiving it resolves the discrepancy — it turned up in the count.
  If they *disagree*, the endpoint changes nothing and returns `fast_count_mismatch`, which the UI
  reads as "the count is off — switch to Full Scan and find which bottle." A wrong number never
  half-receives a manifest.
- **Full Scan** is the row-by-row path from the rules above: receive and flag one specimen at a
  time. It is where a mismatch gets resolved, and where a specific bottle gets flagged as missing.

Mode is a client-side choice — the toggle decides which controls show — but the *rules* live on
the server. `fast-count` is idempotent for the same reason `receive` is: the "Mark Received &
Close" button composes fast-count then close, and a retried or double-clicked request settles to
the same state.

Two fields exist purely to make the receiving desk legible, and both come straight from the
reference: each `Specimen` carries a `Site` and `Provider` (a technician reconciling a bottle
against a requisition reads *Left cheek · Dr. Patel*, not just a code), and each `Manifest` names
its `Courier` — the person who handed the box over, which is the first question when a count is
wrong.

### Database choice

SQLite. The schema uses nothing exotic, EF Core abstracts the provider, and it means a reviewer
runs `dotnet run` and gets a working, seeded database with no container and no connection string
to edit. Moving to Azure SQL is a provider swap and a connection string; the one thing I would
re-check is `DeleteBehavior.Restrict` on `Discrepancy → Specimen`, which is already set precisely
because SQL Server rejects multiple cascade paths.

---

## Write-up

### 1. Deploying this on Azure

**App Service (Linux) for the API, Static Web Apps for the Vue bundle, Azure SQL for the data.**

A request lands on Front Door, terminates TLS, and reaches an App Service instance behind a VNet
integration. The API talks to Azure SQL over a private endpoint — the database has no public
listener. Secrets are not in `appsettings.json`: the App Service uses a managed identity to read
its connection string from Key Vault, and to authenticate to SQL, so there is no password to
rotate or leak. The Vue app is static and cached at the edge; it holds no secrets, because the
lab identity is proved by a token, not by anything the bundle knows.

The three pieces I would add before this is real:

- **Auth.** Entra ID (or an OIDC provider) issues a token carrying a `lab_id` claim.
  `TenantMiddleware` reads the claim instead of the header, and everything below it is unchanged.
- **Background work.** Chasing an open discrepancy is not a request-response job. A Service Bus
  queue and an Azure Function make the "notify the clinic about missing bottles" path retryable
  and independent of the technician's browser.
- **Observability.** Application Insights with the lab id on every trace, so a support engineer
  can answer "what did Northgate see at 09:14" without querying patient rows.

Scale is not the interesting problem here. A receiving desk is a handful of technicians per lab,
so a single App Service plan with two instances covers redundancy rather than throughput; Azure
SQL sizing is driven by manifest history, not by concurrency. I would keep the API stateless and
let session live entirely in the token.

### 2. Tenant isolation, and keeping it once the codebase grows

Isolation is enforced in exactly two places, both inside `AppDbContext`.

**Reads.** Every tenant-owned entity carries its own `LabId` (`ITenantOwned`) and gets a global
query filter, `x.LabId == CurrentLabId`. `CurrentLabId` reads from the request-scoped
`ITenantContext`. A developer writing a new query cannot forget the filter, because they never
write it. Rows from another lab do not appear in a `Where`, in an `Include`, or in a `Count`.

**Writes.** A query filter does not protect an `Update` on a hand-built entity with an id in it.
So `SaveChanges` inspects the change tracker: inserts missing a `LabId` are stamped with the
current lab, and anything — inserted, modified, deleted — whose `LabId` disagrees with the request
throws `CrossTenantWriteException`. That exception is logged at error level and returned to the
caller as a plain `404`, because a `403` would confirm the row exists.

For the same reason, another lab's manifest is `not_found`, never `forbidden`. The query filter
means it genuinely does not exist as far as this request is concerned, and the API says so.

**Testing it as the codebase grows.** `TenantIsolationTests` pins the boundary rather than the
endpoints: a lab sees only its own manifests, another lab's manifest is 404, a cross-lab receive
mutates nothing, a mis-stamped insert is refused, a forgotten `LabId` is stamped. These keep
holding as endpoints are added, because they test the mechanism. The tests run against a real
SQLite database rather than the EF in-memory provider, so unique indexes and foreign-key behaviour
are the ones we ship. `TestHarness` calls `Migrate()`, not `EnsureCreated()`, so a migration that
drifts from the model fails the suite.

What I would add next, in order: an integration test that fires a real HTTP request with a forged
`X-Lab-Id` once auth exists; an analyzer or architecture test asserting no `DbSet` is queried with
`IgnoreQueryFilters()` outside `Data/` and the test project; and a nightly job that scans for rows
whose `LabId` disagrees with their parent manifest's.

### 3. Handling PHI

The rule I designed to is that patient data is only ever in two places: the database, and the
response to a request that has already proved which lab it belongs to.

- **In transit.** TLS everywhere, no exceptions, HSTS on the front door. CORS is an allow-list of
  known origins, not a wildcard.
- **At rest.** Azure SQL with Transparent Data Encryption, and Always Encrypted on `Patient` if a
  DPO asks for it — the column is never queried, only projected, so the performance cost is small.
- **In logs.** Nothing in this codebase logs a patient name. Errors log the specimen *code* and the
  manifest *code*, which are meaningless without the database. The `CrossTenantWriteException`
  message names two lab ids and no rows. That is deliberate: logs leave the trust boundary — they
  go to Application Insights, to a support engineer's screen, into a screenshot in a ticket.
- **Access.** Least privilege on the SQL identity (no `db_owner` for the app), no shared accounts,
  and an audit trail. Receiving a bottle now records *who* — `ReceivedBy`, taken from the technician
  identity on the request — alongside `ReceivedAt`. Flag and close still record only the timestamp,
  so the remaining audit gap is narrow and named: under a HIPAA audit requirement I would carry the
  same actor onto every state transition, because "the manifest was closed" is not a useful audit
  record without "by whom". The seam is already there — the actor rides on `ITenantContext`, so it
  is a field on two more writes, not a new plumbing exercise.
- **In seed data.** The seeded patients are invented. A repository that ships a realistic-looking
  patient list teaches the next engineer that it is fine to paste one in.

The honest limitation: `X-Lab-Id` is a header a client sets. It is a placeholder for a token claim,
and until it becomes one, this application authorises nothing — it only *scopes*. The isolation
machinery is real and tested; the identity it scopes to is not yet proved.

---

## With more time

- Off-manifest bottles. `DiscrepancyType.OffManifest` and the nullable `SpecimenId` are modelled
  for it, but there is no endpoint: a bottle that is not on the list needs a scan-anything input,
  which is a different interaction than the row-by-row table here.
- A real scanner path. `Receive` takes a specimen id; a barcode gun gives you a code, so
  `POST /manifests/{id}/scan { code }` resolving to a specimen — or to an off-manifest discrepancy.
- Optimistic concurrency. Two technicians on one manifest currently last-write-wins. A `rowversion`
  on `Specimen` plus a `409` would be a small change and a real one.
- An actor on the *flag* and *close* transitions too, per the PHI note above — receive already has one.
- Component tests for the Vue layer. The composable is the thing worth testing — the components
  are close to presentational.

## Notes on the build

The backend was written without a compiler to hand, so if `dotnet restore` turns up a package
version I got wrong, that is why. `dotnet build` and `dotnet test` are the first things I would
have you run. The frontend was built and type-checked (`npm run build` runs `vue-tsc --noEmit`).

The EF migration and its model snapshot were hand-authored for the same reason — no SDK to run
`dotnet ef` against. The schema is ordinary, so it should apply cleanly, but if EF reports model
drift on first run, the clean fix is to delete `Data/Migrations/` and regenerate:
`dotnet ef migrations add InitialCreate`. No database ships in the repo, so nothing is lost —
`dotnet run` rebuilds and reseeds `ipipro.db`.

The design reference drove the layout: a dark ops bar carrying mode, location, and the signed-in
technician; the Check-In / Scan History / Manifests / Discrepancies tab row; a left rail with the
Fast Count / Full Scan toggle, a manifest search, the bottle-count stepper, and the recent-manifest
cards; and a detail pane with the Expected / Received / Pending / Flagged stat cards over a specimen
table (status, id, patient, site, provider, received-by, time). The Scan History tab is an honest
empty state — a per-scan audit trail is out of this slice. The one place I spent a deliberate
decision: specimen and manifest codes are set in tabular monospace, because a technician reads them
character by character against a label on a bottle.
