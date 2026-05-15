// projet0.Application/Interfaces/IIncidentPredictionService.cs
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;

namespace projet0.Application.Interfaces
{
    public interface IIncidentPredictionService
    {
        Task<ApiResponse<IncidentPredictionResponseDTO>> PredictNextWeekAndMonthAsync();
        Task<ApiResponse<List<DailyIncidentCountDTO>>> GetHistoricalDataAsync(int monthsBack = 4);
    }
}