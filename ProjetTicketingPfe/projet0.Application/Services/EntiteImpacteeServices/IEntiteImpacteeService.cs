using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Domain.Enums;

namespace projet0.Application.Services.EntiteImpacteeServices
{
    public interface IEntiteImpacteeService
    {
        
        Task<ApiResponse<List<EntiteImpacteeDTO>>> GetAllAsync();
        Task<ApiResponse<List<EntiteImpacteeDTO>>> GetByTypeAsync(TypeEntiteImpactee type);
        Task<ApiResponse<List<EntiteImpacteeDTO>>> GetByIncidentIdAsync(Guid incidentId);
        Task<ApiResponse<EntiteImpacteeDTO>> AddToIncidentAsync(AddEntiteImpacteeDTO dto);
        Task<ApiResponse<bool>> RemoveFromIncidentAsync(Guid entiteImpacteeId);
        
    }
}
