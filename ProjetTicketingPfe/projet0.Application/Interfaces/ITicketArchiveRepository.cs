using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface ITicketArchiveRepository : IGenericRepository<TicketArchive>
    {
        Task<bool> ExistsAsync(Guid ticketId, Guid userId);
        Task<List<Guid>> GetArchivedTicketIdsByUserAsync(Guid userId);
        Task<TicketArchive?> GetByTicketAndUserAsync(Guid ticketId, Guid userId);
        Task<List<TicketArchive>> GetArchivesByTicketCreatorAsync(Guid createurId); // ✅ À AJOUTER


    }
}
