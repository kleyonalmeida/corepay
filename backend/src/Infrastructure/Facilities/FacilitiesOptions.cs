namespace Infrastructure.Facilities;

public sealed class FacilitiesOptions
{
    public const string SectionName = "Facilities";

    public string WebhookSecret { get; set; } = string.Empty;
}
