using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.DTOs.TicketDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Ticket
{
    public interface ICommentaireService
    {
        Task<ApiResponse<CommentaireDTO>> GetCommentaireByIdAsync(Guid id);
        Task<ApiResponse<UpdateCommentaireResponseDTO>> UpdateCommentaireAsync(Guid id, UpdateCommentaireDTO dto, Guid userId);
        Task<ApiResponse<bool>> DeleteCommentaireAsync(Guid id);
    }

    public class CommentaireService : ICommentaireService
    {
        private readonly ICommentaireRepository _commentaireRepository;
        private readonly IPieceJointeService _pieceJointeService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CommentaireService> _logger;

        public CommentaireService(
            ICommentaireRepository commentaireRepository,
            IPieceJointeService pieceJointeService,
            IUserRepository userRepository,
            ILogger<CommentaireService> logger)
        {
            _commentaireRepository = commentaireRepository;
            _pieceJointeService = pieceJointeService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<CommentaireDTO>> GetCommentaireByIdAsync(Guid id)
        {
            try
            {
                var commentaire = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(id);

                if (commentaire == null)
                    return ApiResponse<CommentaireDTO>.Failure("Commentaire non trouvé");

                var dto = MapToDto(commentaire);
                return ApiResponse<CommentaireDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du commentaire {Id}", id);
                return ApiResponse<CommentaireDTO>.Failure("Erreur interne");
            }
        }

        // Dans CommentaireService.cs, méthode UpdateCommentaireAsync

        public async Task<ApiResponse<UpdateCommentaireResponseDTO>> UpdateCommentaireAsync(
            Guid id,
            UpdateCommentaireDTO dto,
            Guid userId)
        {
            try
            {
                _logger.LogInformation("Mise à jour commentaire {Id}", id);

                // 1. Récupérer le commentaire avec ses pièces jointes
                var commentaire = await _commentaireRepository.GetCommentaireForUpdateAsync(id);
                if (commentaire == null)
                    return ApiResponse<UpdateCommentaireResponseDTO>.Failure("Commentaire non trouvé");

                var piecesSupprimees = new List<Guid>();
                var piecesAjoutees = new List<Guid>();

                // 2. Supprimer les pièces jointes demandées
                if (dto.PiecesJointesASupprimer != null && dto.PiecesJointesASupprimer.Any())
                {
                    var idsValides = commentaire.PiecesJointes?
                        .Where(p => dto.PiecesJointesASupprimer.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToList() ?? new();

                    foreach (var pieceId in idsValides)
                    {
                        var success = await _pieceJointeService.SupprimerFichierAsync(pieceId);
                        if (success) piecesSupprimees.Add(pieceId);
                    }
                }

                // 3. Ajouter de nouveaux fichiers
                if (dto.NouveauxFichiers != null && dto.NouveauxFichiers.Any())
                {
                    foreach (var fichier in dto.NouveauxFichiers)
                    {
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

                        piecesAjoutees.Add(pieceJointe.Id);
                    }
                }

                // 4. Gérer la mise à jour du message avec le flag EffacerMessage
                if (dto.EffacerMessage)
                {
                    // Si EffacerMessage = true, on vide le message
                    commentaire.Message = string.Empty;
                    _logger.LogInformation("Message effacé (flag EffacerMessage = true)");
                }
                else if (dto.Message != null)
                {
                    // Si EffacerMessage = false et Message fourni, on met à jour
                    commentaire.Message = dto.Message;
                    _logger.LogInformation("Message mis à jour: '{Message}'", dto.Message);
                }
                else
                {
                    // Si EffacerMessage = false et Message non fourni, on garde l'ancien
                    _logger.LogInformation("Message non modifié, conservation de: '{Message}'", commentaire.Message);
                }

                // 5. Mettre à jour EstInterne (toujours)
                commentaire.EstInterne = dto.EstInterne;

                await _commentaireRepository.SaveChangesAsync();

                // 6. VÉRIFICATION CRITIQUE - Commentaire vide ?
                var commentaireMisAJour = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(id);

                bool aUnMessage = !string.IsNullOrWhiteSpace(commentaireMisAJour.Message);
                bool aDesPiecesJointes = commentaireMisAJour.PiecesJointes != null && commentaireMisAJour.PiecesJointes.Any();

                // Si le commentaire n'a ni message ni pièces jointes, on le supprime
                if (!aUnMessage && !aDesPiecesJointes)
                {
                    _logger.LogWarning("Commentaire {Id} vide après mise à jour - suppression automatique", id);
                    await _commentaireRepository.DeleteAsync(commentaireMisAJour);
                    await _commentaireRepository.SaveChangesAsync();

                    return ApiResponse<UpdateCommentaireResponseDTO>.Success(
                        new UpdateCommentaireResponseDTO(),
                        "Commentaire supprimé car il ne contient plus ni message ni pièce jointe");
                }

                // 7. Préparer la réponse
                var responseDto = MapToUpdateResponse(commentaireMisAJour);
                responseDto.PiecesJointesSupprimees = piecesSupprimees;
                responseDto.PiecesJointesAjoutees = piecesAjoutees;

                string messageReussite = aDesPiecesJointes
                    ? "Commentaire mis à jour avec succès"
                    : "Commentaire mis à jour (message seulement)";

                return ApiResponse<UpdateCommentaireResponseDTO>.Success(responseDto, messageReussite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du commentaire {Id}", id);
                return ApiResponse<UpdateCommentaireResponseDTO>.Failure("Erreur interne");
            }
        }

        public async Task<ApiResponse<bool>> DeleteCommentaireAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression commentaire {Id}", id);

                // 1. Récupérer le commentaire avec ses pièces jointes
                var commentaire = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(id);
                if (commentaire == null)
                    return ApiResponse<bool>.Failure("Commentaire non trouvé");

                // 2. Supprimer les fichiers physiques des pièces jointes
                if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                {
                    foreach (var piece in commentaire.PiecesJointes)
                    {
                        await _pieceJointeService.SupprimerFichierAsync(piece.Id);
                    }
                }

                // 3. Supprimer le commentaire (les pièces jointes seront supprimées en cascade)
                await _commentaireRepository.DeleteAsync(commentaire);
                await _commentaireRepository.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Commentaire supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du commentaire {Id}", id);
                return ApiResponse<bool>.Failure("Erreur interne");
            }
        }

        #region Méthodes privées

        private CommentaireDTO MapToDto(CommentaireTicket commentaire)
        {
            return new CommentaireDTO
            {
                Id = commentaire.Id,
                Message = commentaire.Message,
                DateCreation = commentaire.DateCreation,
                EstInterne = commentaire.EstInterne,
                AuteurId = commentaire.AuteurId,
                AuteurNom = commentaire.Auteur != null
                    ? $"{commentaire.Auteur.Nom} {commentaire.Auteur.Prenom}"
                    : "Inconnu",
                PiecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                {
                    Id = p.Id,
                    NomFichier = p.NomFichier,
                    Taille = p.Taille,
                    ContentType = p.ContentType,
                    TypePieceJointe = p.TypePieceJointe,
                    DateAjout = p.DateAjout
                    // L'URL sera générée côté contrôleur
                }).ToList() ?? new()
            };
        }

        private UpdateCommentaireResponseDTO MapToUpdateResponse(CommentaireTicket commentaire)
        {
            var dto = new UpdateCommentaireResponseDTO
            {
                Id = commentaire.Id,
                Message = commentaire.Message,
                DateCreation = commentaire.DateCreation,
                EstInterne = commentaire.EstInterne,
                AuteurId = commentaire.AuteurId,
                AuteurNom = commentaire.Auteur != null
                    ? $"{commentaire.Auteur.Nom} {commentaire.Auteur.Prenom}"
                    : "Inconnu",
                PiecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                {
                    Id = p.Id,
                    NomFichier = p.NomFichier,
                    Taille = p.Taille,
                    ContentType = p.ContentType,
                    TypePieceJointe = p.TypePieceJointe,
                    DateAjout = p.DateAjout
                }).ToList() ?? new()
            };

            return dto;
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

        #endregion
    }
}