using Microsoft.EntityFrameworkCore;
using projet0.Application.Common.Models.Pagination;
using System.Linq.Expressions;

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
            return query; 
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

        // ✅ AJOUTER CETTE MÉTHODE D'EXTENSION ICI
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);
            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right), parameter);
        }

        // Classe helper privée pour la méthode AndAlso
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                return node == _oldValue ? _newValue : base.Visit(node);
            }
        }
    }
}