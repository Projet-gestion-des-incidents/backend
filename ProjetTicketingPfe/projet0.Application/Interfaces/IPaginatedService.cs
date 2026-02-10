using projet0.Application.Common.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Text;


namespace projet0.Application.Common.Interfaces
{
    public interface IPaginatedService<T, in TRequest>
        where TRequest : PagedRequest
    {
        Task<PagedResult<T>> GetPagedAsync(TRequest request);
    }
}
