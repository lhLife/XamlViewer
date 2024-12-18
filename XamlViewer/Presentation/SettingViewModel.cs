using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using Uno.Extensions.Navigation;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace XamlViewer.Presentation;
public partial class SettingViewModel : ObservableObject
{
    private readonly INavigator navigator;
    private readonly IWritableOptions<AppSettings> writePackageSettings;





    public SettingViewModel(
        INavigator navigator,
        IDispatcher dispatcher,
        IWritableOptions<AppSettings> writePackageSettings)
    {
        this.navigator = navigator;
        this.writePackageSettings = writePackageSettings;

        this.Packages = new ObservableCollection<PackageEntity>(this.writePackageSettings.Value?.Packages ?? Enumerable.Empty<PackageEntity>());

    }



    #region 绑定属性

    public ObservableCollection<PackageEntity> Packages
    {
        get => field;
        set => SetProperty(ref field, value);
    }
    #endregion


    #region 执行命令

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


    [RelayCommand]
    public async Task SearchPackageAsync()
    {
        await this.navigator.NavigateViewModelAsync<SearchPackageViewModel>(this, qualifier: Qualifiers.None);
    }

    #endregion
}
