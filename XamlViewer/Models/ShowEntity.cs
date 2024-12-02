
namespace XamlViewer.Models;

public record ShowEntity(string Name, int Width, int Height, bool IsOnlyRead = true)
{
    public ShowEntity()
        : this(string.Empty, 0, 0, true)
    {

    }
}
