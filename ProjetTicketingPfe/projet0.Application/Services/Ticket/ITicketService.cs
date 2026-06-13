using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.DTOs.TicketDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
using TicketEntity = projet0.Domain.Entities.Ticket;

namespace projet0.Application.Services.Ticket
{
    public interface ITicketService
    {
        // Récupérer tous les tickets
        Task<ApiResponse<PagedResult<TicketDTO>>> GetTicketsPagedAsync(TicketPagedRequest request, Guid userId);

        // Récupérer un ticket par son ID
        Task<ApiResponse<TicketDTO>> GetTicketByIdAsync(Guid id);

        // Créer un nouveau ticket
        Task<ApiResponse<TicketDTO>> CreateTicketAsync(CreateTicketDTO dto, Guid createurId);


        Task<ApiResponse<TicketTechnicienDashboardDTO>> GetTechnicienDashboardAsync(Guid technicienId);

        Task<ApiResponse<bool>> DeleteTicketAsync(Guid id, Guid userId);

        // Récupérer un ticket avec ses commentaires
        Task<ApiResponse<TicketDetailDTO>> GetTicketDetailAsync(Guid id, Guid userId);

        Task<ApiResponse<UpdateTicketResponseDTO>> UpdateTicketAsync(Guid id, UpdateTicketDTO dto, Guid userId);

        Task<ApiResponse<LiaisonResultDTO>> LierIncidentsAuTicket(Guid ticketId, List<Guid> incidentIds, Guid userId);

        Task<ApiResponse<List<TicketDTO>>> GetTicketsByIncidentIdAsync(Guid incidentId);

        Task<TicketDTO> MapToDto(TicketEntity ticket);
        
        Task<ApiResponse<UpdateTicketResponseDTO>> TechnicianUpdateTicketAsync(Guid id, TechnicianUpdateTicketDTO dto, Guid technicienId);

        Task<ApiResponse<bool>> DelierIncidentDuTicket(Guid ticketId, Guid incidentId, Guid userId);

        Task<ApiResponse<PagedResult<TicketDTO>>> GetMesTicketsPagedAsync(TicketPagedRequest request, Guid technicienId);

        Task<ApiResponse<TicketDashboardDTO>> GetTicketDashboardAsync();

        // ARCHIVAGE
        Task<ApiResponse<TicketArchiveDTO>> ArchiverTicketAsync(Guid ticketId, Guid userId);
        Task<ApiResponse<TicketArchiveDTO>> RestaurerTicketAsync(Guid ticketId, Guid userId);
        Task<ApiResponse<PagedResult<TicketDTO>>> GetMyArchivesTicketsPagedAsync(TicketPagedRequest request, Guid userId);
    }
}