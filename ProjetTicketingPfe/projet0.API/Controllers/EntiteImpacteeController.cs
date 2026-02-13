using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.EntiteImpacteeServices;
using projet0.Domain.Entities;
using projet0.Domain.Enums;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/entites-impactees")]
    [Authorize]
    public class EntiteImpacteeController : ControllerBase
    {
        private readonly IEntiteImpacteeService _service;
        private readonly ILogger<EntiteImpacteeController> _logger;

        public EntiteImpacteeController(IEntiteImpacteeService service, ILogger<EntiteImpacteeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<EntiteImpacteeDTO>>> Create([FromBody] CreateEntiteImpacteeDTO dto)
        {
            var result = await _service.CreateAsync(dto);
            if (!result.IsSuccess)
                return StatusCode(500, result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<EntiteImpacteeDTO>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            if (!result.IsSuccess)
                return StatusCode(500, result);

            return Ok(result);
        }

        [HttpGet("by-type/{type}")]
        public async Task<ActionResult<ApiResponse<List<EntiteImpacteeDTO>>>> GetByType(TypeEntiteImpactee type)
        {
            var result = await _service.GetByTypeAsync(type);
            if (!result.IsSuccess)
                return StatusCode(500, result);

            return Ok(result);
        }

        [HttpGet("by-incident/{incidentId}")]
        public async Task<ActionResult<ApiResponse<List<EntiteImpacteeDTO>>>> GetByIncidentId(Guid incidentId)
        {
            var result = await _service.GetByIncidentIdAsync(incidentId);
            if (!result.IsSuccess)
                return StatusCode(500, result);

            return Ok(result);
        }
    }
}
