using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}