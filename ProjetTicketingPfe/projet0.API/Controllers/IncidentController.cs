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
    //[Authorize]
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
        /// <summary>
/// Récupère TOUS les incidents sans filtre, sans pagination
/// </summary>
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
        /// <summary>
        /// Récupère l'ID de l'utilisateur connecté
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }

        #region CRUD Operations


        /// <summary>
        /// Recherche paginée des incidents avec filtres
        /// </summary>
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

        /// <summary>
        /// Récupère un incident par son ID
        /// </summary>
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

        /// <summary>
        /// Récupère un incident détaillé avec ses relations (tickets, entités impactées)
        /// </summary>
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

        /// <summary>
        /// Récupère un incident par son code
        /// </summary>
        [HttpGet("code/{code}")]
        [Authorize(Policy = "IncidentRead")]

        public async Task<ActionResult<ApiResponse<IncidentDTO>>> GetByCode(string code)
        {
            try
            {
                var result = await _incidentService.GetIncidentByCodeAsync(code);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'incident {IncidentCode}", code);
                return StatusCode(500, ApiResponse<IncidentDTO>.Failure(
                    "Erreur interne du serveur lors de la récupération de l'incident par code"));
            }
        }

        /// <summary>
        /// Crée un nouvel incident
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "IncidentCreate")]
   
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> Create([FromBody] CreateIncidentDTO dto)
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

        /// <summary>
        /// Met à jour un incident existant
        /// </summary>
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
                    if (result.Message?.Contains("non trouvé") == true)
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

        /// <summary>
        /// Supprime un incident
        /// </summary>
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

        /// <summary>
        /// Récupère les incidents par statut
        /// </summary>
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

        /// <summary>
        /// Récupère les incidents par sévérité
        /// </summary>
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

        /// <summary>
        /// Récupère les incidents créés par un utilisateur spécifique
        /// </summary>
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

        /// <summary>
        /// Récupère les incidents créés par l'utilisateur connecté
        /// </summary>
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

        /// <summary>
        /// Met à jour uniquement le statut d'un incident
        /// </summary>
        [HttpPatch("{id}/statut")]
        [Authorize(Policy = "IncidentUpdate")]
     
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> UpdateStatut(
            Guid id,
            [FromBody] UpdateIncidentStatutDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var result = await _incidentService.UpdateIncidentStatutAsync(id, dto, userId);

                if (!result.IsSuccess)
                {
                    if (result.Message?.Contains("non trouvé") == true)
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de mise à jour du statut sans authentification");
                return Unauthorized(ApiResponse<IncidentDTO>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de statut de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentDTO>.Failure(
                    "Erreur interne du serveur lors du changement de statut"));
            }
        }

        /// <summary>
        /// Marque un incident comme résolu
        /// </summary>
        [HttpPatch("{id}/resoudre")]
        [Authorize(Policy = "IncidentUpdate")]

        public async Task<ActionResult<ApiResponse<bool>>> Resoudre(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.ResoudreIncidentAsync(id, userId);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de résolution d'incident sans authentification");
                return Unauthorized(ApiResponse<bool>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la résolution de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure(
                    "Erreur interne du serveur lors de la résolution de l'incident"));
            }
        }

        /// <summary>
        /// Assigne des entités impactées à un incident
        /// </summary>
        [HttpPost("{id}/entites-impactees")]
        [Authorize(Policy = "IncidentUpdate")]

        public async Task<ActionResult<ApiResponse<bool>>> AssignerEntitesImpactees(
            Guid id,
            [FromBody] List<Guid> entiteIds)
        {
            try
            {
                if (entiteIds == null || entiteIds.Count == 0)
                {
                    return BadRequest(ApiResponse<bool>.Failure(
                        "La liste des entités impactées ne peut pas être vide"));
                }

                var result = await _incidentService.AssignerEntitesImpacteesAsync(id, entiteIds);

                if (!result.IsSuccess)
                {
                    if (result.Message?.Contains("non trouvé") == true)
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'assignation des entités impactées à l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure(
                    "Erreur interne du serveur lors de l'assignation des entités impactées"));
            }
        }

        #endregion
    }
}
