using projet0.Application.Common.Models.Pagination;


namespace projet0.Application.Common.Interfaces
{
    public interface IPaginatedService<T, in TRequest>
        where TRequest : PagedRequest
    {
        Task<PagedResult<T>> GetPagedAsync(TRequest request);
    }
}
