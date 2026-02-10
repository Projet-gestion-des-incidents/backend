using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace projet0.Infrastructure.Helpers
{
    public class PaginatedList<T>
    {
        public List<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public PaginatedList(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

        // Méthode principale
        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Valider les paramètres
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // Compter le total
            var totalCount = await source.CountAsync(cancellationToken);

            // Si pas d'éléments, retourner liste vide
            if (totalCount == 0)
            {
                return new PaginatedList<T>(new List<T>(), 0, page, pageSize);
            }

            // Calculer le skip
            var skip = (page - 1) * pageSize;

            // Si skip dépasse le total, ajuster la page
            if (skip >= totalCount)
            {
                page = (int)Math.Ceiling(totalCount / (double)pageSize);
                skip = (page - 1) * pageSize;
            }

            // Récupérer les items
            var items = await source
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedList<T>(items, totalCount, page, pageSize);
        }

        // Version avec tri (CORRIGÉE)
        public static async Task<PaginatedList<T>> CreateAsync<TKey>(
            IQueryable<T> source,
            Expression<Func<T, TKey>> keySelector, // CHANGEMENT ICI : Expression<>
            bool descending = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var orderedSource = descending
                ? source.OrderByDescending(keySelector) // IQueryable, pas IOrderedEnumerable
                : source.OrderBy(keySelector);

            return await CreateAsync(orderedSource, page, pageSize, cancellationToken);
        }

        // Version avec tri par propriété (alternative)
        public static async Task<PaginatedList<T>> CreateSortedAsync(
            IQueryable<T> source,
            string sortBy = "Id",
            bool descending = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Pour simplifier, retourne sans tri dynamique
            // Vous pouvez utiliser System.Linq.Dynamic.Core pour le tri dynamique

            return await CreateAsync(source, page, pageSize, cancellationToken);
        }

        // Mapper vers un autre type
        public PaginatedList<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            var mappedItems = Items.Select(mapper).ToList();
            return new PaginatedList<TResult>(mappedItems, TotalCount, Page, PageSize);
        }

        // Pour l'API response
        public object ToApiResponse(string? message = null)
        {
            return new
            {
                Data = Items,
                Pagination = new
                {
                    Page,
                    PageSize,
                    TotalCount,
                    TotalPages,
                    HasPreviousPage,
                    HasNextPage
                },
                Message = message ?? $"Total: {TotalCount} élément(s)",
                Timestamp = DateTime.UtcNow
            };
        }
    }
}