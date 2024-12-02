using System.Collections.ObjectModel;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Uno.Extensions;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

namespace XamlViewer.Presentation;

public partial class MainViewModel : ObservableObject
{
    private INavigator _navigator;
    private readonly Window window;
    private static int _index = 0;
    private readonly string? _defXaml;

    public IDispatcher Dispatcher { get; private set; }
    public IOptions<AppConfig> AppConfig { get; private set; }


    public MainViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appConfig,
        IDispatcher dispatcher,
        INavigator navigator,
        Window window,
        IConfiguration configuration,
        IWritableOptions<AppSettings> writePackageSettings)
    {
        this.AppConfig = appConfig;
        this.Dispatcher = dispatcher;
        this._navigator = navigator;
        this.window = window;
        _defXaml = configuration.GetValue<string>("files.DefaultXaml", string.Empty);

        this.Works = new ObservableCollection<WorkViewModel>(
            WrapViewModel(new WorkEntity[] { CreateEmpty() })
        );

        this.SelectedLast();
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

