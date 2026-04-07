using Microsoft.EntityFrameworkCore;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projet0.Infrastructure.Repositories
{
    public class TPERepository : GenericRepository<TPE>, ITPERepository
    {
        public TPERepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TPE>> GetByCommercantIdAsync(Guid commercantId)
        {
            return await _dbSet
                .Where(t => t.CommercantId == commercantId)
                .OrderBy(t => t.NumSerie)
                .ToListAsync();
        }

        public async Task<bool> IsNumSerieUniqueForModeleAsync(string numSerie, ModeleTPE modele, Guid? excludeId = null)
        {
            var query = _dbSet.Where(t => t.NumSerie == numSerie && t.Modele == modele);
            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            return !await query.AnyAsync();
        }

        public async Task<IQueryable<TPE>> QueryWithDetailsAsync()
        {
            return _context.TPEs
                .Include(t => t.Commercant)
                .AsQueryable();
        }

        public async Task<PagedResult<TPE>> GetPagedAsync(TPEPagedRequest request)
        {
            var query = _context.TPEs
                .Include(t => t.Commercant)
                .AsQueryable();

            // Appliquer les filtres
            if (request.Modele.HasValue)
                query = query.Where(t => t.Modele == request.Modele.Value);

            if (request.CommercantId.HasValue)
                query = query.Where(t => t.CommercantId == request.CommercantId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(t =>
                    t.NumSerie.ToLower().Contains(term) ||
                    t.NumSerieComplet.ToLower().Contains(term) ||
                    (t.Commercant != null &&
                        (t.Commercant.Nom.ToLower().Contains(term) ||
                         t.Commercant.Prenom.ToLower().Contains(term) ||
                         (t.Commercant.Nom + " " + t.Commercant.Prenom).ToLower().Contains(term)))
                );
            }

            // Compter le total
            var totalCount = await query.CountAsync();

            // Appliquer le tri
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            // Appliquer la pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<TPE>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        private IQueryable<TPE> ApplySorting(IQueryable<TPE> query, string? sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "NumSerieComplet";

            var sortByLower = sortBy.ToLower();

            return (sortByLower, descending) switch
            {
                ("numserie", false) => query.OrderBy(t => t.NumSerie),
                ("numserie", true) => query.OrderByDescending(t => t.NumSerie),

                ("numseriecomplet", false) => query.OrderBy(t => t.NumSerieComplet),
                ("numseriecomplet", true) => query.OrderByDescending(t => t.NumSerieComplet),

                ("modele", false) => query.OrderBy(t => t.Modele),
                ("modele", true) => query.OrderByDescending(t => t.Modele),

                ("commercant", false) => query.OrderBy(t => t.Commercant.Nom).ThenBy(t => t.Commercant.Prenom),
                ("commercant", true) => query.OrderByDescending(t => t.Commercant.Nom).ThenByDescending(t => t.Commercant.Prenom),

                _ => query.OrderBy(t => t.NumSerieComplet)
            };
        }
        // Dans TPERepository.cs
        public async Task<int> GetNextSequenceNumberAsync(ModeleTPE modele)
        {
            // Récupérer le dernier numéro de série pour ce modèle
            var dernierTPE = await _dbSet
                .Where(t => t.Modele == modele)
                .OrderByDescending(t => t.NumSerie)
                .FirstOrDefaultAsync();

            if (dernierTPE == null)
                return 1;

            // Extraire le numéro séquentiel (les 3 derniers chiffres par exemple)
            if (int.TryParse(dernierTPE.NumSerie, out int dernierNumero))
            {
                return dernierNumero + 1;
            }

            return 1;
        }

        public async Task<string> GenerateNumSerieAsync(ModeleTPE modele)
        {
            var nextNumber = await GetNextSequenceNumberAsync(modele);
            // Formater avec 6 chiffres (ex: 000001, 000002, ...)
            return nextNumber.ToString("D6");
        }
    }
}