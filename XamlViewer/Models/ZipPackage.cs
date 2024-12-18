
using System.ComponentModel.DataAnnotations;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace XamlViewer.Models;

public class ZipPackage : IPackageMetadata
{
    private readonly string source;
    private readonly Func<Stream> _streamFactory;
    private ManifestMetadata _metadata;

    public ZipPackage([Required] string source)
    {
        this.source = source;
        _streamFactory = () =>
        {
            try
            {
                return File.Open(this.source, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (UnauthorizedAccessException)
            {
                //just try read
                return File.Open(this.source, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

        };
        this.EnsureManifest();
    }

    private void EnsureManifest()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        using var nuspecStream = reader.GetNuspec();

        var manifest = Manifest.ReadFrom(nuspecStream, false);
        _metadata = manifest.Metadata;
    }

    public string Id => _metadata.Id;

    public NuGetVersion Version => _metadata.Version;

    public string Title => _metadata.Title;

    public IEnumerable<string> Authors => _metadata.Authors;

    public IEnumerable<string> Owners => _metadata.Owners;

    public Uri IconUrl => _metadata.IconUrl;

    public Uri LicenseUrl => _metadata.LicenseUrl;

    public Uri ProjectUrl => _metadata.ProjectUrl;

    public bool RequireLicenseAcceptance => _metadata.RequireLicenseAcceptance;

    public bool DevelopmentDependency => _metadata.DevelopmentDependency;

    public string Description => _metadata.Description;

    public string Summary => _metadata.Summary;

    public string ReleaseNotes => _metadata.ReleaseNotes;

    public string Language => _metadata.Language;

    public string Tags => _metadata.Tags;

    public bool Serviceable => _metadata.Serviceable;

    public string Copyright => _metadata.Copyright;

    public string Icon => _metadata.Icon;

    public string Readme => _metadata.Readme;

    public IEnumerable<FrameworkAssemblyReference> FrameworkReferences => _metadata.FrameworkReferences;

    public IEnumerable<PackageReferenceSet> PackageAssemblyReferences => _metadata.PackageAssemblyReferences;

    public IEnumerable<PackageDependencyGroup> DependencyGroups => _metadata.DependencyGroups;

    public Version MinClientVersion => _metadata.MinClientVersion;

    public IEnumerable<ManifestContentFiles> ContentFiles => _metadata.ContentFiles;

    public IEnumerable<PackageType> PackageTypes => _metadata.PackageTypes;

    public RepositoryMetadata Repository => _metadata.Repository;

    public LicenseMetadata LicenseMetadata => _metadata.LicenseMetadata;

    public IEnumerable<FrameworkReferenceGroup> FrameworkReferenceGroups => _metadata.FrameworkReferenceGroups;
}
