using System;
using System.Collections.Generic;
using System.Text;


namespace projet0.Application.Common.Models.Pagination
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        // Méthodes statiques pour faciliter la création
        public static PagedResult<T> Create(
            List<T> items,
            int totalCount,
            int page,
            int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // Pour mapper les items
        public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapFunction)
        {
            return new PagedResult<TResult>
            {
                Items = Items.Select(mapFunction).ToList(),
                TotalCount = TotalCount,
                Page = Page,
                PageSize = PageSize
            };
        }
    }
}
