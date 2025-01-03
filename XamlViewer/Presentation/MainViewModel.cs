using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Uno.Extensions;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using XamlViewer.Extensions;

namespace XamlViewer.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly IAppHostEnvironment environment;
    private INavigator _navigator;
    private readonly Window window;
    private readonly ILogger<MainViewModel> logger;
    private readonly IWritableOptions<AppSettings> writePackageSettings;
    private static int _index = 0;
    private readonly string? _defXaml;

    public IDispatcher Dispatcher { get; private set; }
    public IOptions<AppConfig> AppConfig { get; private set; }


    public MainViewModel(
        IAppHostEnvironment environment,
        IStringLocalizer localizer,
        IOptions<AppConfig> appConfig,
        IDispatcher dispatcher,
        INavigator navigator,
        Window window,
        ILogger<MainViewModel> logger,
        IConfiguration configuration,
        IWritableOptions<AppSettings> writePackageSettings)
    {
        this.environment = environment;
        this.AppConfig = appConfig;
        this.Dispatcher = dispatcher;
        this._navigator = navigator;
        this.window = window;
        this.logger = logger;
        this.writePackageSettings = writePackageSettings;
        _defXaml = configuration.GetValue<string>("files.DefaultXaml", string.Empty);

        this.Works = new ObservableCollection<WorkViewModel>(
            WrapViewModel(new WorkEntity[] { CreateEmpty() })
        );

        this.SelectedLast();

        _ = InitializeAsync();
    }


    private void LoadAllDependencys(List<PackageEntity> dependencys, List<PackageEntity> entitys)
    {
        foreach (var entity in entitys)
        {
            dependencys.Add(entity);

            LoadAllDependencys(dependencys, entity.Children);
        }

    }

    private async Task InitializeAsync()
    {
        await Plugins.Load(environment.AppDataPath, logger);
    }



    #region 绑定属性

    public ObservableCollection<WorkViewModel> Works
    {
        get => field;
        set => SetProperty(ref field, value);
    }


    public WorkViewModel? SelectedWork
    {
        get => field;
        set => SetProperty(ref field, value);
    }


    #endregion

    #region 执行命令

    [RelayCommand]
    public async Task SettingAsync()
    {
        //修复TabView因选中时跳转页面，返回后出现bug的问题，因默认已经选中了某个项，但是数据还未加载中，造成选中项没有
#if !HAS_UNO_WINUI
        this.SelectedWork = null;
#endif

        await this._navigator!.NavigateViewModelAsync<SettingViewModel>(this, qualifier: Qualifiers.None);
    }

    [RelayCommand(CanExecute = nameof(CanCloseAsync))]
    public async Task CloseAsync(TabViewTabCloseRequestedEventArgs? workEntity)
    {
        var item = workEntity!.Item as WorkViewModel;
        this.Works.Remove(item!);

        item!.Dispose();
    }

    private bool CanCloseAsync(TabViewTabCloseRequestedEventArgs? workEntity)
    {
        return workEntity?.Item != null;
    }


    [RelayCommand]
    public async Task AddAsync()
    {
        this.Works.AddRange(WrapViewModel(CreateEmpty()));

        this.SelectedLast();
    }

    [RelayCommand]
    public async Task AddFileAsync()
    {
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;

#if DESKTOP
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Unspecified;
#else 

        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
#endif
        //openPicker.SuggestedStartLocation = OperatingSystem.IsWindows() ? PickerLocationId.DocumentsLibrary : PickerLocationId.Unspecified;
        openPicker.FileTypeFilter.Add(".xaml");


        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();

        if (file == null) return;

        //文件已存在时退出
        if (this.Works.Any(a => a.Path == file.Path)) return;

        this.Works.AddRange(WrapViewModel(new WorkEntity()
        {
            Title = file.Name,
            Mode = WorkMode.File,
            Path = file.Path,
        }));

        this.SelectedLast();

    }



    [RelayCommand]
    public async Task PinAsync(bool isChecked)
    {
        await this.Dispatcher.ExecuteAsync(() =>
        {
            var p = OverlappedPresenter.Create();
            p.IsAlwaysOnTop = isChecked;
            window.AppWindow.SetPresenter(p);

        });

    }

    [RelayCommand]
    public async Task ReaderXamlAsync()
    {
        if (this.SelectedWork is null) return;

        await this.SelectedWork!.ReaderXamlAsync();

    }

    #endregion


    private WorkEntity CreateEmpty()
    {
        return new WorkEntity()
        {
            Title = $"新建文件{++_index}",
            Mode = WorkMode.None,
            Text = _defXaml,
        };
    }
    private List<WorkViewModel> WrapViewModel(params WorkEntity[] entities)
    {
        return entities.Select(a => new WorkViewModel(this, a)).ToList();
    }

    private void SelectedLast()
    {
        this.SelectedWork = this.Works.LastOrDefault();
    }

}

