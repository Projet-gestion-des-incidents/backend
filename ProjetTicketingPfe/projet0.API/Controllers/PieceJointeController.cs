using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Incident;
using projet0.Application.Services.User;
using System.Security.Claims;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/pieces-jointes")]
    [Authorize]
    public class PieceJointeController : ControllerBase
    {
        private readonly IPieceJointeService _pieceJointeService;
        private readonly ILogger<PieceJointeController> _logger;
        private readonly IUserService _userService;
        private readonly IIncidentService _incidentService;

        public PieceJointeController(
            IPieceJointeService pieceJointeService,
            ILogger<PieceJointeController> logger,
            IUserService userService,
            IIncidentService incidentService)
        {
            _pieceJointeService = pieceJointeService;
            _logger = logger;
            _userService = userService;
            _incidentService = incidentService;
        }

        [HttpGet("{id}")]
        [AllowAnonymous] // Permet l'accès direct aux fichiers sans auth (optionnel)
        public async Task<IActionResult> Telecharger(Guid id)
        {
            try
            {
                var url = await _pieceJointeService.GetUrlFichierAsync(id);
                if (string.IsNullOrEmpty(url))
                    return NotFound();

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du fichier {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize]  // ✅ Changez de [Authorize(Policy = "TicketDelete")] à [Authorize] seulement
        public async Task<ActionResult<ApiResponse<bool>>> Supprimer(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // 1. Récupérer la pièce jointe
                var pieceJointe = await _pieceJointeService.GetMetadataAsync(id);
                if (pieceJointe == null)
                    return NotFound(ApiResponse<bool>.Failure("Pièce jointe non trouvée"));

                // 2. Vérifier les droits de l'utilisateur
                var userRoles = await _userService.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                // 3. Si c'est une pièce jointe d'incident
                if (pieceJointe.IncidentId.HasValue)
                {
                    // Récupérer l'incident
                    var incident = await _incidentService.GetIncidentByIdAsync(pieceJointe.IncidentId.Value);

                    if (incident == null || incident.Data == null)
                        return NotFound(ApiResponse<bool>.Failure("Incident associé non trouvé"));

                    // Admin peut supprimer n'importe quelle pièce jointe
                    if (isAdmin)
                    {
                        // OK
                    }
                    // Commerçant peut supprimer uniquement les pièces jointes de SES incidents
                    else if (isCommercant && incident.Data.CreatedById == userId)
                    {
                        // OK
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Pour les autres types de pièces jointes (commentaires...), seul l'admin peut supprimer
                    if (!isAdmin)
                    {
                        return Forbid();
                    }
                }

                // 4. Procéder à la suppression
                var result = await _pieceJointeService.SupprimerFichierAsync(id);
                if (!result)
                    return NotFound();

                return Ok(ApiResponse<bool>.Success(true, "Fichier supprimé avec succès"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier {Id}", id);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Ajouter des pièces jointes à un incident
        /// </summary>
        [HttpPost("incident/{incidentId}/upload")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<List<PieceJointeDTO>>>> AjouterPiecesJointesAIncident(
            Guid incidentId,
            [FromForm] List<IFormFile> fichiers)
        {
            try
            {
                var userId = GetCurrentUserId(); // À ajouter
                var piecesAjoutees = new List<PieceJointeDTO>();

                foreach (var fichier in fichiers)
                {
                    var pieceDto = new CreatePieceJointeDTO
                    {
                        NomFichier = fichier.FileName,
                        Fichier = fichier
                    };

                    var pieceJointe = await _pieceJointeService.SauvegarderFichierPourIncidentAsync(
                        pieceDto, incidentId, userId);

                    piecesAjoutees.Add(new PieceJointeDTO
                    {
                        Id = pieceJointe.Id,
                        NomFichier = pieceJointe.NomFichier,
                        DateAjout = pieceJointe.DateAjout,
                        Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{pieceJointe.Id}"
                    });
                }

                return Ok(ApiResponse<List<PieceJointeDTO>>.Success(
                    piecesAjoutees,
                    $"{piecesAjoutees.Count} fichier(s) ajouté(s) avec succès"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de pièces jointes à l'incident {IncidentId}", incidentId);
                return StatusCode(500, ApiResponse<List<PieceJointeDTO>>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupérer toutes les pièces jointes d'un incident
        /// </summary>
        [HttpGet("incident/{incidentId}")]
        [Authorize(Policy = "IncidentRead")]
        public async Task<ActionResult<ApiResponse<List<PieceJointeDTO>>>> GetPiecesJointesByIncident(Guid incidentId)
        {
            try
            {
                var pieces = await _pieceJointeService.GetPiecesJointesByIncidentIdAsync(incidentId);

                return Ok(ApiResponse<List<PieceJointeDTO>>.Success(pieces));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des pièces jointes de l'incident {IncidentId}", incidentId);
                return StatusCode(500, ApiResponse<List<PieceJointeDTO>>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Supprimer des pièces jointes d'un incident
        /// </summary>
        [HttpDelete("incident/{incidentId}")]
        [Authorize(Policy = "IncidentUpdate")]
        public async Task<ActionResult<ApiResponse<bool>>> SupprimerPiecesJointesIncident(
            Guid incidentId,
            [FromBody] List<Guid> pieceJointeIds)
        {
            try
            {
                var userId = GetCurrentUserId();

                // ✅ Récupérer l'incident
                var incident = await _incidentService.GetIncidentByIdAsync(incidentId);
                if (incident == null || incident.Data == null)
                {
                    return NotFound(ApiResponse<bool>.Failure("Incident non trouvé"));
                }

                // ✅ Vérifier les droits
                var userRoles = await _userService.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                // Si c'est un commerçant, vérifier que l'incident lui appartient
                if (isCommercant && !isAdmin && incident.Data.CreatedById != userId)
                {
                    return Forbid(); // 403 - Accès non autorisé
                }

                // Vérifier que les pièces appartiennent bien à cet incident
                foreach (var id in pieceJointeIds)
                {
                    var piece = await _pieceJointeService.GetMetadataAsync(id);
                    if (piece == null)
                    {
                        return BadRequest(ApiResponse<bool>.Failure(
                            $"La pièce jointe {id} n'existe pas"));
                    }

                    if (piece.IncidentId != incidentId)
                    {
                        return BadRequest(ApiResponse<bool>.Failure(
                            $"La pièce jointe {id} n'appartient pas à cet incident"));
                    }
                }

                var result = await _pieceJointeService.SupprimerPiecesJointesIncidentAsync(pieceJointeIds);

                if (!result)
                    return BadRequest(ApiResponse<bool>.Failure("Certaines pièces n'ont pas pu être supprimées"));

                return Ok(ApiResponse<bool>.Success(true, "Pièces jointes supprimées avec succès"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des pièces jointes de l'incident {IncidentId}", incidentId);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }
    }
}