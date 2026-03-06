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
                    PiecesJointes = c.PiecesJointes?.Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        
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
                _logger.LogInformation("TicketId reçu: {TicketId}", ticketId);
                _logger.LogInformation("TicketId en chaîne: {TicketIdString}", ticketId.ToString());
                _logger.LogInformation("Message: {Message}", dto.Message);
                _logger.LogInformation("EstInterne: {EstInterne}", dto.EstInterne);
                _logger.LogInformation("Nombre de fichiers: {NbFichiers}", dto.Fichiers?.Count ?? 0);

                var userId = GetCurrentUserId();
                _logger.LogInformation("Utilisateur connecté: {UserId}", userId);

                // Vérifier que le ticket existe
                _logger.LogInformation("Appel de GetTicketByIdAsync avec ID: {TicketId}", ticketId);
                var ticketResult = await _ticketService.GetTicketByIdAsync(ticketId);

                _logger.LogInformation("Résultat de GetTicketByIdAsync - IsSuccess: {IsSuccess}, Data null: {DataNull}, Message: {Message}",
                    ticketResult.IsSuccess,
                    ticketResult.Data == null,
                    ticketResult.Message);

                if (!ticketResult.IsSuccess || ticketResult.Data == null)
                {
                    _logger.LogWarning("Ticket {TicketId} non trouvé", ticketId);
                    return NotFound(ApiResponse<CommentaireDTO>.Failure("Ticket non trouvé"));
                }

                _logger.LogInformation("Ticket trouvé: {Reference}, Titre: {Titre}",
                    ticketResult.Data.ReferenceTicket,
                    ticketResult.Data.TitreTicket);


                var commentaire = new CommentaireTicket
                {
                    Id = Guid.NewGuid(),
                    Message = dto.Message ?? string.Empty,  // Si null, mettre chaîne vide
                    DateCreation = DateTime.UtcNow,
                    EstInterne = dto.EstInterne,
                    TicketId = ticketId,
                    AuteurId = userId,
                    PiecesJointes = new List<PieceJointe>()
                };

                _logger.LogInformation("Création commentaire ID: {CommentaireId}", commentaire.Id);
                await _commentaireRepository.AddAsync(commentaire);
                _logger.LogInformation("Commentaire ajouté en base");

                // Gérer les fichiers uploadés
                if (dto.Fichiers != null && dto.Fichiers.Any())
                {
                    _logger.LogInformation("Traitement de {Count} fichier(s)", dto.Fichiers.Count);

                    foreach (var fichier in dto.Fichiers)
                    {
                        try
                        {
                            _logger.LogInformation("Traitement fichier: {FileName}, Taille: {Length}, ContentType: {ContentType}",
                                fichier.FileName, fichier.Length, fichier.ContentType);

                            var pieceDto = new CreatePieceJointeDTO
                            {
                                NomFichier = fichier.FileName,
                                
                                //TypePieceJointe = DeterminerTypePieceJointe(fichier.FileName),
                                Fichier = fichier
                            };

                            var pieceJointe = await _pieceJointeService.SauvegarderFichierAsync(
                                pieceDto, commentaire.Id, userId);

                            _logger.LogInformation("Fichier sauvegardé avec ID: {PieceJointeId}", pieceJointe.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur lors de la sauvegarde du fichier {FileName}", fichier.FileName);
                            throw;
                        }
                    }
                }

                await _commentaireRepository.SaveChangesAsync();
                _logger.LogInformation("Commentaire sauvegardé avec succès");

                // Recharger avec les relations
                var commentaireComplet = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(commentaire.Id);

                var result = new CommentaireDTO
                {
                    Id = commentaireComplet.Id,
                    Message = commentaireComplet.Message,
                    DateCreation = commentaireComplet.DateCreation,
                    EstInterne = commentaireComplet.EstInterne,
                    AuteurId = commentaireComplet.AuteurId,
                    AuteurNom = commentaireComplet.Auteur != null ? $"{commentaireComplet.Auteur.Nom} {commentaireComplet.Auteur.Prenom}" : "Inconnu",
                    PiecesJointes = commentaireComplet.PiecesJointes?.Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        NomFichier = p.NomFichier,
                        
                        DateAjout = p.DateAjout,
                        Url = $"{Request.Scheme}://{Request.Host}/api/pieces-jointes/{p.Id}"
                    }).ToList() ?? new()
                };

                return Ok(ApiResponse<CommentaireDTO>.Success(result, "Commentaire ajouté avec succès"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERREUR DÉTAILLÉE: {Message}, InnerException: {InnerException}, StackTrace: {StackTrace}",
                    ex.Message, ex.InnerException?.Message, ex.StackTrace);
                return StatusCode(500, ApiResponse<CommentaireDTO>.Failure($"Erreur interne: {ex.Message}"));
            }
        }
        /*private TypePieceJointe DeterminerTypePieceJointe(string nomFichier)
        {
            var extension = Path.GetExtension(nomFichier).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => TypePieceJointe.Image,
                ".pdf" or ".doc" or ".docx" or ".txt" => TypePieceJointe.Document,
                ".xls" or ".xlsx" or ".csv" => TypePieceJointe.Tableur,
                ".zip" or ".rar" or ".7z" => TypePieceJointe.Archive,
                _ => TypePieceJointe.Autre
            };
        }*/

        /// <summary>
        /// Récupérer un commentaire par son ID
        /// </summary>
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
    }
}