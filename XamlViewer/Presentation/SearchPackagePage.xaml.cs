
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml.Input;

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

    private void TextBox_TextCompositionChanged(TextBox sender, TextCompositionChangedEventArgs args)
    {

    }

    private void TextBox_TextCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
    {

    }

    private void TextBox_CandidateWindowBoundsChanged(TextBox sender, CandidateWindowBoundsChangedEventArgs args)
    {

    }

    private void TextBox_TextCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {

    }
}
