using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Ticket;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Security.Claims;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/ticket/{ticketId}/commentaires")]
    [Authorize]
    public class CommentaireController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ICommentaireRepository _commentaireRepository;
        private readonly IPieceJointeService _pieceJointeService;
        private readonly ILogger<CommentaireController> _logger;

        public CommentaireController(
            ITicketService ticketService,
            ICommentaireRepository commentaireRepository,
            IPieceJointeService pieceJointeService,
            ILogger<CommentaireController> logger)
        {
            _ticketService = ticketService;
            _commentaireRepository = commentaireRepository;
            _pieceJointeService = pieceJointeService;
            _logger = logger;
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
                        NomFichier = p.NomFichier,
                        Taille = p.Taille,
                        ContentType = p.ContentType,
                        TypePieceJointe = p.TypePieceJointe,
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
    [FromForm] CreateCommentaireDTO dto,
    [FromForm] List<IFormFile>? fichiers)
        {
            try
            {
                _logger.LogInformation("=== DÉBUT AJOUT COMMENTAIRE ===");
                _logger.LogInformation("TicketId reçu: {TicketId}", ticketId);
                _logger.LogInformation("TicketId en chaîne: {TicketIdString}", ticketId.ToString());
                _logger.LogInformation("Message: {Message}", dto.Message);
                _logger.LogInformation("EstInterne: {EstInterne}", dto.EstInterne);
                _logger.LogInformation("Nombre de fichiers: {NbFichiers}", fichiers?.Count ?? 0);

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
                    Message = dto.Message,
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
                if (fichiers != null && fichiers.Any())
                {
                    _logger.LogInformation("Traitement de {Count} fichier(s)", fichiers.Count);

                    foreach (var fichier in fichiers)
                    {
                        try
                        {
                            _logger.LogInformation("Traitement fichier: {FileName}, Taille: {Length}, ContentType: {ContentType}",
                                fichier.FileName, fichier.Length, fichier.ContentType);

                            var pieceDto = new CreatePieceJointeDTO
                            {
                                NomFichier = fichier.FileName,
                                Taille = fichier.Length,
                                ContentType = fichier.ContentType,
                                TypePieceJointe = DeterminerTypePieceJointe(fichier.FileName),
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
                        Taille = p.Taille,
                        ContentType = p.ContentType,
                        TypePieceJointe = p.TypePieceJointe,
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
        private TypePieceJointe DeterminerTypePieceJointe(string nomFichier)
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
        }
    }
}