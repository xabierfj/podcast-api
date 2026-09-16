using PodcastApi.Domain;
using PodcastApi.Dto;

namespace PodcastApi.Mappers;

public static class EpisodeMapper
{
    public static EpisodeDto ToEpisodeDto (this Episode e) => new ()
    {
        Id = e.Id ,
        EpisodeNumber = e.EpisodeNumber ,
        Title = e.Title ,
        Description = e.Description ,
        PublicationDate = e.PublicationDate ,
        FormattedEpisodeNumber = e.FormattedEpisodeNumber ,
        FormattedDuration = e.FormattedDuration ,
        IsSpecial = e.IsSpecial ,
        SimpsonsSeason = e.SimpsonsSeason ,
        SimpsonsEpisode = e.SimpsonsEpisode ,
        SimpsonsTitle = e.SimpsonsTitle ,
        SpotifyUrl = e.SpotifyUrl ,
        AudioUrl = e.AudioUrl ,
        ImageUrl = e.ImageUrl ,
        Guests = e.Guests.Select(g => g.Name).ToArray()
    };
}