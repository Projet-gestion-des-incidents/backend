using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

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
        Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id);

        // Méthodes spécifiques
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByStatutAsync(StatutIncident statut);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsBySeveriteAsync(SeveriteIncident severite);
        Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByCreatedByAsync(Guid createdById);
     
    }
}