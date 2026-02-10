using Microsoft.EntityFrameworkCore;
using projet0.Application.Common.Models.Pagination;

namespace projet0.Application.Extensions
{
    public static class QueryableExtensions
    {
        // Extension principale
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source,
            PagedRequest request)
        {
            // 1. Appliquer la recherche si nécessaire
            var query = source;

            // 2. Compter le total (avant pagination)
            var totalCount = await query.CountAsync();

            // 3. Appliquer le tri
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = ApplySorting(query, request.SortBy, request.SortDescending);
            }

            // 4. Appliquer la pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return PagedResult<T>.Create(items, totalCount, request.Page, request.PageSize);
        }

        // Méthode pour le tri dynamique (simple version)
        private static IQueryable<T> ApplySorting<T>(
            IQueryable<T> query,
            string sortBy,
            bool descending)
        {
            // Pour un projet PFE, vous pouvez utiliser System.Linq.Dynamic.Core
            // ou implémenter une version simple

            return query; // À améliorer selon vos besoins
        }

        // Version avec filtre
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source,
            Func<IQueryable<T>, IQueryable<T>>? filter = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = filter != null ? filter(source) : source;

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PagedResult<T>.Create(items, totalCount, page, pageSize);
        }
    }
}