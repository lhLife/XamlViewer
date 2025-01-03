using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls.Primitives;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Uno.Extensions.Navigation;
using Uno.Extensions.Specialized;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using XamlViewer.Extensions;
using XamlViewer.Models;

namespace XamlViewer.Presentation;
public partial class SettingViewModel : ObservableObject
{
    private readonly IAppHostEnvironment environment;
    private readonly IOptions<AppConfig> appConfig;
    private readonly INavigator navigator;
    private readonly ILogger<SettingViewModel> logger;
    private readonly IWritableOptions<AppSettings> writePackageSettings;
    private SourceRepository? sourceRepository;





    public SettingViewModel(
        IAppHostEnvironment environment,
        IOptions<AppConfig> appConfig,
        INavigator navigator,
        IDispatcher dispatcher,
        ILogger<SettingViewModel> logger,
        IWritableOptions<AppSettings> writePackageSettings)
    {
        this.environment = environment;
        this.appConfig = appConfig;
        this.navigator = navigator;
        this.logger = logger;
        this.writePackageSettings = writePackageSettings;

        this.Packages = new ObservableCollection<PackageEntity>(this.writePackageSettings.Value?.Packages ?? Enumerable.Empty<PackageEntity>());

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

    [ObservableProperty]
    public partial ObservableCollection<PackageEntity> Packages { get; set; }


    [ObservableProperty]
    public partial ObservableCollection<NuGet.Configuration.PackageSource> PackageSources { get; set; }

    public NuGet.Configuration.PackageSource? SelectedPackageSource
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                sourceRepository = Repository.CreateSource(Repository.Provider.GetCoreV3(), value);
        }
    }


    #endregion


    #region 执行命令

    [RelayCommand]
    public async Task AddPackageAsync()
    {
        var response = await this.navigator.NavigateViewModelForResultAsync<PackageInfoViewModel, PackageEntity>(this, qualifier: Qualifiers.Dialog, data: null);

        var option = await response!.Result;
        var result = option.SomeOrDefault();

        if (result is not null)
        {
            this.Packages.Add(result!);

            await writePackageSettings.UpdateAsync(a => a with { Packages = this.Packages.ToArray() });
        }

    }


    [RelayCommand]
    public async Task RemovePackageAsync(PackageEntity selected)
    {
        this.Packages?.Remove(selected);

        await writePackageSettings.UpdateAsync(a => a with { Packages = this.Packages.ToArray() });
    }

    [RelayCommand]
    public async Task EditPackageAsync(PackageEntity selected)
    {

        var response = await this.navigator.NavigateViewModelForResultAsync<PackageInfoViewModel, PackageEntity>(this, qualifier: Qualifiers.Dialog, data: selected);

        var option = await response!.Result;
        var result = option.SomeOrDefault();

        if (result is not null)
        {
            //this.Packages.Add(result!);

            //this.Packages.Replace(a => a.GetHashCode() == selected.GetHashCode(), selected);

            this.Packages[this.Packages.IndexOf(selected)] = result;

            await writePackageSettings.UpdateAsync(a => a with { Packages = this.Packages.ToArray() });

        }

    }


    [RelayCommand]
    public async Task SearchPackageAsync()
    {
        await this.navigator.NavigateViewModelAsync<SearchPackageViewModel>(this, qualifier: Qualifiers.None);
    }


    [RelayCommand]
    public async Task UpdateDependencysAsync(PackageEntity selected)
    {
        var version = NuGet.Versioning.NuGetVersion.Parse(selected.Version);

        //var identity = new PackageIdentity(selected.Name, NuGet.Versioning.NuGetVersion.Parse(selected.Version));

        var dependencyInfoResource = await sourceRepository!.GetResourceAsync<DependencyInfoResource>();
        var idresource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

        var cache = Path.Combine(environment.AppDataPath, "cache");
        using var sourceCacheContext = new SourceCacheContext() { NoCache = false };

        //加载当前所有包信息，不包含选中的这个。
        List<PackageEntity> dependencys = new List<PackageEntity>();

        var noSelecteds = this.Packages.Where(a => a != selected).ToList();
        //noSelecteds.ForEach(a => LoadAllDependencys(dependencys, a));

        LoadAllDependencys(dependencys, noSelecteds);



        PackageEntity entity = selected with { Children = new List<PackageEntity>(), IsLoadDependency = false };



        await LoadDependencysAsync(dependencys, entity, idresource, dependencyInfoResource, sourceCacheContext, NuGetFramework.Parse(selected.Framework ?? appConfig.Value.Framework));

        this.Packages.Replace(a => a == selected, entity);


        await writePackageSettings.UpdateAsync(a => a with { Packages = this.Packages.ToArray() });
    }



    [RelayCommand]
    public async Task LoadPackageAsync()
    {

        List<PackageEntity> dependencys = new List<PackageEntity>();
        List<PackageEntity> sources = this.Packages.ToList();
        this.LoadAllDependencys(dependencys, sources);


        //dependencys = dependencys.WhereToList(a => !a.IsDownload);


        var nuget = Path.Combine(environment.AppDataPath, "nuget");
        var plugins = Path.Combine(environment.AppDataPath, "plugins");



        if (!Directory.Exists(nuget)) Directory.CreateDirectory(nuget);
        if (!Directory.Exists(plugins)) Directory.CreateDirectory(plugins);



        var source = NuGet.Configuration.PackageSourceProvider.LoadPackageSources(NuGet.Configuration.Settings.LoadDefaultSettings(null)).FirstOrDefault();

        var resource = Repository.CreateSource(Repository.Provider.GetCoreV3(), source);


        var cache = Path.Combine(environment.AppDataPath, "cache");
        using var sourceCacheContext = new SourceCacheContext() { NoCache = false };
        var context = new PackageDownloadContext(sourceCacheContext, cache, true);
        var downloader = await resource.GetResourceAsync<NuGet.Protocol.Core.Types.DownloadResource>();


        foreach (var item in dependencys)
        {

            var identity = new NuGet.Packaging.Core.PackageIdentity(item.Name, NuGetVersion.Parse(item.Version));

            var packagePath = Path.Combine(nuget, identity.Id + "." + identity.Version.ToNormalizedString() + ".nupkg");

            if (!File.Exists(packagePath))
            {
                var result = await downloader.GetDownloadResourceResultAsync(identity, context, string.Empty, NuGet.Common.NullLogger.Instance, CancellationToken.None);

                using (var fileStream = File.Open(packagePath, FileMode.OpenOrCreate, FileAccess.Write))
                    await result.PackageStream.CopyToAsync(fileStream);

            }

            var zip = new ZipPackage(packagePath);

            var paths = new List<string>();
            if (!string.IsNullOrEmpty(item.Dir))
            {
                paths = zip.GetFiles(item.Dir).ToList();
            }
            else if (!string.IsNullOrEmpty(item.Framework))
            {
                paths = zip.GetFiles($"{PackagingConstants.Folders.Lib}/{item.Framework}").ToList();
            }
            else
            {

                var libs = zip.GetLibItems();

                var lib = libs
                    .Where(a => NuGetFramework.Parse(item.Framework ?? appConfig.Value.Framework).IsSupport(a.TargetFramework))
                    .OrderByDescending(a => a.TargetFramework.Version)
                    .FirstOrDefault();

                if (lib is not null)
                {
                    paths = lib.Items.ToList();
                }

            }

            paths = paths.Where(a => a.EndsWith(".dll", StringComparison.InvariantCulture)).ToList();

            var s = await zip.CopyFilesAsync(plugins, paths, NuGet.Common.NullLogger.Instance, CancellationToken.None);

        }


        dependencys.ForEach(a => a.IsDownload = true);


        this.Packages = new ObservableCollection<PackageEntity>(this.Packages.Select(a => a with { IsDownload = true }));

        await writePackageSettings.UpdateAsync(a => a with { Packages = this.Packages.ToArray() });

        try
        {

            await Plugins.Load(environment.AppDataPath, logger);
        }
        catch (Exception ex)
        {
        }
    }


    #endregion

    private void LoadAllDependencys(List<PackageEntity> dependencys, List<PackageEntity> entitys)
    {
        foreach (var entity in entitys)
        {
            dependencys.Add(entity);

            LoadAllDependencys(dependencys, entity.Children);
        }

    }


    private async Task LoadDependencysAsync(List<PackageEntity> dependencys, PackageEntity entity, FindPackageByIdResource idresource, DependencyInfoResource dependencyInfoResource, SourceCacheContext sourceCacheContext, NuGetFramework parentFramework)
    {

        var identity = new PackageIdentity(entity.Name, NuGet.Versioning.NuGetVersion.Parse(entity.Version));
        entity.IsLoadDependency = true;

        if (string.IsNullOrEmpty(entity.Framework))
        {
            var info = await idresource.GetDependencyInfoAsync(identity.Id, identity.Version, sourceCacheContext, NuGet.Common.NullLogger.Instance, CancellationToken.None);


            var de = info.DependencyGroups.Where(a => parentFramework.IsSupport(a.TargetFramework)).OrderByDescending(a => a.TargetFramework.Version).FirstOrDefault();

            entity.Framework = de?.TargetFramework.ToString();

        }
        if (!string.IsNullOrEmpty(entity.Framework))
        {
            entity.Dir = $"lib/{entity.Framework}";
        }


        var metadata = await dependencyInfoResource.ResolvePackage(identity, NuGetFramework.Parse(entity.Framework ?? parentFramework.ToString()), sourceCacheContext, NuGet.Common.NullLogger.Instance, CancellationToken.None);


        foreach (var item in metadata.Dependencies)
        {
            var p = new PackageEntity()
            {
                Name = item.Id,
                Version = item.VersionRange.IsMaxInclusive ? item.VersionRange.MaxVersion.ToString() : item.VersionRange.MinVersion.ToString()
            };


            //如果本地有这个包，则不加载依赖
            if (AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name.Equals(item.Id))) continue;

            if (writePackageSettings.Value?.Ignores?.Any(a => a.Equals(item.Id, StringComparison.OrdinalIgnoreCase)) ?? false) continue;

            if (dependencys.Any(a => a.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase))) continue;

            entity.Children.Add(p);
            await LoadDependencysAsync(dependencys, p, idresource, dependencyInfoResource, sourceCacheContext, NuGetFramework.Parse(entity.Framework ?? appConfig.Value.Framework));
        }


    }
}
