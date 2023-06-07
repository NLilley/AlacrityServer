using AlacrityCore.Enums.Client;

namespace AlacrityCore.Models.DTOs;
public record ClientSettingsDto
{
    public int SessionDurationMins { get; set; }
    public VisualizationQuality VisualizationQuality { get; set; }
    public bool IsTelemetryEnabled { get; set; }
}
