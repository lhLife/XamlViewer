using CommunityToolkit.WinUI;

namespace XamlViewer.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SearchPackagePage : Page
{
    //public SearchPackageViewModel ViewModel => this.DataContext as SearchPackageViewModel;
    public SearchPackagePage()
    {
        this.InitializeComponent();

    }

    private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        searchButton.Command!.Execute(null);
    }

}
