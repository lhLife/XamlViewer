namespace XamlViewer.Models;

public record AppConfig
{
    public string? Environment { get; init; }
    public ShowEntity[]? Shows { get; init; }

}
