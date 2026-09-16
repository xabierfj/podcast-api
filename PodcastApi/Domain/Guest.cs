namespace PodcastApi.Domain;

public class Guest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Episode> Episodes { get; set; } = new();
}