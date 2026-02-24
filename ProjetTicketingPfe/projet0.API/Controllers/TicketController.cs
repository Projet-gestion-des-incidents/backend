using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
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

        public TicketController(
            ITicketService ticketService,
            ILogger<TicketController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
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
        public async Task<ActionResult<ApiResponse<TicketDTO>>> Create([FromBody] CreateTicketDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var result = await _ticketService.CreateTicketAsync(dto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

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
                var result = await _ticketService.GetTicketDetailAsync(id);

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
                var result = await _ticketService.DeleteTicketAsync(id);

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
        #endregion
    }
}
