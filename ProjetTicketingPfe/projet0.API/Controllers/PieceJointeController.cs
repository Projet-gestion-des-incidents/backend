using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
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

        public PieceJointeController(
            IPieceJointeService pieceJointeService,
            ILogger<PieceJointeController> logger)
        {
            _pieceJointeService = pieceJointeService;
            _logger = logger;
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
        [Authorize(Policy = "TicketDelete")]
        public async Task<ActionResult<ApiResponse<bool>>> Supprimer(Guid id)
        {
            try
            {
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
        // Dans PieceJointeController.cs

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
                // Vérifier que les pièces appartiennent bien à cet incident
                foreach (var id in pieceJointeIds)
                {
                    // ✅ Utiliser GetMetadataAsync qui retourne un objet PieceJointe
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