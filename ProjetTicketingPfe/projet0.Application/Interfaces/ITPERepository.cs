using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace projet0.Application.Interfaces
{
    public interface ITPERepository : IGenericRepository<TPE>
    {
        Task<IEnumerable<TPE>> GetByCommercantIdAsync(Guid commercantId);
        Task<bool> IsNumSerieUniqueForModeleAsync(string numSerie, ModeleTPE modele, Guid? excludeId = null);
    }
}