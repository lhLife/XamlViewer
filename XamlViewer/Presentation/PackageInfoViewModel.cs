using Uno.UI.HotDesign;

namespace XamlViewer.Presentation;
public partial class PackageInfoViewModel : ObservableObject
{
    private readonly INavigator navigator;

    //public PackageEntity PackageInclude { get; private set; }

    public PackageInfoViewModel(INavigator navigator, PackageEntity? packageInclude)
    {
        this.navigator = navigator;
        //this.PackageInclude = packageInclude ?? new PackageEntity();
        this.Name = packageInclude?.Name;
        this.Version = packageInclude?.Version;
        this.Dir = packageInclude?.Dir;
        this.Framework = packageInclude?.Framework;
    }

    ////[Required]
    ////[MinLength(2)]
    //[ObservableProperty]
    //private string name;
    ////public partial string Name { get; set; }


    ////[Required]
    ////[MinLength(2)]
    //[ObservableProperty]
    //private string version;
    ////public partial string Version { get; set; }

    ////[Required]
    ////[MinLength(2)]
    //[ObservableProperty]
    //private string search;
    ////public partial string Search { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? Version { get; set; }

    [ObservableProperty]
    public partial string? Dir { get; set; }

    [ObservableProperty]
    public partial string? Framework { get; set; }


    [RelayCommand]
    public async Task CompleteAsync()
    {
        //await this.navigator.NavigateBackWithResultAsync<PackageInclude>(this, data: this.PackageInclude);
        //NavigatorFactory

        //await this.navigator.NavigateBackAsync(this);
        var data = new PackageEntity() { Name = this.Name, Version = this.Version, Dir = this.Dir, Framework = this.Framework };
        await this.navigator.NavigateBackWithResultAsync(this, data: data);


    }
}
