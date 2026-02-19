using System;
using System.Collections.Generic;
using System.Text;
// Fichier: projet0.Application/Interfaces/ITicketRepository.cs
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.Application.Interfaces
{
    public interface ITicketRepository : IGenericRepository<Ticket>
    {
        // Méthodes spécifiques aux tickets
        Task<Ticket> GetByReferenceAsync(string reference);
        Task<Ticket> GetTicketWithDetailsAsync(Guid id);
        Task<List<Ticket>> GetTicketsByStatutAsync(StatutTicket statut);
        Task<List<Ticket>> GetTicketsByPrioriteAsync(PrioriteTicket priorite);
        Task<List<Ticket>> GetTicketsByCreateurAsync(Guid createurId);
        Task<List<Ticket>> GetTicketsByAssigneeAsync(Guid assigneeId);
        Task<bool> IsReferenceUniqueAsync(string reference, Guid? excludeId = null);
        Task<string> GenerateReferenceTicketAsync();
        Task<int> GetNextTicketNumberAsync(int year);
        IQueryable<Ticket> QueryWithDetails(Guid? createurId = null, Guid? assigneeId = null);
    }
}