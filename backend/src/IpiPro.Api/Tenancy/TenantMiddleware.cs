using IpiPro.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Tenancy;

/// <summary>
/// Stands in for authentication. In production the lab id and the technician identity would
/// come from a validated token claim rather than client-supplied headers; the rest of the
/// application would not change, because it only ever reads ITenantContext.
/// </summary>
public sealed class TenantMiddleware
{
    public const string LabHeader = "X-Lab-Id";
    public const string TechHeader = "X-Lab-Tech";

    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext http, ITenantContext tenant, AppDbContext db)
    {
        // Swagger, health, and CORS preflight run without a tenant.
        if (!http.Request.Path.StartsWithSegments("/api"))
        {
            await _next(http);
            return;
        }

        if (!http.Request.Headers.TryGetValue(LabHeader, out var raw) ||
            !int.TryParse(raw.ToString(), out var labId))
        {
            await WriteProblem(http, StatusCodes.Status401Unauthorized, "missing_tenant",
                $"Request must supply a numeric {LabHeader} header.");
            return;
        }

        // The lab must exist. Without this, an unknown id would silently produce empty
        // result sets that look identical to "this lab has no manifests".
        var exists = await db.Labs.AsNoTracking().AnyAsync(l => l.Id == labId);
        if (!exists)
        {
            await WriteProblem(http, StatusCodes.Status401Unauthorized, "unknown_tenant",
                "The supplied lab is not recognised.");
            return;
        }

        var techName = http.Request.Headers.TryGetValue(TechHeader, out var techRaw)
            ? techRaw.ToString()
            : null;

        ((TenantContext)tenant).Resolve(labId, techName);

        await _next(http);
    }

    private static Task WriteProblem(HttpContext http, int status, string code, string detail)
    {
        http.Response.StatusCode = status;
        http.Response.ContentType = "application/problem+json";
        return http.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "Tenant context could not be established.",
            status,
            code,
            detail
        });
    }
}
