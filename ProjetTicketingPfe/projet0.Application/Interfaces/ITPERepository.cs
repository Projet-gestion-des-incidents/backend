// projet0.Application/Interfaces/ITPERepository.cs
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace projet0.Application.Interfaces
{
    public interface ITPERepository : IGenericRepository<TPE>
    {
        Task<IEnumerable<TPE>> GetByCommercantIdAsync(Guid commercantId);
        Task<bool> IsNumSerieUniqueAsync(string numSerie, Guid? excludeId = null);
    }
}