
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;
using Uno.Extensions.Specialized;

namespace XamlViewer.Presentation;

public partial class WorkViewModel : ObservableObject, IDisposable
{
    //内部成员
    private IDispatcher dispatcher => main.Dispatcher;
    private readonly MainViewModel main;
    private readonly WorkEntity entity;
    private FileSystemWatcher? watcher;

    //隐藏后编辑区长度缓存
    private GridLength? hideEditerLength;
    private GridLength? hideViewerLength;

    private long lastWriteTimeTicks = 0;
    //xaml dataContext 数据
    private object? dataContext;

    private ShowEntity? cache;

    public WorkViewModel(MainViewModel main, WorkEntity entity)
    {
        this.ShowEntities = new ObservableCollection<ShowEntity>(main.AppConfig.Value?.Shows ?? Enumerable.Empty<ShowEntity>());
        this.main = main;
        this.entity = entity;

        this.SelectedShowEntity = this.ShowEntities!.FirstOrDefault();

        _ = this.InitializeAsync();
    }


    private async Task InitializeAsync()
    {
        List<Task> awaitTasks = new List<Task>();
        if (this.entity.Mode == WorkMode.File)
        {
            _ = this.Watcher();
            awaitTasks.Add(this.ReadFileAsync());
        }

        Task.WaitAll(awaitTasks.ToArray());

        await this.ReaderXamlAsync();

        //SinglePaneChanged(this.main.ListDetailMode);

    }

    #region 绑定属性


    public IconSource Icon => new SymbolIconSource() { Symbol = this.entity.IsFile ? Symbol.OpenFile : Symbol.Document };

    public string Title => this.entity.Title!;

    public bool IsFile => this.entity.IsFile;
    public string? Path => this.entity.Path;

    public string? EditText
    {
        get => this.entity.Text;
        //set
        //{
        //    if (SetProperty(this.entity.Text, value, this.entity, (e, n) => e.Text = n))
        //        _ = this.ReaderXamlAsync();
        //}
        set => SetProperty(this.entity.Text, value, this.entity, (e, n) => e.Text = n);
    }

    public string? JsonText
    {
        get => this.entity.Json;
        set
        {
            if (SetProperty(this.entity.Json, value, this.entity, (e, n) => e.Json = n))
                JsonTextChanged(value);
        }
    }

    public GridLength ViewerLength
    {
        get => field;
        set => SetProperty(ref field, value);
    } = new GridLength(1, GridUnitType.Star);

    public GridLength EditerLength
    {
        get => field;
        set => SetProperty(ref field, value);
    } = new GridLength(1, GridUnitType.Star);


    public bool IsShow
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                IsShowChanged(value);
        }
    } = true;

    public bool IsShowSplitter
    {
        get => field;
        set => SetProperty(ref field, value);
    } = true;

    public FrameworkElement? DesignContent
    {
        get => field;
        set => SetProperty(ref field, value);
    }



    public int ContentHeight
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public int ContentWidth
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public bool SizeIsEnabled
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<ShowEntity> ShowEntities
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public ShowEntity? SelectedShowEntity
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                OnSelectedShowEntityChanged(value);
        }

    }


    public double RotateValue
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                this.ReverseRotateValue = -value;
        }
    } = -90;

    public double ReverseRotateValue
    {
        get => field;
        set => SetProperty(ref field, value);
    } = 90;


    public int DesignerRow
    {
        get => field;
        set => SetProperty(ref field, value);
    } = 0;

    public int EditorRow
    {
        get => field;
        set => SetProperty(ref field, value);
    } = 2;



    public bool IsSplit
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
                SplitViewChanged(value);
        }
    } = true;


    public bool IsShowViewer
    {
        get => field;
        set => SetProperty(ref field, value);
    } = true;


    //public bool ViewerShowValue
    //{
    //    get => field;
    //    set
    //    {
    //        if (SetProperty(ref field, value) && !this.IsSplit)
    //        {
    //            //判断是否是拆分才做业务，如果是拆分，那么修改显示区的显示值不修改，否则修改，
    //            this.IsShowViewer = value;
    //        }
    //    }
    //}

    public int WorkSelectedIndex
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value) && !this.IsSplit)
                this.IsShowViewer = value == 2;
        }
    } = 0;


    #endregion

    #region 执行命令


    [RelayCommand]
    public void Rotate()
    {
        this.RotateValue = (this.RotateValue - 90) % 180;
    }

    [RelayCommand]
    public void Switch()
    {
        var r = this.DesignerRow;
        this.DesignerRow = this.EditorRow;
        this.EditorRow = r;
    }


    //[RelayCommand]
    //public void EditTextFastChanged(string text) => this.EditText = text;


    #endregion

    #region 属性触发变更


    public void IsShowChanged(bool isShow)
    {
        if (!isShow)
        {
            //隐藏时缓存长度用来还原
            this.hideEditerLength = this.EditerLength;

            this.EditerLength = new GridLength(0, GridUnitType.Pixel);
        }
        else
        {
            this.EditerLength = this.hideEditerLength ?? new GridLength(1, GridUnitType.Star);
        }
    }



    private void OnSelectedShowEntityChanged(ShowEntity? newValue)
    {
        if (newValue == null) return;

        //新值是否时响应模式，如果是且缓存不为空则用缓存替换（高宽可能已经改变了）
        if (!newValue!.IsOnlyRead && cache is not null)
        {
            newValue = cache;
            cache = null;
        }


        //新值如果是非响应模式时，且缓存为空时
        if (newValue.IsOnlyRead && cache is null)
            cache = new ShowEntity("响应缓存", this.ContentWidth, this.ContentHeight, false);



        this.SizeIsEnabled = !newValue!.IsOnlyRead;
        this.ContentHeight = newValue!.Height;
        this.ContentWidth = newValue!.Width;
    }


    public void JsonTextChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var db = System.Text.Json.JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(value);
        dataContext = db!;
        if (this.DesignContent != null) this.DesignContent.DataContext = this.dataContext;
    }


    //public void SinglePaneChanged(bool value)
    //{
    //    if (value)
    //    {
    //        this.hideViewerLength = this.ViewerLength;
    //        this.hideEditerLength = this.EditerLength;
    //        //设置试图区域为0，编辑器最大
    //        this.ViewerLength = new GridLength(0, GridUnitType.Pixel);
    //        this.EditerLength = new GridLength(1, GridUnitType.Star);
    //        this.IsShowSplitter = false;
    //    }
    //    else
    //    {
    //        //还原
    //        this.ViewerLength = this.hideViewerLength ?? new GridLength(1, GridUnitType.Star);
    //        this.EditerLength = this.hideEditerLength ?? new GridLength(1, GridUnitType.Star);
    //        this.hideEditerLength = null;
    //        this.hideViewerLength = null;
    //        this.IsShowSplitter = true;
    //    }
    //}

    public void SplitViewChanged(bool isSplit)
    {
        if (isSplit)
        {
            //如果是拆分，那么设计和编辑不在一个区域，否则在一个区域
            this.DesignerRow = this.EditorRow == 2 ? 0 : 2;
            this.ViewerLength = this.hideViewerLength ?? new GridLength(1, GridUnitType.Star);
            this.hideViewerLength = null;
            this.IsShowViewer = true;
            this.WorkSelectedIndex = 0;
            //this.ViewerShowValue = false;
        }
        else
        {
            this.DesignerRow = this.EditorRow;
            this.hideViewerLength = this.ViewerLength;
            this.ViewerLength = new GridLength(0, GridUnitType.Pixel);
            this.IsShowViewer = this.WorkSelectedIndex == 2;
        }
    }


    #endregion

    private async Task Watcher()
    {

        this.watcher = new FileSystemWatcher();
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Path = System.IO.Path.GetDirectoryName(this.entity.Path)!;
        watcher.Filter = System.IO.Path.GetFileName(this.entity.Path)!;
        watcher.EnableRaisingEvents = true;
        watcher.Changed += Watcher_Changed;

    }



    private async void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        await this.ReadFileAsync();

        await this.ReaderXamlAsync();
    }

    private async Task ReadFileAsync()
    {
        FileInfo fileInfo = new FileInfo(this.entity.Path!);
        if (lastWriteTimeTicks >= fileInfo.LastWriteTime.Ticks) return;

        lastWriteTimeTicks = fileInfo.LastWriteTime.Ticks;

        try
        {

            var value = File.ReadAllText(this.entity.Path!);
            this.EditText = value;
        }
        catch (IOException ioex)
        {
            return;
        }

    }

    public async Task ReaderXamlAsync()
    {
        var text = this.EditText;

        FrameworkElement? ui = null;

        if (!string.IsNullOrWhiteSpace(text))
        {
            object content = null;
            try
            {
                //移除x:Class 属性，在winui3中存在时，报错问题
#if !HAS_UNO_WINUI
                var el = XDocument.Parse(text);
                var attr = XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml");
                el.XPathSelectElements("//*").ForEach(a => a.Attribute(attr)?.Remove());
                text = el.ToString();
#endif


                content = await this.dispatcher.ExecuteAsync<object>((ct) => ValueTask.FromResult(Microsoft.UI.Xaml.Markup.XamlReader.Load(text)));
                //if (content is FrameworkElement u) ui = u;
            }
            catch (Exception ex)
            {
                content = await this.dispatcher.ExecuteAsync<FrameworkElement>((ct) => ValueTask.FromResult<FrameworkElement>(new TextBlock()
                {
                    Text = ex.Message,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    IsTextSelectionEnabled = true,
                }));

            }

            if (content is not null && content is FrameworkElement u) ui = u;
        }



        this.dispatcher.TryEnqueue(() =>
        {
            if (ui is not null)
                ui.DataContext = this.dataContext;
            this.DesignContent = ui;
        });
    }

    public void Dispose()
    {
        this.watcher?.Dispose();
    }
}
