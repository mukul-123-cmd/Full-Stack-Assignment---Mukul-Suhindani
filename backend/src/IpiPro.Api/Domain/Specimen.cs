namespace IpiPro.Api.Domain;

public class Specimen : ITenantOwned
{
    public int Id { get; set; }
    public int LabId { get; set; }

    public int ManifestId { get; set; }
    public Manifest? Manifest { get; set; }

    /// <summary>Accession / barcode value printed on the bottle.</summary>
    public string Code { get; set; } = string.Empty;

    public string Patient { get; set; } = string.Empty;

    /// <summary>Collection site on the body, e.g. "Left forearm". Printed on the requisition.</summary>
    public string Site { get; set; } = string.Empty;

    /// <summary>Requesting clinician.</summary>
    public string Provider { get; set; } = string.Empty;

    public SpecimenStatus Status { get; set; } = SpecimenStatus.Pending;

    public DateTime? ReceivedAt { get; set; }

    /// <summary>Which technician checked the bottle in. Null until received.</summary>
    public string? ReceivedBy { get; set; }
}
