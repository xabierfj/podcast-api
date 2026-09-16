namespace PodcastApi.Dto;

public record PagedResults<T>(IReadOnlyList<T> Items, int TotalPages, int CurrentPage, int PageSize);