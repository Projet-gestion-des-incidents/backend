using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.DTOs.TicketDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Incident;
using projet0.Application.Services.Ticket;
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
        private readonly ITicketService _ticketService;
        private readonly IIncidentTicketRepository _incidentTicketRepository;
        public IncidentController(
            IIncidentService incidentService,
            ILogger<IncidentController> logger, ITicketService ticketService, 
            IIncidentTicketRepository incidentTicketRepository)
        {
            _incidentService = incidentService;
            _logger = logger;
            _ticketService = ticketService;  
            _incidentTicketRepository = incidentTicketRepository;  
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

                // Ajouter les URLs des pièces jointes
                if (result.Data.PiecesJointes != null)
                {
                    foreach (var piece in result.Data.PiecesJointes)
                    {
                        piece.Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{piece.Id}";
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentDetailDTO>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        [HttpPost]
        [Authorize(Policy = "IncidentCreate")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<IncidentDTO>>> Create(
            [FromForm] CreateIncidentDTO dto)
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
                var userId = GetCurrentUserId();
                var userRoles = await _incidentService.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                var incident = await _incidentService.GetIncidentByIdAsync(id);
                if (!incident.IsSuccess || incident.Data == null)
                    return NotFound(ApiResponse<bool>.Failure("Incident non trouvé"));

                // RÈGLES DE SUPPRESSION
                if (isAdmin)
                {
                    // Admin peut supprimer n'importe quel incident
                }
                else if (isCommercant)
                {
                    // Vérifier que c'est son incident
                    if (incident.Data.CreatedById != userId)
                    {
                        return BadRequest(ApiResponse<bool>.Failure(
                            "Vous ne pouvez supprimer que vos propres incidents.",
                            resultCode: 95
                        ));
                    }

                    // SOLUTION RAPIDE : Vérifier si le statut est différent de 0 (Non traité)
                    // Dans votre enum, "Non traité" correspond à 0
                    if ((int)incident.Data.StatutIncident != 0)
                    {
                        return BadRequest(ApiResponse<bool>.Failure(
                            "Vous ne pouvez supprimer qu'un incident sans statut.",
                            resultCode: 96
                        ));
                    }
                }
                else
                {
                    return Forbid();
                }

                var result = await _incidentService.DeleteIncidentAsync(id, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
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
        public async Task<ActionResult<ApiResponse<PagedResult<IncidentDTO>>>> GetMyIncidents(
            [FromQuery] IncidentSearchRequest request) 
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation("Récupération paginée des incidents du commerçant {UserId} - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, Statut: {Statut}",
                    userId, request.Page, request.PageSize, request.SearchTerm, request.StatutIncident);

                // Appeler une nouvelle méthode du service avec le filtre par utilisateur
                var result = await _incidentService.GetMyIncidentsPagedAsync(request, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de récupération de ses incidents sans authentification");
                return Unauthorized(ApiResponse<PagedResult<IncidentDTO>>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée de mes incidents");
                return StatusCode(500, ApiResponse<PagedResult<IncidentDTO>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        [HttpPost("{incidentId}/lier-tickets")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<LiaisonTicketsResultDTO>>> LierTicketsAIncident(
    Guid incidentId,
    [FromBody] List<Guid> ticketIds)
        {
            try
            {
                var userId = GetCurrentUserId();
                var ticketsLies = 0;
                var ticketsDejaLies = 0;
                var ticketsNonTrouves = 0;
                var details = new List<string>();

                foreach (var ticketId in ticketIds)
                {
                    // Vérifier si le ticket existe
                    var ticket = await _ticketService.GetTicketByIdAsync(ticketId);
                    if (ticket?.Data == null)
                    {
                        ticketsNonTrouves++;
                        details.Add($"Ticket {ticketId} non trouvé");
                        continue;
                    }

                    // Vérifier si la liaison existe déjà
                    var existe = await _incidentTicketRepository.ExistsAsync(ticketId, incidentId);

                    if (existe)
                    {
                        ticketsDejaLies++;
                        details.Add($"Ticket {ticketId} déjà lié à l'incident");
                    }
                    else
                    {
                        // Créer la liaison
                        var result = await _ticketService.LierIncidentsAuTicket(
                            ticketId,
                            new List<Guid> { incidentId },
                            userId
                        );

                        if (result.IsSuccess)
                        {
                            ticketsLies++;
                            details.Add($"Ticket {ticketId} lié avec succès");
                        }
                    }
                }

                var resultDto = new LiaisonTicketsResultDTO
                {
                    TicketsLies = ticketsLies,
                    TicketsDejaLies = ticketsDejaLies,
                    TicketsNonTrouves = ticketsNonTrouves,
                    TotalTicketsTraites = ticketIds.Count,
                    Details = details
                };

                string message;
                if (ticketsLies > 0 && ticketsDejaLies > 0)
                    message = $"{ticketsLies} ticket(s) lié(s), {ticketsDejaLies} déjà existant(s)";
                else if (ticketsLies > 0)
                    message = $"{ticketsLies} ticket(s) lié(s) avec succès";
                else if (ticketsDejaLies > 0)
                    message = $"Tous les tickets ({ticketsDejaLies}) étaient déjà liés à cet incident";
                else
                    message = "Aucun ticket n'a pu être lié";

                return Ok(ApiResponse<LiaisonTicketsResultDTO>.Success(
                    resultDto,
                    message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du lien tickets-incident");
                return StatusCode(500, ApiResponse<LiaisonTicketsResultDTO>.Failure("Erreur interne"));
            }
        }

        [HttpGet("{incidentId}/tickets")]
        [Authorize(Policy = "IncidentRead")]
        public async Task<ActionResult<ApiResponse<List<TicketDTO>>>> GetTicketsByIncident(Guid incidentId)
        {
            try
            {
                var tickets = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);
                var dtos = new List<TicketDTO>();
                foreach (var ticket in tickets)
                {
                    dtos.Add(await _ticketService.MapToDto(ticket));
                }
                return Ok(ApiResponse<List<TicketDTO>>.Success(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tickets de l'incident");
                return StatusCode(500, ApiResponse<List<TicketDTO>>.Failure("Erreur interne"));
            }
        }

        [HttpPut("{incidentId}/resoudre")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<bool>>> ResoudreIncident(Guid incidentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.ResoudreIncident(incidentId, userId);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la résolution de l'incident {IncidentId}", incidentId);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        [HttpDelete("{incidentId}/tpes/{tpeId}")]
        [Authorize(Policy = "IncidentUpdate")]  // À adapter selon votre politique
        public async Task<ActionResult<ApiResponse<bool>>> DelierTPE(
            Guid incidentId,
            Guid tpeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.DelierTPEAsync(incidentId, tpeId, userId);

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la liaison TPE");
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Lie plusieurs TPEs à un incident existant
        /// </summary>
        /// 
        [HttpPost("{incidentId}/tpes")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<List<IncidentTPEDTO>>>> LierPlusieursTPEs(
            Guid incidentId,
            [FromBody] List<Guid> tpeIds)
        {
            try
            {
                _logger.LogInformation("Liaison de {Count} TPE(s) à l'incident {IncidentId}", tpeIds.Count, incidentId);

                var userId = GetCurrentUserId();
                var result = await _incidentService.LierTPEsAsync(incidentId, tpeIds, userId);

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la liaison multiple de TPEs");
                return StatusCode(500, ApiResponse<List<IncidentTPEDTO>>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupère tous les incidents qui n'ont aucun ticket lié
        /// </summary>
        [HttpGet("disponibles")]
        [Authorize(Policy = "IncidentRead")]
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetIncidentsSansTicket()
        {
            try
            {
                _logger.LogInformation("Récupération des incidents sans aucun ticket lié");

                var incidents = await _incidentTicketRepository.GetIncidentsSansTicketAsync();

                var dtos = new List<IncidentDTO>();
                foreach (var incident in incidents)
                {
                    dtos.Add(await _incidentService.MapToDto(incident));
                }

                _logger.LogInformation("{Count} incident(s) sans ticket trouvé(s)", dtos.Count);

                return Ok(ApiResponse<List<IncidentDTO>>.Success(
                    dtos,
                    $"{dtos.Count} incident(s) sans ticket associé"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents sans ticket");
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupère les statistiques du dashboard incidents
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IncidentDashboardDTO>>> GetIncidentDashboard()
        {
            try
            {
                _logger.LogInformation("Récupération du dashboard incidents");

                var result = await _incidentService.GetIncidentDashboardAsync();

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard incidents");
                return StatusCode(500, ApiResponse<IncidentDashboardDTO>.Failure("Erreur interne du serveur"));
            }
        }


        // API/Controllers/IncidentController.cs

        /// <summary>
        /// Archive un incident résolu
        /// </summary>
        [HttpPost("{id}/archiver")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<IncidentArchiveDTO>>> ArchiverIncident(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.ArchiverIncidentAsync(id, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<IncidentArchiveDTO>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'archivage de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentArchiveDTO>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Restaure un incident archivé
        /// </summary>
        [HttpPost("{id}/restaurer")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<IncidentArchiveDTO>>> RestaurerIncident(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _incidentService.RestaurerIncidentAsync(id, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la restauration de l'incident {IncidentId}", id);
                return StatusCode(500, ApiResponse<IncidentArchiveDTO>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupère les incidents archivés (paginated)
        /// </summary>
        [HttpGet("archives")]
        [Authorize(Policy = "IncidentRead")]
        public async Task<ActionResult<ApiResponse<PagedResult<IncidentDTO>>>> GetIncidentsArchives(
            [FromQuery] IncidentSearchRequest request)
        {
            try
            {
                var result = await _incidentService.GetIncidentsArchivesPagedAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents archivés");
                return StatusCode(500, ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne"));
            }
        }
        //////////commmm
        #endregion

    }
}
