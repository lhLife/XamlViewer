namespace XamlViewer.Models;
public record PackageEntity
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Framework { get; set; }
    public string? Dir { get; set; }

    public bool IsLoadDependency { get; set; } = false;
    public bool IsDownload { get; set; } = false;

    public List<PackageEntity> Children { get; set; } = new List<PackageEntity>();
}
