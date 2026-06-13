using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Enums;
using IncidentEntity = projet0.Domain.Entities.Incident;

namespace projet0.Application.Services.Incident
{
    public interface IIncidentService
    {
        Task<ApiResponse<IncidentDTO>> GetIncidentByIdAsync(Guid id);
        Task<ApiResponse<IncidentDetailDTO>> GetIncidentDetailAsync(Guid id);
        Task<ApiResponse<List<IncidentDTO>>> GetAllIncidentsAsync(Guid? userId = null);
        Task<ApiResponse<PagedResult<IncidentDTO>>> SearchIncidentsAsync(IncidentSearchRequest request, Guid userId);
        Task<ApiResponse<IncidentDTO>> CreateIncidentAsync(CreateIncidentDTO dto, Guid createdById);
        Task<ApiResponse<IncidentDTO>> UpdateIncidentAsync(Guid id, UpdateIncidentDTO dto, Guid updatedById);
        Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id, Guid userId);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByStatutAsync(StatutIncident statut);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsBySeveriteAsync(SeveriteIncident severite);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByCreatedByAsync(Guid createdById);
        Task<ApiResponse<bool>> MettreAJourStatutIncident(Guid incidentId);
        Task<ApiResponse<bool>> FermerIncident(Guid incidentId);
        Task<IncidentDTO> MapToDto(IncidentEntity incident);
        Task<ApiResponse<bool>> ResoudreIncident(Guid incidentId, Guid userId);
        Task<ApiResponse<bool>> DelierTPEAsync(Guid incidentId, Guid tpeId, Guid userId);
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        Task<ApiResponse<List<IncidentTPEDTO>>> LierTPEsAsync(Guid incidentId, List<Guid> tpeIds, Guid userId);
        Task<ApiResponse<PagedResult<IncidentDTO>>> GetMyIncidentsPagedAsync(IncidentSearchRequest request, Guid userId);
        Task<ApiResponse<IncidentDashboardDTO>> GetIncidentDashboardAsync();
        Task<ApiResponse<CommercantIncidentDashboardDTO>> GetCommercantDashboardAsync(Guid commercantId);

        // ARCHIVAGE
        Task<ApiResponse<IncidentArchiveDTO>> ArchiverIncidentAsync(Guid incidentId, Guid userId);
        Task<ApiResponse<IncidentArchiveDTO>> RestaurerIncidentAsync(Guid incidentId, Guid userId);
        Task<ApiResponse<PagedResult<IncidentDTO>>> GetMyArchivesPagedAsync(IncidentSearchRequest request, Guid userId);
         Task<ApiResponse<PagedResult<IncidentDTO>>> GetIncidentsArchivesPagedAsync(IncidentSearchRequest request, Guid userId);
    }
}
