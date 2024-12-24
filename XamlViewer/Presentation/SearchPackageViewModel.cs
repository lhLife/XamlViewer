

using System;
using System.Collections.ObjectModel;
using System.IO.Compression;
using Microsoft.VisualBasic;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Windows.ApplicationModel;

namespace XamlViewer.Presentation;
public partial class SearchPackageViewModel : ObservableObject
{
    private SourceRepository? sourceRepository;

    public SearchPackageViewModel()
    {

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var nugetSettings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
        this.PackageSources = new ObservableCollection<NuGet.Configuration.PackageSource>(NuGet.Configuration.PackageSourceProvider.LoadPackageSources(nugetSettings));
        this.SelectedPackageSource = this.PackageSources.FirstOrDefault();

        if (this.SelectedPackageSource is not null)
            sourceRepository = Repository.CreateSource(Repository.Provider.GetCoreV3(), this.SelectedPackageSource);


    }


    #region 绑定属性

    public ObservableCollection<NuGet.Configuration.PackageSource> PackageSources
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public NuGet.Configuration.PackageSource? SelectedPackageSource
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                sourceRepository = Repository.CreateSource(Repository.Provider.GetCoreV3(), value);
        }
    }

    public string? PackageName
    {
        get => field;
        set => SetProperty(ref field, value);
    }


    public ObservableCollection<IPackageSearchMetadata> Packages
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public IPackageSearchMetadata? SelectedPackage
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                this.ClearVersion();
        }
    }

    public ObservableCollection<VersionInfo> Versions
    {
        get => field;
        set => SetProperty(ref field, value);
    } = new ObservableCollection<VersionInfo>();


    public VersionInfo? SelectedVersion
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    //public bool IsShow
    //{
    //    get => field;
    //    set => SetProperty(ref field, value);
    //} = false;

    [ObservableProperty]
    public partial bool IsShow { get; set; } = false;

    #endregion



    #region 执行命令

    [RelayCommand]
    public async Task SearchPackageAsync(CancellationToken token)
    {
        if (this.sourceRepository is null) return;

        var a = this.PackageName;

        //var findmetadataResource = await this.sourceRepository.GetResourceAsync<PackageMetadataResource>();
        //await findmetadataResource.GetMetadataAsync(this.PackageName, true, true, NullSourceCacheContext.Instance, NuGet.Common.NullLogger.Instance, token);

        var searchResource = await this.sourceRepository.GetResourceAsync<PackageSearchResource>();

        var packages = await searchResource.SearchAsync(this.PackageName, new SearchFilter(true), 0, 10, NuGet.Common.NullLogger.Instance, token);


        this.Packages = new ObservableCollection<IPackageSearchMetadata>(packages);
    }

    [RelayCommand]
    public async Task GetPackageVersionsAsync()
    {
        var versions = await this.SelectedPackage!.GetVersionsAsync();

        this.Versions = new ObservableCollection<VersionInfo>(versions.Reverse());
        this.IsShow = true;
    }


    [RelayCommand]
    public async Task HideAsync()
    {
        this.IsShow = false;
    }


    [RelayCommand]
    public async Task OpenAsync()
    {
        var identity = this.SelectedPackage!.Identity;
        if (this.SelectedVersion is not null)
        {
            identity = new PackageIdentity(this.SelectedPackage.Identity.Id, this.SelectedVersion.Version);
        }

        var nuget = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "nuget");

        var packagePath = Path.Combine(nuget, identity.Id + "." + identity.Version.ToNormalizedString() + ".nupkg");
        if (!File.Exists(packagePath))
        {

            var dir = Path.GetDirectoryName(packagePath);
            if (dir is not null && Directory.Exists(dir))
                _ = Directory.CreateDirectory(dir);



            var downloader = await sourceRepository!.GetResourceAsync<DownloadResource>();

            var cache = Path.Combine(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, "cache");
            using var sourceCacheContext = new SourceCacheContext() { NoCache = false };
            var context = new PackageDownloadContext(sourceCacheContext, cache, true);

            var result = await downloader.GetDownloadResourceResultAsync(identity, context, string.Empty, NuGet.Common.NullLogger.Instance, CancellationToken.None);

            using (var fileStream = File.Open(packagePath, FileMode.OpenOrCreate, FileAccess.Write))
                await result.PackageStream.CopyToAsync(fileStream);


            //var metadata = await sourceRepository.GetResourceAsync<MetadataResource>();
            //var vers = metadata.GetVersions(identity.Id, sourceCacheContext, NuGet.Common.NullLogger.Instance, CancellationToken.None);

        }

        //using (var stream = new FileStream(packagePath, FileMode.Open, FileAccess.Read))
        //{
        //    using (PackageArchiveReader reader = new PackageArchiveReader(stream))
        //    {
        //        var nuspec = reader.NuspecReader;
        //        var d = nuspec.GetContentFiles().ToList();

        //        var items = reader.GetContentItems();
        //    }


        //    //using (var zip = new ZipArchive(stream))
        //    //{
        //    //    var content = zip.Entries.ToList();
        //    //}
        //}


    }




    [RelayCommand]
    public async Task ClearSelectedPackageAsync()
    {
        this.SelectedPackage = null;
    }





    #endregion


    private void ClearVersion()
    {
        this.Versions.Clear();
        this.IsShow = false;
        this.SelectedVersion = null;
    }

}
