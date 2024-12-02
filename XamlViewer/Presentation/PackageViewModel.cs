using Uno.UI.HotDesign;

namespace XamlViewer.Presentation;
public partial class PackageViewModel : ObservableObject
{
    private readonly INavigator navigator;

    public PackageEntity PackageInclude { get; private set; }

    public PackageViewModel(INavigator navigator, PackageEntity? packageInclude)
    {
        this.navigator = navigator;
        this.PackageInclude = packageInclude ?? new PackageEntity();
        //this.Name = packageInclude?.Name;
        //this.Version = packageInclude?.Version;
        //this.Search = packageInclude?.Search;
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


    public string? Name
    {
        get => this.PackageInclude.Name;
        set => SetProperty(field, value, this.PackageInclude, (a, b) => this.PackageInclude.Name = value);
    }

    public string? Version
    {
        get => this.PackageInclude.Version;
        set => SetProperty(field, value, this.PackageInclude, (a, b) => this.PackageInclude.Version = value);
    }
    public string? Search
    {
        get => this.PackageInclude.Search;
        set => SetProperty(field, value, this.PackageInclude, (a, b) => this.PackageInclude.Search = value);
    }



    [RelayCommand]
    public async Task CompleteAsync()
    {
        //await this.navigator.NavigateBackWithResultAsync<PackageInclude>(this, data: this.PackageInclude);
        //NavigatorFactory

        //await this.navigator.NavigateBackAsync(this);

        await this.navigator.NavigateBackWithResultAsync(this, data: this.PackageInclude);


    }
}
