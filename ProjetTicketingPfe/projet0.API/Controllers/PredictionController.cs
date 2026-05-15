using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.API.Controllers.Base;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;  // ← CHANGEMENT ICI

namespace projet0.API.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PredictionController : BaseApiController
    {
        private readonly IIncidentPredictionService _predictionService;

        public PredictionController(IIncidentPredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        [HttpGet("incidents")]
        [ProducesResponseType(typeof(ApiResponse<IncidentPredictionResponseDTO>), 200)]
        public async Task<IActionResult> PredictIncidents()
        {
            var result = await _predictionService.PredictNextWeekAndMonthAsync();
            return Ok(result);
        }

        [HttpGet("historical")]
        [ProducesResponseType(typeof(ApiResponse<List<DailyIncidentCountDTO>>), 200)]
        public async Task<IActionResult> GetHistoricalData([FromQuery] int monthsBack = 4)
        {
            var result = await _predictionService.GetHistoricalDataAsync(monthsBack);
            return Ok(result);
        }
    }
}

