

namespace XamlViewer.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WorkPage : Page
{
    public WorkPage()
    {
        this.InitializeComponent();
    }

    private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        var d = (float)(args.NewValue / 100);
        if (d != this.content.ZoomFactor)
            this.content.ChangeView(0, 0, d);


        this.Log()?.LogDebug($" {this.content.ZoomFactor}");
    }
}
