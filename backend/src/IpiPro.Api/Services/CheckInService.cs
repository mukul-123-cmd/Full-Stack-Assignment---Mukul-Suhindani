using IpiPro.Api.Contracts;
using IpiPro.Api.Data;
using IpiPro.Api.Domain;
using IpiPro.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Services;

/// <summary>
/// All check-in rules live here. Notice there is not a single LabId comparison in this file:
/// tenant scoping is a property of the DbContext, not something each handler remembers to do.
/// If it were written per-query, the first person to add an endpoint would forget.
/// </summary>
public sealed class CheckInService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ITenantContext _tenant;

    public CheckInService(AppDbContext db, TimeProvider clock, ITenantContext tenant)
    {
        _db = db;
        _clock = clock;
        _tenant = tenant;
    }

    private DateTime UtcNow => _clock.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<ManifestSummaryDto>> ListManifestsAsync(CancellationToken ct = default)
    {
        var manifests = await _db.Manifests
            .Include(m => m.Specimens)
            .Include(m => m.Discrepancies)
            .OrderBy(m => m.Status == ManifestStatus.Open ? 0 : 1)
            .ThenByDescending(m => m.SentAt)
            .ToListAsync(ct);

        return manifests.Select(m => DtoMapper.ToSummary(m, CountOpenDiscrepancies(m))).ToList();
    }

    public async Task<ManifestDetailDto> GetManifestAsync(int manifestId, CancellationToken ct = default)
    {
        var manifest = await LoadManifestAsync(manifestId, ct);
        return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));
    }

    /// <summary>
    /// Idempotent by design: a technician who scans the same bottle twice, or whose first
    /// request timed out and retried, must not move any counter twice. The second call is a
    /// success that changes nothing.
    /// </summary>
    public async Task<ManifestDetailDto> ReceiveSpecimenAsync(
        int manifestId, int specimenId, CancellationToken ct = default)
    {
        var manifest = await LoadOpenManifestAsync(manifestId, ct);
        var specimen = FindSpecimen(manifest, specimenId);

        if (specimen.Status == SpecimenStatus.Received)
            return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));

        ReceiveOne(manifest, specimen);

        await _db.SaveChangesAsync(ct);
        return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));
    }

    /// <summary>
    /// Flagging a listed bottle as missing raises exactly one open discrepancy, no matter how
    /// many times it is called.
    /// </summary>
    public async Task<ManifestDetailDto> FlagSpecimenAsync(
        int manifestId, int specimenId, CancellationToken ct = default)
    {
        var manifest = await LoadOpenManifestAsync(manifestId, ct);
        var specimen = FindSpecimen(manifest, specimenId);

        if (specimen.Status == SpecimenStatus.Received)
            throw new DomainException(
                "specimen_already_received",
                $"Specimen {specimen.Code} is already received and cannot be flagged as missing.");

        if (specimen.Status == SpecimenStatus.Flagged)
            return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));

        specimen.Status = SpecimenStatus.Flagged;
        specimen.ReceivedAt = null;
        specimen.ReceivedBy = null;

        var alreadyOpen = manifest.Discrepancies.Any(d =>
            d.SpecimenId == specimen.Id && d.Status == DiscrepancyStatus.Open);

        if (!alreadyOpen)
        {
            manifest.Discrepancies.Add(new Discrepancy
            {
                LabId = manifest.LabId,
                ManifestId = manifest.Id,
                SpecimenId = specimen.Id,
                Type = DiscrepancyType.Missing,
                Status = DiscrepancyStatus.Open,
                CreatedAt = UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));
    }

    /// <summary>
    /// The quick path. Instead of scanning each bottle, the technician counts the shipment and
    /// enters the total. It only reconciles cleanly when the tally matches what the clinic
    /// declared — anything else is a genuine discrepancy that Fast Count cannot attribute to a
    /// particular bottle, so we refuse and send them to Full Scan rather than guess.
    ///
    /// When it does match, every bottle is received in one action. Idempotent: a second call
    /// with the same matching total finds nothing left to receive and changes nothing.
    /// </summary>
    public async Task<ManifestDetailDto> FastCountAsync(
        int manifestId, int countedTotal, CancellationToken ct = default)
    {
        var manifest = await LoadOpenManifestAsync(manifestId, ct);

        if (countedTotal != manifest.ExpectedCount)
            throw new DomainException(
                "fast_count_mismatch",
                $"Counted {countedTotal} but the manifest declares {manifest.ExpectedCount}. " +
                "Switch to Full Scan to reconcile bottle by bottle.");

        foreach (var specimen in manifest.Specimens.Where(s => s.Status != SpecimenStatus.Received))
            ReceiveOne(manifest, specimen);

        await _db.SaveChangesAsync(ct);
        return DtoMapper.ToDetail(manifest, CountOpenDiscrepancies(manifest));
    }

    /// <summary>
    /// Close is refused while any bottle is still Pending — that is what "reconciled" means.
    /// A manifest that reconciles with unresolved missing bottles closes into
    /// ClosedWithDiscrepancy, so the shipment is off the technician's desk while the
    /// discrepancies stay open for whoever chases the clinic.
    /// </summary>
    public async Task<ManifestDetailDto> CloseManifestAsync(int manifestId, CancellationToken ct = default)
    {
        var manifest = await LoadManifestAsync(manifestId, ct);

        if (manifest.IsClosed())
            throw new DomainException(
                "manifest_already_closed",
                $"Manifest {manifest.Code} is already closed.");

        var pending = manifest.Specimens.Count(s => s.Status == SpecimenStatus.Pending);
        if (pending > 0)
            throw new DomainException(
                "manifest_not_reconciled",
                $"{pending} specimen(s) are still pending. Receive or flag every bottle before closing.");

        var openDiscrepancies = CountOpenDiscrepancies(manifest);

        manifest.Status = openDiscrepancies > 0
            ? ManifestStatus.ClosedWithDiscrepancy
            : ManifestStatus.Closed;
        manifest.ClosedAt = UtcNow;

        await _db.SaveChangesAsync(ct);
        return DtoMapper.ToDetail(manifest, openDiscrepancies);
    }

    // --- helpers -------------------------------------------------------------------

    /// <summary>
    /// Marks one bottle received and, if it had been flagged as missing, resolves the
    /// discrepancy it raised — the bottle turned up, so the phantom open item should not
    /// outlive it. Shared by the per-bottle path and Fast Count so the two cannot drift.
    /// </summary>
    private void ReceiveOne(Manifest manifest, Specimen specimen)
    {
        if (specimen.Status == SpecimenStatus.Flagged)
        {
            foreach (var d in manifest.Discrepancies.Where(d =>
                         d.SpecimenId == specimen.Id && d.Status == DiscrepancyStatus.Open))
            {
                d.Status = DiscrepancyStatus.Resolved;
                d.ResolvedAt = UtcNow;
            }
        }

        specimen.Status = SpecimenStatus.Received;
        specimen.ReceivedAt = UtcNow;
        specimen.ReceivedBy = _tenant.TechName;
    }

    /// <summary>
    /// The global query filter is doing the work here. A manifest belonging to another lab
    /// is not "forbidden", it simply does not exist, so we answer 404 and leak nothing about
    /// whether that id is real.
    /// </summary>
    private async Task<Manifest> LoadManifestAsync(int manifestId, CancellationToken ct)
    {
        var manifest = await _db.Manifests
            .Include(m => m.Specimens)
            .Include(m => m.Discrepancies)
            .FirstOrDefaultAsync(m => m.Id == manifestId, ct);

        return manifest ?? throw DomainException.NotFound($"Manifest {manifestId}");
    }

    private async Task<Manifest> LoadOpenManifestAsync(int manifestId, CancellationToken ct)
    {
        var manifest = await LoadManifestAsync(manifestId, ct);

        if (manifest.IsClosed())
            throw new DomainException(
                "manifest_closed",
                $"Manifest {manifest.Code} is closed and no longer accepts check-in actions.");

        return manifest;
    }

    private static Specimen FindSpecimen(Manifest manifest, int specimenId) =>
        manifest.Specimens.FirstOrDefault(s => s.Id == specimenId)
        ?? throw DomainException.NotFound($"Specimen {specimenId} on manifest {manifest.Code}");

    private static int CountOpenDiscrepancies(Manifest manifest) =>
        manifest.Discrepancies.Count(d => d.Status == DiscrepancyStatus.Open);
}
