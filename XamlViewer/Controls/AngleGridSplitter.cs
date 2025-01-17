namespace XamlViewer.Controls;

public partial class AngleGridSplitter : GridSplitter
{

    public double Angle
    {
        get
        {
            return (double)GetValue(AngleProperty);
        }
        set
        {
            SetValue(AngleProperty, value);
        }
    }

    public static DependencyProperty AngleProperty { get; } = DependencyProperty.Register("Angle", typeof(double), typeof(FrameworkElement), new PropertyMetadata(0.0, AttachChanged));



    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {

#if ANDROID || IOS
        base.OnManipulationDelta(e);

#else


        // We use Truncate here to provide 'snapping' points with the DragIncrement property
        // It works for both our negative and positive values, as otherwise we'd need to use
        // Ceiling when negative and Floor when positive to maintain the correct behavior.
        var horizontalChange =
            Math.Truncate(e.Cumulative.Translation.X / DragIncrement) * DragIncrement;
        var verticalChange =
            Math.Truncate(e.Cumulative.Translation.Y / DragIncrement) * DragIncrement;

        // Important: adjust for RTL language flow settings and invert horizontal axis
#if !HAS_UNO
        if (this.FlowDirection == FlowDirection.RightToLeft)
        {
            horizontalChange *= -1;
        }
#endif
        switch (this.Angle)
        {
            case -270:
            case 90:
                var a = horizontalChange > 0 ? -horizontalChange : Math.Abs(horizontalChange);
                horizontalChange = verticalChange > 0 ? -verticalChange : Math.Abs(verticalChange);
                verticalChange = a;
                break;
            case -180:
            case 180:
                horizontalChange = horizontalChange > 0 ? -horizontalChange : Math.Abs(horizontalChange);
                verticalChange = verticalChange > 0 ? -verticalChange : Math.Abs(verticalChange);
                break;
            case -90:
            case 270:
                var c = horizontalChange;
                horizontalChange = verticalChange;
                verticalChange = c;
                break;
        }


        if (Orientation == Orientation.Vertical)
        {
            if (!OnDragHorizontal(horizontalChange))
            {
                return;
            }
        }
        else if (Orientation == Orientation.Horizontal)
        {
            if (!OnDragVertical(verticalChange))
            {
                return;
            }
        }
        
#endif
    }


    private static void AttachChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        
    }


}
