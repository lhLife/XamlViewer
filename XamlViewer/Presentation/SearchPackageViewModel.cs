

using System;
using System.Collections.ObjectModel;
using System.IO.Compression;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI.Collections;
using Microsoft.VisualBasic;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Windows.ApplicationModel;

namespace XamlViewer.Presentation;
public partial class SearchPackageViewModel : ObservableObject, CommunityToolkit.WinUI.Collections.IIncrementalSource<IPackageSearchMetadata>

{
    private SourceRepository? sourceRepository;
    private readonly IOptions<AppConfig> appConfig;
    private readonly IWritableOptions<AppSettings> writePackageSettings;
    private readonly INavigator navigator;

    public SearchPackageViewModel(
        IOptions<AppConfig> appConfig,
        IWritableOptions<AppSettings> writePackageSettings,
        INavigator navigator)
    {

        _ = InitializeAsync();
        this.appConfig = appConfig;
        this.writePackageSettings = writePackageSettings;
        this.navigator = navigator;
    }

    private async Task InitializeAsync()
    {
        var nugetSettings = NuGet.Configuration.Settings.LoadDefaultSettings(null);
        this.PackageSources = new ObservableCollection<NuGet.Configuration.PackageSource>(NuGet.Configuration.PackageSourceProvider.LoadPackageSources(nugetSettings));
        this.SelectedPackageSource = this.PackageSources.FirstOrDefault();

        if (this.SelectedPackageSource is not null)
            sourceRepository = Repository.CreateSource(Repository.Provider.GetCoreV3(), this.SelectedPackageSource);


        await SearchPackageAsync();
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
    public async Task SearchPackageAsync(/*CancellationToken token*/)
    {
        if (this.sourceRepository is null) return;

        var a = this.PackageName;

        //var findmetadataResource = await this.sourceRepository.GetResourceAsync<PackageMetadataResource>();
        //await findmetadataResource.GetMetadataAsync(this.PackageName, true, true, NullSourceCacheContext.Instance, NuGet.Common.NullLogger.Instance, token);

        //var searchResource = await this.sourceRepository.GetResourceAsync<PackageSearchResource>();

        //var packages = await searchResource.SearchAsync(this.PackageName, new SearchFilter(true), 0, 10, NuGet.Common.NullLogger.Instance, token);



        this.Packages = new IncrementalLoadingCollection<SearchPackageViewModel, IPackageSearchMetadata>(this, 20);
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


        var entity = new PackageEntity()
        {
            Version = identity.Version.ToString(),
            Name = identity.Id,
            Framework = appConfig.Value.DefaultDllDir,
            Dir = appConfig.Value?.DefaultDllDir
        };

        var ls = new List<PackageEntity>(this.writePackageSettings.Value?.Packages ?? Enumerable.Empty<PackageEntity>());

        if (!ls.Any(a => entity.Name.Equals(a.Name)))
        {
            ls.Add(entity);

            await writePackageSettings.UpdateAsync(a => a with { Packages = ls.ToArray() });
        }



        await this.navigator.NavigateBackAsync(this, qualifier: Qualifiers.None);


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

    public async Task<IEnumerable<IPackageSearchMetadata>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var searchResource = await this.sourceRepository!.GetResourceAsync<PackageSearchResource>();

        var packages = await searchResource.SearchAsync(this.PackageName, new SearchFilter(true), pageIndex * pageSize, pageSize, NuGet.Common.NullLogger.Instance, cancellationToken);

        return packages ?? Enumerable.Empty<IPackageSearchMetadata>();
    }
}
