using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.DTOs.TicketDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Ticket;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Security.Claims;
using projet0.Application.Services.Ticket;


namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/commentaires")]
    [Authorize]
    public class CommentaireController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ICommentaireRepository _commentaireRepository;
        private readonly IPieceJointeService _pieceJointeService;
        private readonly ILogger<CommentaireController> _logger;
        private readonly ICommentaireService _commentaireService;  

        public CommentaireController(
            ITicketService ticketService,
            ICommentaireRepository commentaireRepository,
            IPieceJointeService pieceJointeService,
            ICommentaireService commentaireService,
            ILogger<CommentaireController> logger)
        {
            _ticketService = ticketService;
            _commentaireRepository = commentaireRepository;
            _pieceJointeService = pieceJointeService;
            _logger = logger;
            _commentaireService = commentaireService; 

        }
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            throw new UnauthorizedAccessException("Utilisateur non authentifié");
        }

        [HttpGet]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<List<CommentaireDTO>>>> GetCommentaires(Guid ticketId)
        {
            try
            {
                var commentaires = await _commentaireRepository.GetCommentairesByTicketIdAsync(ticketId);

                var dtos = commentaires.Select(c => new CommentaireDTO
                {
                    Id = c.Id,
                    Message = c.Message,
                    DateCreation = c.DateCreation,
                    EstInterne = c.EstInterne,
                    AuteurId = c.AuteurId,
                    AuteurNom = c.Auteur != null ? $"{c.Auteur.Nom} {c.Auteur.Prenom}" : "Inconnu",
                    TicketId = c.TicketId, 
                    TicketReference = c.Ticket?.ReferenceTicket,  
                    PiecesJointes = c.PiecesJointes?.Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        NomFichier = p.NomFichier,
                        ContentType = p.ContentType,
                        DateAjout = p.DateAjout,
                        Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{p.Id}"
                    }).ToList() ?? new()
                }).ToList();

                return Ok(ApiResponse<List<CommentaireDTO>>.Success(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commentaires");
                return StatusCode(500, ApiResponse<List<CommentaireDTO>>.Failure("Erreur interne"));
            }
        }

        [HttpPost]
        [Authorize(Policy = "TicketComment")]
        public async Task<ActionResult<ApiResponse<CommentaireDTO>>> AjouterCommentaire(
            Guid ticketId,
            [FromForm] CreateCommentaireDTO dto)
        {
            try
            {
                _logger.LogInformation("=== DÉBUT AJOUT COMMENTAIRE ===");

                if (string.IsNullOrWhiteSpace(dto.Message) && (dto.Fichiers == null || !dto.Fichiers.Any()))
                {
                    _logger.LogWarning("Tentative de création d'un commentaire vide");
                    return BadRequest(ApiResponse<CommentaireDTO>.Failure(
                        "Un commentaire doit contenir soit un message, soit au moins une pièce jointe, soit les deux."));
                }

                var userId = GetCurrentUserId();

                // Vérifier que le ticket existe
                var ticketResult = await _ticketService.GetTicketByIdAsync(ticketId);
                if (!ticketResult.IsSuccess || ticketResult.Data == null)
                {
                    _logger.LogWarning("Ticket {TicketId} non trouvé", ticketId);
                    return NotFound(ApiResponse<CommentaireDTO>.Failure("Ticket non trouvé"));
                }

                // ✅ Utiliser le service au lieu de créer directement
                var commentaireDto = await _commentaireService.CreateCommentaireAsync(ticketId, dto, userId);

                // Ajouter les URLs des pièces jointes
                foreach (var piece in commentaireDto.PiecesJointes)
                {
                    piece.Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{piece.Id}";
                }

                return Ok(ApiResponse<CommentaireDTO>.Success(commentaireDto, "Commentaire ajouté avec succès"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERREUR DÉTAILLÉE: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<CommentaireDTO>.Failure($"Erreur interne: {ex.Message}"));
            }
        }

        /// <summary>
        /// Récupérer un commentaire par son ID
        /// </summary>
        /// 
        [HttpGet("{commentaireId}")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<CommentaireDTO>>> GetCommentaireById(Guid commentaireId)
        {
            try
            {
                _logger.LogInformation("Récupération du commentaire {CommentaireId}", commentaireId);

                var commentaire = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(commentaireId);

                if (commentaire == null)
                    return NotFound(ApiResponse<CommentaireDTO>.Failure("Commentaire non trouvé"));

                var result = new CommentaireDTO
                {
                    Id = commentaire.Id,
                    Message = commentaire.Message,
                    DateCreation = commentaire.DateCreation,
                    EstInterne = commentaire.EstInterne,
                    AuteurId = commentaire.AuteurId,
                    AuteurNom = commentaire.Auteur != null ? $"{commentaire.Auteur.Nom} {commentaire.Auteur.Prenom}" : "Inconnu",
                    PiecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        NomFichier = p.NomFichier,
                        
                        DateAjout = p.DateAjout,
                        Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{p.Id}"
                    }).ToList() ?? new()
                };

                return Ok(ApiResponse<CommentaireDTO>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du commentaire {CommentaireId}", commentaireId);
                return StatusCode(500, ApiResponse<CommentaireDTO>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Mettre à jour un commentaire (message et/ou pièces jointes)
        /// </summary>
        /// 
        [HttpPut("{commentaireId}")]
        [Authorize(Policy = "TicketComment")]
        public async Task<ActionResult<ApiResponse<UpdateCommentaireResponseDTO>>> UpdateCommentaire(
            Guid commentaireId,
            [FromForm] UpdateCommentaireDTO dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour commentaire {CommentaireId}", commentaireId);
                // Vérifier la cohérence des IDs
                if (commentaireId != dto.Id)
                {
                    return BadRequest(ApiResponse<UpdateCommentaireResponseDTO>.Failure(
                        "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête"));
                }
                var userId = GetCurrentUserId();

                // Vérifier que le commentaire existe
                var commentaireExistant = await _commentaireRepository.GetByIdAsync(commentaireId);
                if (commentaireExistant == null)
                    return NotFound(ApiResponse<UpdateCommentaireResponseDTO>.Failure("Commentaire non trouvé"));                

                // Appeler le service
                var result = await _commentaireService.UpdateCommentaireAsync(commentaireId, dto, userId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                // Ajouter les URLs des nouvelles pièces jointes
                foreach (var piece in result.Data.PiecesJointes)
                {
                    piece.Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{piece.Id}";
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du commentaire {CommentaireId}", commentaireId);
                return StatusCode(500, ApiResponse<UpdateCommentaireResponseDTO>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Supprimer un commentaire (supprime aussi ses pièces jointes)
        /// </summary>
        /// 
        [HttpDelete("{commentaireId}")]
        [Authorize(Policy = "TicketDelete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCommentaire(Guid commentaireId)
        {
            try
            {
                _logger.LogInformation("Suppression commentaire {CommentaireId}", commentaireId);

                var userId = GetCurrentUserId();

                // Vérifier que le commentaire existe
                var commentaireExistant = await _commentaireRepository.GetByIdAsync(commentaireId);
                if (commentaireExistant == null)
                    return NotFound(ApiResponse<bool>.Failure("Commentaire non trouvé"));

                // Vérifier les permissions (optionnel)
                if (commentaireExistant.AuteurId != userId)
                {
                    // Vérifier si l'utilisateur est admin (à adapter)
                    // return Forbidden();
                }

                var result = await _commentaireService.DeleteCommentaireAsync(commentaireId);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du commentaire {CommentaireId}", commentaireId);
                return StatusCode(500, ApiResponse<bool>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupère les pièces jointes d'un commentaire
        /// </summary>
        /// 
        [HttpGet("{commentaireId}/pieces-jointes")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<List<PieceJointeDTO>>>> GetPiecesJointesByCommentaire(Guid commentaireId)
        {
            try
            {
                _logger.LogInformation("Récupération des pièces jointes du commentaire {CommentaireId}", commentaireId);

                var commentaire = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(commentaireId);
                if (commentaire == null)
                    return NotFound(ApiResponse<List<PieceJointeDTO>>.Failure("Commentaire non trouvé"));

                var piecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                {
                    Id = p.Id,
                    NomFichier = p.NomFichier,
                    
                    DateAjout = p.DateAjout,
                    Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{p.Id}"
                }).ToList() ?? new();

                return Ok(ApiResponse<List<PieceJointeDTO>>.Success(piecesJointes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des pièces jointes");
                return StatusCode(500, ApiResponse<List<PieceJointeDTO>>.Failure("Erreur interne"));
            }
        }

        /// <summary>
        /// Récupère les commentaires du technicien connecté
        /// </summary>
        /// <param name="ticketId">ID du ticket (optionnel). Si fourni, filtre les commentaires pour ce ticket spécifique</param>
        /// <returns>Liste des commentaires du technicien</returns>
        /// 
        [HttpGet("mes-commentaires")]
        [Authorize(Policy = "TicketRead")]
        public async Task<ActionResult<ApiResponse<List<CommentaireDTO>>>> GetMesCommentaires([FromQuery] Guid? ticketId = null)
        {
            try
            {
                _logger.LogInformation("Récupération des commentaires du technicien connecté. Filtre par ticket: {TicketFilter}",
                    ticketId.HasValue ? ticketId.Value.ToString() : "Aucun");

                var technicienId = GetCurrentUserId();
                IEnumerable<CommentaireTicket> commentaires;

                if (ticketId.HasValue)
                {
                    // Cas 1: Filtrer par ticket spécifique
                    _logger.LogInformation("Recherche des commentaires du technicien {TechnicienId} pour le ticket {TicketId}",
                        technicienId, ticketId.Value);

                    // Vérification optionnelle que le ticket existe
                    var ticketResult = await _ticketService.GetTicketByIdAsync(ticketId.Value);
                    if (!ticketResult.IsSuccess || ticketResult.Data == null)
                    {
                        _logger.LogWarning("Ticket {TicketId} non trouvé", ticketId.Value);
                        return NotFound(ApiResponse<List<CommentaireDTO>>.Failure("Ticket non trouvé"));
                    }

                    commentaires = await _commentaireRepository.GetCommentairesByTicketAndTechnicienAsync(
                        ticketId.Value, technicienId);
                }
                else
                {
                    // Cas 2: Tous les commentaires du technicien
                    _logger.LogInformation("Recherche de tous les commentaires du technicien {TechnicienId}", technicienId);
                    commentaires = await _commentaireRepository.GetCommentairesByTechnicienAsync(technicienId);
                }

                // Mapping vers DTOs
                var dtos = commentaires.Select(c => new CommentaireDTO
                {
                    Id = c.Id,
                    Message = c.Message,
                    DateCreation = c.DateCreation,
                    EstInterne = c.EstInterne,
                    AuteurId = c.AuteurId,
                    AuteurNom = c.Auteur != null ? $"{c.Auteur.Nom} {c.Auteur.Prenom}" : "Inconnu",
                    TicketId = c.TicketId, 
                    TicketReference = c.Ticket?.ReferenceTicket, 
                    PiecesJointes = c.PiecesJointes?.Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        NomFichier = p.NomFichier,
                        DateAjout = p.DateAjout,
                        Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{p.Id}"
                    }).ToList() ?? new()
                }).ToList();

                string message = dtos.Any()
                    ? $"{dtos.Count} commentaire(s) récupéré(s) avec succès"
                    : "Aucun commentaire trouvé";

                return Ok(ApiResponse<List<CommentaireDTO>>.Success(dtos, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commentaires du technicien");
                return StatusCode(500, ApiResponse<List<CommentaireDTO>>.Failure("Erreur interne"));
            }
        }
    }
}