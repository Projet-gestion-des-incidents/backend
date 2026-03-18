using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using IncidentEntity = projet0.Domain.Entities.Incident;

namespace projet0.Application.Services.Incident
{
    public interface IIncidentService
    {
        // CRUD de base
        Task<ApiResponse<IncidentDTO>> GetIncidentByIdAsync(Guid id);
        Task<ApiResponse<IncidentDetailDTO>> GetIncidentDetailAsync(Guid id);
        Task<ApiResponse<List<IncidentDTO>>> GetAllIncidentsAsync();
        Task<ApiResponse<PagedResult<IncidentDTO>>> SearchIncidentsAsync(IncidentSearchRequest request);
        Task<ApiResponse<IncidentDTO>> CreateIncidentAsync(CreateIncidentDTO dto, Guid createdById);
        Task<ApiResponse<IncidentDTO>> UpdateIncidentAsync(Guid id, UpdateIncidentDTO dto, Guid updatedById);
        Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id, Guid userId);

        // Méthodes spécifiques
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByStatutAsync(StatutIncident statut);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsBySeveriteAsync(SeveriteIncident severite);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByCreatedByAsync(Guid createdById);
        Task<ApiResponse<bool>> MettreAJourStatutIncident(Guid incidentId);
        Task<ApiResponse<bool>> FermerIncident(Guid incidentId);

        // Pour le mapping (utilisé dans les contrôleurs)
        Task<IncidentDTO> MapToDto(IncidentEntity incident);
        // Dans IIncidentService.cs
        Task<ApiResponse<bool>> ResoudreIncident(Guid incidentId, Guid userId);
        Task<ApiResponse<bool>> DelierTPEAsync(Guid incidentId, Guid tpeId, Guid userId);
        // Dans IIncidentService.cs
        Task<IList<string>> GetUserRolesAsync(Guid userId);
        // Dans IIncidentService.cs
        Task<ApiResponse<List<IncidentTPEDTO>>> LierTPEsAsync(Guid incidentId, List<Guid> tpeIds, Guid userId);
    }
}