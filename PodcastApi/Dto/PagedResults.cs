namespace PodcastApi.Dto;

public record PagedResults<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);