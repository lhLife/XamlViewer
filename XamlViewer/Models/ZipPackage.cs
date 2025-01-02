
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace XamlViewer.Models;

public class ZipPackage : IPackageMetadata, NuGet.Packaging.IPackageContentReader
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

    public IEnumerable<FrameworkSpecificGroup> GetFrameworkItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetFrameworkItems().ToList();
    }

    public IEnumerable<FrameworkSpecificGroup> GetBuildItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetBuildItems().ToList();
    }

    public IEnumerable<FrameworkSpecificGroup> GetToolItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetToolItems().ToList();
    }

    public IEnumerable<FrameworkSpecificGroup> GetContentItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetContentItems().ToList();
    }

    public IEnumerable<FrameworkSpecificGroup> GetLibItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetLibItems().ToList();
    }

    public IEnumerable<FrameworkSpecificGroup> GetReferenceItems()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetReferenceItems().ToList();
    }

    public IEnumerable<PackageDependencyGroup> GetPackageDependencies()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetPackageDependencies().ToList();
    }


    public IEnumerable<string> GetFiles()
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetFiles().ToList();
    }

    public IEnumerable<string> GetFiles(string folder)
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.GetFiles(folder.EndsWith('/') ? folder.Substring(0, folder.Length - 1) : folder).ToList();
    }


    public Task<IEnumerable<string>> CopyFilesAsync(string destination, IEnumerable<string> packageFiles, NuGet.Common.ILogger logger, CancellationToken token)
    {
        return Task.FromResult(CopyFiles(destination, packageFiles, logger, token));
    }
    public IEnumerable<string> CopyFiles(string destination, IEnumerable<string> packageFiles, NuGet.Common.ILogger logger, CancellationToken token)
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);

        return reader.CopyFiles(destination, packageFiles, (a, b, c) => this.ExtractFile(a, Path.Combine(destination, this.GetFileName(a)), logger), logger, token).ToList();
    }

    private string GetFileName(string path)
    {
        return path.Split('/').LastOrDefault()!;
    }

    public string ExtractFile(string packageFile, string targetFilePath, NuGet.Common.ILogger logger)
    {
        using var stream = _streamFactory();
        using var reader = new PackageArchiveReader(stream);
        return reader.ExtractFile(packageFile, targetFilePath, logger);
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
