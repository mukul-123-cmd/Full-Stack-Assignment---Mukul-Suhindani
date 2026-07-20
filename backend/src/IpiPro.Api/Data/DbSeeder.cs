using IpiPro.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Data;

/// <summary>
/// Synthetic seed data only — no real patient information, ever. Two labs, so tenant
/// isolation is something you can see by flipping a header rather than something you
/// have to take on trust.
/// </summary>
public static class DbSeeder
{
    private const string SeedTech = "Lab Tech 1";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: seeding runs without a tenant, so the filters would otherwise
        // hide every row and we would re-seed on each boot.
        if (await db.Manifests.IgnoreQueryFilters().AnyAsync(ct)) return;

        var sentAt = new DateTime(2026, 7, 6, 8, 15, 0, DateTimeKind.Utc);

        var northgate = new Lab { Name = "Northgate Pathology" };
        var ridgeview = new Lab { Name = "Ridgeview Diagnostics" };
        db.Labs.AddRange(northgate, ridgeview);
        await db.SaveChangesAsync(ct);

        // --- Lab 1: the manifest the design reference is showing --------------------
        var busy = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2481",
            ClinicName = "Vale Street Clinic",
            Courier = "Khush Arya",
            ExpectedCount = 6,
            Status = ManifestStatus.Open,
            SentAt = sentAt,
            Specimens =
            {
                Spec(northgate.Id, "SPC-10041", "Harper Quinn", "Right cheek", "Dr. Patel", SpecimenStatus.Received, sentAt),
                Spec(northgate.Id, "SPC-10042", "Marcus Ellery", "Left cheek", "Dr. Patel", SpecimenStatus.Received, sentAt),
                Spec(northgate.Id, "SPC-10043", "Nadia Osei", "Back, upper", "Dr. Chen", SpecimenStatus.Flagged),
                Spec(northgate.Id, "SPC-10044", "Theo Whitlock", "Right shoulder", "Dr. Chen", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10045", "Imogen Bassey", "Scalp", "Dr. Reed", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10046", "Rafael Duarte", "Left forearm", "Dr. Patel", SpecimenStatus.Pending)
            }
        };

        var fresh = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2482",
            ClinicName = "Kingsmead Surgery",
            Courier = "Devlin Rowe",
            ExpectedCount = 4,
            Status = ManifestStatus.Open,
            SentAt = sentAt.AddHours(3),
            Specimens =
            {
                Spec(northgate.Id, "SPC-10051", "Priya Raman", "Left forearm", "Dr. Osman", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10052", "Callum Reed", "Right calf", "Dr. Osman", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10053", "Ana Ferreira", "Abdomen", "Dr. Whitfield", SpecimenStatus.Pending),
                Spec(northgate.Id, "SPC-10054", "Dominic Shaw", "Right hand", "Dr. Whitfield", SpecimenStatus.Pending)
            }
        };

        var settled = new Manifest
        {
            LabId = northgate.Id,
            Code = "MF-2475",
            ClinicName = "Vale Street Clinic",
            Courier = "Khush Arya",
            ExpectedCount = 2,
            Status = ManifestStatus.Closed,
            SentAt = sentAt.AddDays(-2),
            ClosedAt = sentAt.AddDays(-2).AddHours(5),
            Specimens =
            {
                Spec(northgate.Id, "SPC-09980", "Elena Kovac", "Left temple", "Dr. Reed", SpecimenStatus.Received, sentAt.AddDays(-2)),
                Spec(northgate.Id, "SPC-09981", "Owen Brackley", "Right ear", "Dr. Reed", SpecimenStatus.Received, sentAt.AddDays(-2))
            }
        };

        // --- Lab 2: exists purely so that "Lab A cannot see this" is demonstrable ----
        var otherLab = new Manifest
        {
            LabId = ridgeview.Id,
            Code = "MF-2481", // same code as Northgate's: codes are unique per lab, not globally
            ClinicName = "Ridgeview Outpatients",
            Courier = "Sana Iqbal",
            ExpectedCount = 3,
            Status = ManifestStatus.Open,
            SentAt = sentAt.AddHours(1),
            Specimens =
            {
                Spec(ridgeview.Id, "SPC-77010", "Joan Mbeki", "Left forearm", "Dr. Nayar", SpecimenStatus.Received, sentAt),
                Spec(ridgeview.Id, "SPC-77011", "Peter Lindqvist", "Right cheek", "Dr. Nayar", SpecimenStatus.Pending),
                Spec(ridgeview.Id, "SPC-77012", "Sasha Delaney", "Nape", "Dr. Holt", SpecimenStatus.Pending)
            }
        };

        db.Manifests.AddRange(busy, fresh, settled, otherLab);
        await db.SaveChangesAsync(ct);

        // The flagged bottle on MF-2481 carries the open discrepancy it would have raised.
        db.Discrepancies.Add(new Discrepancy
        {
            LabId = northgate.Id,
            ManifestId = busy.Id,
            SpecimenId = busy.Specimens.First(s => s.Code == "SPC-10043").Id,
            Type = DiscrepancyType.Missing,
            Status = DiscrepancyStatus.Open,
            CreatedAt = sentAt.AddHours(1)
        });

        await db.SaveChangesAsync(ct);
    }

    private static Specimen Spec(
        int labId, string code, string patient, string site, string provider,
        SpecimenStatus status, DateTime? receivedAt = null) =>
        new()
        {
            LabId = labId,
            Code = code,
            Patient = patient,
            Site = site,
            Provider = provider,
            Status = status,
            ReceivedAt = status == SpecimenStatus.Received ? receivedAt : null,
            ReceivedBy = status == SpecimenStatus.Received ? SeedTech : null
        };
}
