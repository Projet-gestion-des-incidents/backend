using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.Incident;
using projet0.Domain.Enums;
using System.Security.Claims;

namespace projet0.API.Controllers
{

    [ApiController]
    [Route("api/incident")]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;
        private readonly ILogger<IncidentController> _logger;

        public IncidentController(
            IIncidentService incidentService,
            ILogger<IncidentController> logger)
        {
            _incidentService = incidentService;
            _logger = logger;
        }

[HttpGet("all")]
[Authorize(Policy = "IncidentRead")]
public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetAllIncidents()
{
    try
    {
        var result = await _incidentService.GetAllIncidentsAsync();
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur lors de la récupération de tous les incidents");
        return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure(
            "Erreur interne du serveur"));
    }
}
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }

        #region CRUD Operations

        [HttpGet("withFilters")]
        [Authorize(Policy = "IncidentRead")]     
        public async Task<ActionResult<ApiResponse<PagedResult<IncidentDTO>>>> SearchIncidents(
            [FromQuery] IncidentSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _incidentService.SearchIncidentsAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche d'incidents");
                return StatusCode(500, ApiResponse<PagedResult<IncidentDTO>>.Failure(
                    "Erreur interne du serveur lors de la recherche d'incidents"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "IncidentRead")]   
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> GetById(Guid id)
        {
            try
            {
                var result = await _incidentService.GetIncidentByIdAsync(id);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentDTO>.Failure(
                    "Erreur interne du serveur lors de la récupération de l'incident"));
            }
        }

        [HttpGet("{id}/details")]
        [Authorize(Policy = "IncidentRead")]  
        public async Task<ActionResult<ApiResponse<IncidentDetailDTO>>> GetDetails(Guid id)
        {
            try
            {
                var result = await _incidentService.GetIncidentDetailAsync(id);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentDetailDTO>.Failure(
                    "Erreur interne du serveur lors de la récupération des détails de l'incident"));
            }
        }

        // projet0.API/Controllers/IncidentController.cs

        [HttpPost]
        [Authorize(Policy = "IncidentCreate")]
        [Consumes("multipart/form-data")]  // ✅ Important !
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> Create(
            [FromForm] CreateIncidentDTO dto)  // ✅ [FromForm] au lieu de [FromBody]
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();

                var result = await _incidentService.CreateIncidentAsync(dto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data?.Id },
                    result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de création d'incident sans authentification");
                return Unauthorized(ApiResponse<IncidentDTO>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'incident");
                return StatusCode(500, ApiResponse<IncidentDTO>.Failure(
                    "Erreur interne du serveur lors de la création de l'incident"));
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> Update(Guid id, [FromBody] UpdateIncidentDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var result = await _incidentService.UpdateIncidentAsync(id, dto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message?.Contains("introuvable") == true)
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de mise à jour d'incident sans authentification");
                return Unauthorized(ApiResponse<IncidentDTO>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentDTO>.Failure(
                    "Erreur interne du serveur lors de la mise à jour de l'incident"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "IncidentDelete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            try
            {
                var result = await _incidentService.DeleteIncidentAsync(id);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure(
                    "Erreur interne du serveur lors de la suppression de l'incident"));
            }
        }

        #endregion

        #region Specific Operations

        [HttpGet("statut/{statut}")]
        [Authorize(Policy = "IncidentRead")]   
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetByStatut(StatutIncident statut)
        {
            try
            {
                var result = await _incidentService.GetIncidentsByStatutAsync(statut);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents par statut {Statut}", statut);
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure(
                    "Erreur interne du serveur lors de la récupération des incidents par statut"));
            }
        }

        [HttpGet("severite/{severite}")]
        [Authorize(Policy = "IncidentRead")]     
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetBySeverite(SeveriteIncident severite)
        {
            try
            {
                var result = await _incidentService.GetIncidentsBySeveriteAsync(severite);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents par sévérité {Severite}", severite);
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure(
                    "Erreur interne du serveur lors de la récupération des incidents par sévérité"));
            }
        }

        [HttpGet("created-by/{userId}")]
        [Authorize(Policy = "IncidentRead")]  
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetByCreatedBy(Guid userId)
        {
            try
            {
                var result = await _incidentService.GetIncidentsByCreatedByAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents par créateur {UserId}", userId);
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure(
                    "Erreur interne du serveur lors de la récupération des incidents par créateur"));
            }
        }

        [HttpGet("my-incidents")]
        [Authorize(Policy = "IncidentRead")]  
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetMyIncidents()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.GetIncidentsByCreatedByAsync(userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de récupération de ses incidents sans authentification");
                return Unauthorized(ApiResponse<List<IncidentDTO>>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de mes incidents");
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure(
                    "Erreur interne du serveur lors de la récupération de vos incidents"));
            }
        }
    
        #endregion
    }
}
