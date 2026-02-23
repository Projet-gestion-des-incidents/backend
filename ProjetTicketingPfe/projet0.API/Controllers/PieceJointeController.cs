using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;

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
    }
}