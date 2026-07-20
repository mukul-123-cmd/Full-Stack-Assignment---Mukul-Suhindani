namespace IpiPro.Api.Tenancy;

/// <summary>
/// Who this request is acting as: which lab, and which technician. Resolved once per request.
/// Everything downstream — the DbContext query filter, the SaveChanges guard, the ReceivedBy
/// stamp — reads identity from here and nowhere else.
/// </summary>
public interface ITenantContext
{
    /// <summary>Null during seeding and in design-time tooling; never null on an HTTP request.</summary>
    int? LabId { get; }

    bool HasTenant { get; }

    /// <summary>Display name of the acting technician. A safe default when none was supplied.</summary>
    string TechName { get; }

    /// <summary>Throws if no tenant is resolved. Use wherever a lab is genuinely required.</summary>
    int RequireLabId();
}
