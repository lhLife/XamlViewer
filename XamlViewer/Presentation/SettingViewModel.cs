using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using Uno.Extensions.Navigation;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace XamlViewer.Presentation;
public partial class SettingViewModel : ObservableObject
{
    private readonly INavigator navigator;
    private readonly IWritableOptions<AppSettings> writePackageSettings;

    //public PackageInclude[] Packages
    //{
    //    get;
    //    set => SetProperty(ref field, value);
    //}


    //[ObservableProperty]
    //private ObservableCollection<PackageInclude> packages = new ObservableCollection<PackageInclude>();

    public ObservableCollection<PackageEntity> Packages
    {
        get => field;
        set => SetProperty(ref field, value);
    }




    public SettingViewModel(
        INavigator navigator,
        IDispatcher dispatcher,
        IWritableOptions<AppSettings> writePackageSettings)
    {
        this.navigator = navigator;
        this.writePackageSettings = writePackageSettings;

        this.Packages = new ObservableCollection<PackageEntity>(this.writePackageSettings.Value?.Packages ?? Enumerable.Empty<PackageEntity>());

    }


    [RelayCommand]
    public async Task AddPackageAsync()
    {
        var response = await this.navigator.NavigateViewModelForResultAsync<PackageViewModel, PackageEntity>(this, qualifier: Qualifiers.Dialog, data: null);

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
}
