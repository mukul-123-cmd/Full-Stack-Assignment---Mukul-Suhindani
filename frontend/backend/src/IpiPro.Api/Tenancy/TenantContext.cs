namespace IpiPro.Api.Tenancy;

/// <summary>
/// Scoped, write-once. Only TenantMiddleware may set it, and only once per request,
/// so nothing later in the pipeline can quietly re-point the request at another lab.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public int? LabId { get; private set; }

    public bool HasTenant => LabId.HasValue;

    public string TechName { get; private set; } = "Lab Tech";

    public int RequireLabId() =>
        LabId ?? throw new InvalidOperationException("No tenant resolved for this operation.");

    internal void Resolve(int labId, string? techName = null)
    {
        if (LabId.HasValue)
            throw new InvalidOperationException("Tenant already resolved for this request.");

        LabId = labId;
        if (!string.IsNullOrWhiteSpace(techName))
            TechName = techName.Trim();
    }
}
