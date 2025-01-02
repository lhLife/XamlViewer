

namespace XamlViewer.Models;
public record AppSettings
{
    public PackageEntity[]? Packages { get; init; }

    //public WorkEntity[]? Works { get; init; }

    public string[]? Ignores { get; set; }
}
