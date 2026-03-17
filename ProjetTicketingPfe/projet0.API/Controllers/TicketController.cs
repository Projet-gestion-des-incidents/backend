using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.DTOs.TicketDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
using projet0.Application.Interfaces;
using projet0.Application.Services.Incident;
using projet0.Application.Services.Ticket;
using projet0.Domain.Enums;
using System.Security.Claims;

namespace projet0.API.Controllers
{

    [ApiController]
    [Route("api/ticket")]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;
        private readonly IIncidentTicketRepository _incidentTicketRepository;
        private readonly IIncidentService _incidentService;

        public TicketController(
            ITicketService ticketService,
            ILogger<TicketController> logger, 
            IIncidentTicketRepository incidentTicketRepository, 
            IIncidentService incidentService)
        {
            _ticketService = ticketService;
            _logger = logger;
            _incidentTicketRepository = incidentTicketRepository; 
            _incidentService = incidentService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }

        #region CRUD Operations

        [HttpGet]  
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<PagedResult<TicketDTO>>>> GetTicketsPaged(
       [FromQuery] TicketPagedRequest request) 
        {
            try
            {
                _logger.LogInformation("Récupération paginée des tickets - Page: {Page}, PageSize: {PageSize}",
                    request.Page, request.PageSize);

                var result = await _ticketService.GetTicketsPagedAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des tickets");
                return StatusCode(500, ApiResponse<PagedResult<TicketDTO>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        [HttpPost]
        [Authorize(Policy = "TicketCreate")]
        public async Task<ActionResult<ApiResponse<TicketDTO>>> Create([FromForm] CreateTicketDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var result = await _ticketService.CreateTicketAsync(dto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Le message de succès indique comment ajouter des commentaires
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data?.Id },
                    result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentative de création de ticket sans authentification");
                return Unauthorized(ApiResponse<TicketDTO>.Failure(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du ticket");
                return StatusCode(500, ApiResponse<TicketDTO>.Failure(
                    "Erreur interne du serveur lors de la création du ticket"));
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<TicketDTO>>> GetById(Guid id)
        {
            try
            {
                var result = await _ticketService.GetTicketByIdAsync(id);

                if (!result.IsSuccess || result.Data == null)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du ticket {Id}", id);
                return StatusCode(500, ApiResponse<TicketDTO>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        [HttpGet("{id}/details")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<TicketDetailDTO>>> GetDetails(Guid id)
        {
            try
            {
                var result = await _ticketService.GetTicketDetailAsync(id, id);

                if (!result.IsSuccess || result.Data == null)
                    return NotFound(result);

                // Ajouter les URLs des pièces jointes
                if (result.Data.Commentaires != null)
                {
                    foreach (var commentaire in result.Data.Commentaires)
                    {
                        foreach (var piece in commentaire.PiecesJointes)
                        {
                            piece.Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{piece.Id}";
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails du ticket {Id}", id);
                return StatusCode(500, ApiResponse<TicketDetailDTO>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "TicketDelete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();  // ← Récupérer l'utilisateur connecté
                var result = await _ticketService.DeleteTicketAsync(id, userId);  // ← Passer userId

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
                _logger.LogError(ex, "Erreur lors de la suppression du ticket {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Failure(
                    "Erreur interne du serveur lors de la suppression du ticket"));
            }
        }

        /// <summary>
        /// Mettre à jour un ticket (titre, description, statut, priorité, assignation et commentaires)
        /// </summary>
        /// <param name="id">ID du ticket à modifier</param>
        /// <param name="dto">Données de mise à jour</param>
        /// <returns>Ticket mis à jour avec détails des modifications</returns>

        [HttpPut("{id}")]
        [Authorize(Policy = "TicketUpdate")]
        public async Task<ActionResult<ApiResponse<UpdateTicketResponseDTO>>> UpdateTicket(
            Guid id,
            [FromForm] UpdateTicketDTO dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour du ticket {Id}", id);

                var userId = GetCurrentUserId();

                // Vérifier que le ticket existe
                var ticketExistant = await _ticketService.GetTicketByIdAsync(id);
                if (ticketExistant == null || !ticketExistant.IsSuccess)
                    return NotFound(ApiResponse<UpdateTicketResponseDTO>.Failure("Ticket non trouvé"));

                var result = await _ticketService.UpdateTicketAsync(id, dto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Ajouter les URLs des pièces jointes dans les commentaires
                if (result.Data.Commentaires != null)
                {
                    foreach (var commentaire in result.Data.Commentaires)
                    {
                        foreach (var piece in commentaire.PiecesJointes)
                        {
                            piece.Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{piece.Id}";
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du ticket {Id}", id);
                return StatusCode(500, ApiResponse<UpdateTicketResponseDTO>.Failure(
                    "Erreur interne du serveur lors de la mise à jour du ticket"));
            }
        }

        [HttpPost("{ticketId}/lier-incidents")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<bool>>> LierIncidents(
            Guid ticketId,
            [FromBody] List<Guid> incidentIds)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _ticketService.LierIncidentsAuTicket(ticketId, incidentIds, userId);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du lien incidents-ticket");
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        [HttpGet("{ticketId}/incidents")]
        public async Task<ActionResult<ApiResponse<List<IncidentDTO>>>> GetIncidentsByTicket(Guid ticketId)
        {
            try
            {
                var incidents = await _incidentTicketRepository.GetIncidentsByTicketIdAsync(ticketId);
                var dtos = new List<IncidentDTO>();
                foreach (var incident in incidents)
                {
                    dtos.Add(await _incidentService.MapToDto(incident));  // ← ICI
                }
                return Ok(ApiResponse<List<IncidentDTO>>.Success(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des incidents du ticket");
                return StatusCode(500, ApiResponse<List<IncidentDTO>>.Failure("Erreur interne"));
            }
        }

        [HttpPut("{ticketId}/statut")]
        public async Task<ActionResult<ApiResponse<TicketDTO>>> UpdateStatut(
    Guid ticketId,
    [FromBody] UpdateTicketStatutDTO dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Créer un DTO de mise à jour avec seulement le statut
                var updateDto = new UpdateTicketDTO
                {
                    StatutTicket = dto.StatutTicket
                };

                // Utiliser UpdateTicketAsync qui existe déjà
                var result = await _ticketService.UpdateTicketAsync(ticketId, updateDto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Retourner un TicketDTO (pas UpdateTicketResponseDTO)
                var ticketDto = new TicketDTO
                {
                    Id = result.Data.Id,
                    ReferenceTicket = result.Data.ReferenceTicket,
                    TitreTicket = result.Data.TitreTicket,
                    DescriptionTicket = result.Data.DescriptionTicket,
                    StatutTicket = result.Data.StatutTicket,
                    StatutTicketLibelle = result.Data.StatutTicketLibelle,
                    DateCreation = result.Data.DateCreation,
                    DateLimite = result.Data.DateLimite,
                    DateCloture = result.Data.DateCloture,
                    CreateurId = result.Data.CreateurId,
                    CreateurNom = result.Data.CreateurNom,
                    AssigneeId = result.Data.AssigneeId,
                    AssigneeNom = result.Data.AssigneeNom,
                    NombreCommentaires = result.Data.NombreCommentaires,
                    NombrePiecesJointes = result.Data.NombrePiecesJointes
                };

                return Ok(ApiResponse<TicketDTO>.Success(ticketDto, result.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du ticket");
                return StatusCode(500, ApiResponse<TicketDTO>.Failure("Erreur interne"));
            }
        }

        // Dans TicketController.cs - AJOUTER CETTE MÉTHODE

        [HttpGet("mes-tickets")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<List<TicketDTO>>>> GetMesTicketsAssignes()
        {
            try
            {
                var userId = GetCurrentUserId();

                // Créer une requête filtrée par l'ID du technicien connecté
                var request = new TicketPagedRequest
                {
                    Page = 1,
                    PageSize = 100,
                    SortBy = "DateCreation",
                    SortDescending = true
                };

                var result = await _ticketService.GetTicketsPagedAsync(request);

                // Filtrer manuellement pour ne garder que les tickets assignés à l'utilisateur
                if (result.IsSuccess && result.Data != null)
                {
                    var ticketsAssignes = result.Data.Items
                        .Where(t => t.AssigneeId == userId)
                        .ToList();

                    var pagedResult = new PagedResult<TicketDTO>
                    {
                        Items = ticketsAssignes,
                        TotalCount = ticketsAssignes.Count,
                        Page = 1,
                        PageSize = ticketsAssignes.Count
                    };

                    return Ok(ApiResponse<PagedResult<TicketDTO>>.Success(pagedResult));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tickets assignés");
                return StatusCode(500, ApiResponse<PagedResult<TicketDTO>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        // Dans les méthodes AdminUpdateTicket et TechnicianUpdateTicket
        //[HttpPut("{id}/admin-update")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AdminUpdateTicket(Guid id, [FromBody] AdminUpdateTicketDTO dto)
        //{
        //    var userId = GetCurrentUserId();
        //    var result = await _ticketService.AdminUpdateTicketAsync(id, dto, userId);

        //    if (!result.IsSuccess)  // ← CHANGER Success en IsSuccess
        //        return BadRequest(result);

        //    return Ok(result);
        //}

        [HttpPut("{id}/technician-update")]
        [Authorize(Roles = "Technicien")]
        public async Task<IActionResult> TechnicianUpdateTicket(Guid id, [FromBody] TechnicianUpdateTicketDTO dto)
        {
            var userId = GetCurrentUserId();
            var result = await _ticketService.TechnicianUpdateTicketAsync(id, dto, userId);

            if (!result.IsSuccess)  // ← CHANGER Success en IsSuccess
                return BadRequest(result);

            return Ok(result);
        }
        // Dans TicketController.cs

        [HttpDelete("{ticketId}/incidents/{incidentId}")]
        [Authorize(Policy = "AdminOnly")] // Ou la politique que vous voulez
        public async Task<ActionResult<ApiResponse<bool>>> DelierIncident(
            Guid ticketId,
            Guid incidentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _ticketService.DelierIncidentDuTicket(ticketId, incidentId, userId);

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la liaison");
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }
        #endregion
    }
}
