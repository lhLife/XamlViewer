
namespace XamlViewer.Models;
public class WorkEntity
{
    public string? Title { get; set; }
    public string? Path { get; set; }
    public string? Text { get; set; }
    public WorkMode Mode { get; set; }
    public string? Json { get; set; }
    public bool IsFile => this.Mode != WorkMode.None;

    //public WorkEntity[] Children { get; set; } = new WorkEntity[0];

}


public enum WorkMode
{
    None = 0,
    File = 1,
    Folder = 2,
}
