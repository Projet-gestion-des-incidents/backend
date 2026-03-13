using Microsoft.EntityFrameworkCore;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace projet0.Application.Interfaces
{
    public interface ITicketRepository : IGenericRepository<Ticket>
    {
        // Méthodes spécifiques aux tickets
        Task<Ticket> GetByReferenceAsync(string reference);
        Task<Ticket> GetTicketWithDetailsAsync(Guid id);
        Task<List<Ticket>> GetTicketsByStatutAsync(StatutTicket statut);        
        Task<List<Ticket>> GetTicketsByCreateurAsync(Guid createurId);
        Task<List<Ticket>> GetTicketsByAssigneeAsync(Guid assigneeId);
        Task<bool> IsReferenceUniqueAsync(string reference, Guid? excludeId = null);
        Task<string> GenerateReferenceTicketAsync();
        Task<int> GetNextTicketNumberAsync(int year);
        IQueryable<Ticket> QueryWithDetails(Guid? createurId = null, Guid? assigneeId = null);

        // Obtenir une requête avec les includes par défaut
        IQueryable<Ticket> GetQueryWithIncludes();

        // Obtenir une requête filtrée
        IQueryable<Ticket> GetFilteredQuery(Expression<Func<Ticket, bool>>? filter = null);
        Task<List<Ticket>> GetTicketsByIncidentIdAsync(Guid incidentId);
        DbContext GetDbContext();

        void Detach(Ticket entity);
        void Attach(Ticket entity);
        void SetModified(Ticket entity);
        Task ReloadAsync(Ticket ticket);
        Task<bool> ExistsAsync(Guid id);
        Task<int> UpdateTicketStatutAsync(Guid ticketId, StatutTicket? nouveauStatut, Guid userId);
    }
}