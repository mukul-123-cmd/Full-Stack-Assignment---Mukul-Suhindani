using IpiPro.Api.Data;
using IpiPro.Api.Domain;
using IpiPro.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IpiPro.Api.Tests;

/// <summary>
/// Fast Count is the bulk path: count the shipment, and if the tally matches what the clinic
/// declared, receive everything at once. These pin the two things that make it safe — it only
/// reconciles on an exact match, and running it twice does not double-count.
/// </summary>
public class FastCountTests
{
    /// <summary>MF-2481 declares 6 expected; 2 received, 1 flagged, 3 pending at the start.</summary>
    private static async Task<(TestHarness Harness, CheckInService Svc, Manifest M)> BusyManifestAsync()
    {
        var harness = await TestHarness.CreateAsync();
        var (db, svc) = harness.AsLab(TestHarness.Northgate);
        var manifest = await db.Manifests.Include(m => m.Specimens).FirstAsync(m => m.Code == "MF-2481");
        return (harness, svc, manifest);
    }

    [Fact]
    public async Task A_matching_count_receives_every_bottle_and_makes_the_manifest_closeable()
    {
        var (harness, svc, m) = await BusyManifestAsync();
        await using var _h = harness;

        var result = await svc.FastCountAsync(m.Id, 6);

        Assert.Equal(6, result.Counts.Received);
        Assert.Equal(0, result.Counts.Pending);
        Assert.Equal(0, result.Counts.Flagged);
        Assert.True(result.Counts.CanClose);
    }

    [Fact]
    public async Task A_matching_count_resolves_a_bottle_that_had_been_flagged_missing()
    {
        var (harness, svc, m) = await BusyManifestAsync();
        await using var _h = harness;

        var result = await svc.FastCountAsync(m.Id, 6);

        // The flagged bottle turned up in the count, so its discrepancy is resolved.
        Assert.Equal(0, result.Counts.OpenDiscrepancies);
    }

    [Fact]
    public async Task A_count_that_disagrees_with_expected_is_rejected_and_changes_nothing()
    {
        var (harness, svc, m) = await BusyManifestAsync();
        await using var _h = harness;

        var ex = await Assert.ThrowsAsync<DomainException>(() => svc.FastCountAsync(m.Id, 5));
        Assert.Equal("fast_count_mismatch", ex.Code);

        // Nothing moved: still the original 2 received.
        var after = await svc.GetManifestAsync(m.Id);
        Assert.Equal(2, after.Counts.Received);
        Assert.Equal(3, after.Counts.Pending);
    }

    [Fact]
    public async Task Running_a_matching_fast_count_twice_does_not_double_count()
    {
        var (harness, svc, m) = await BusyManifestAsync();
        await using var _h = harness;

        var first = await svc.FastCountAsync(m.Id, 6);
        var second = await svc.FastCountAsync(m.Id, 6);

        Assert.Equal(first.Counts, second.Counts);
        Assert.Equal(6, second.Counts.Received);
    }

    [Fact]
    public async Task Fast_count_stamps_who_received_the_bottles()
    {
        var harness = await TestHarness.CreateAsync();
        await using var _h = harness;

        var (db, svc) = harness.AsLab(TestHarness.Northgate, techName: "Riya Kapoor");
        var m = await db.Manifests.Include(x => x.Specimens).FirstAsync(x => x.Code == "MF-2481");

        await svc.FastCountAsync(m.Id, 6);

        var received = await db.Specimens.Where(s => s.ManifestId == m.Id).ToListAsync();
        Assert.All(received, s => Assert.Equal("Riya Kapoor", s.ReceivedBy));
    }
}
