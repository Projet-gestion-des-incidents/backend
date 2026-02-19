// Fichier: projet0.API/Controllers/TicketController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Services.Ticket;
using System.Security.Claims;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/ticket")]
    //[Authorize]
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
        /// Récupère TOUS les tickets
        /// </summary>
        [HttpGet("all")]
        //[Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<List<TicketDTO>>>> GetAllTickets()
        {
            try
            {
                var result = await _ticketService.GetAllTicketsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les tickets");
                return StatusCode(500, ApiResponse<List<TicketDTO>>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        /// <summary>
        /// Crée un nouveau ticket
        /// </summary>
        [HttpPost]
        //[Authorize(Policy = "TicketCreate")]
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
                    nameof(GetById), // À implémenter plus tard
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
        public async Task<ActionResult<ApiResponse<TicketDTO>>> GetById(Guid id)
        {
            try
            {
                // Pour l'instant, retournez une réponse temporaire
                // Vous implémenterez la vraie méthode plus tard
                return Ok(ApiResponse<TicketDTO>.Success(null, "Méthode à implémenter"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du ticket {Id}", id);
                return StatusCode(500, ApiResponse<TicketDTO>.Failure(
                    "Erreur interne du serveur"));
            }
        }

        #endregion
    }
}
