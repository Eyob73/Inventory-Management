namespace Inventory_Management.Application.DTOs.Common;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _page = 1;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageNumber { init => _page = value < 1 ? 1 : value; }
    public int PageIndex { init => _page = value < 1 ? 1 : value; }

    public int PageSize
    {
        get => _pageSize;
        init =>
            _pageSize =
                value < 1 ? 20
                : value > MaxPageSize ? MaxPageSize
                : value;
    }

    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string OrderBy { get; init; } = "Title";
    public bool Descending { get; init; }
}
