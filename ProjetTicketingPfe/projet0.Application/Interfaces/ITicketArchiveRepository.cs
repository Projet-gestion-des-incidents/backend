using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface ITicketArchiveRepository : IGenericRepository<TicketArchive>
    {
        Task<bool> ExistsAsync(Guid ticketId, Guid userId);
        Task<List<Guid>> GetArchivedTicketIdsByUserAsync(Guid userId);
        Task<TicketArchive?> GetByTicketAndUserAsync(Guid ticketId, Guid userId);
        Task<List<TicketArchive>> GetArchivesByTicketCreatorAsync(Guid createurId); 

        Task<List<Guid>> GetAllArchivedTicketIdsAsync();

    }
}
