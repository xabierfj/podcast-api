namespace PodcastApi.Dto;

public record GuestWithEpisodesDto
{
    public int GuestId { get; init; }
    public string Name { get; init; } = "";
    public List<EpisodeDto> Episodes { get; init; } = new();
}
