namespace HonypotTrack.Application.Commons.Bases;

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResponse<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalRecords)
    {
        return new PagedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords
        };
    }
}
