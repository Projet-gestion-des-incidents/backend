using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface ITPERepository : IGenericRepository<TPE>
    {
        Task<IEnumerable<TPE>> GetByCommercantIdAsync(Guid commercantId);
        Task<bool> IsNumSerieUniqueForModeleAsync(string numSerie, ModeleTPE modele, Guid? excludeId = null);
        Task<IQueryable<TPE>> QueryWithDetailsAsync();
        Task<int> GetNextSequenceNumberAsync(ModeleTPE modele);
        Task<string> GenerateNumSerieAsync(ModeleTPE modele);
    }
}