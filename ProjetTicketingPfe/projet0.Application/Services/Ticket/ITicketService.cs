using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using TicketEntity = projet0.Domain.Entities.Ticket;  // ← AJOUTER EN HAUT

namespace projet0.Application.Services.Ticket
{
    public interface ITicketService
    {
        // Récupérer tous les tickets
        Task<ApiResponse<PagedResult<TicketDTO>>> GetTicketsPagedAsync(TicketPagedRequest request);

        // Récupérer un ticket par son ID
        Task<ApiResponse<TicketDTO>> GetTicketByIdAsync(Guid id);

        // Créer un nouveau ticket
        Task<ApiResponse<TicketDTO>> CreateTicketAsync(CreateTicketDTO dto, Guid createurId);
        
       Task<ApiResponse<bool>> DeleteTicketAsync(Guid id);

        // NOUVEAU: Récupérer un ticket avec ses commentaires
        Task<ApiResponse<TicketDetailDTO>> GetTicketDetailAsync(Guid id);

        Task<ApiResponse<UpdateTicketResponseDTO>> UpdateTicketAsync(Guid id, UpdateTicketDTO dto, Guid userId);
        Task<ApiResponse<bool>> LierIncidentsAuTicket(Guid ticketId, List<Guid> incidentIds, Guid userId);
        Task<ApiResponse<TicketDTO>> UpdateTicketStatutAsync(Guid ticketId, StatutTicket nouveauStatut, Guid userId);
        Task<ApiResponse<List<TicketDTO>>> GetTicketsByIncidentIdAsync(Guid incidentId);

        // Pour le mapping (utilisé dans les contrôleurs)
        Task<TicketDTO> MapToDto(TicketEntity ticket);
    }
}