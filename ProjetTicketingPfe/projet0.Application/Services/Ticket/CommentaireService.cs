using Microsoft.AspNetCore.Http;
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
        Task<CommentaireDTO> CreateCommentaireAsync(Guid ticketId, CreateCommentaireDTO dto, Guid userId);
    }

    public class CommentaireService : ICommentaireService
    {
        private readonly ICommentaireRepository _commentaireRepository;
        private readonly IPieceJointeService _pieceJointeService;
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;  // Ajouter
        private readonly INotificationService _notificationService;  // Ajouter
        private readonly ILogger<CommentaireService> _logger;

        public CommentaireService(
            ICommentaireRepository commentaireRepository,
            IPieceJointeService pieceJointeService,
            IUserRepository userRepository,
            ITicketRepository ticketRepository,  // Ajouter
            INotificationService notificationService,  // Ajouter
            ILogger<CommentaireService> logger)
        {
            _commentaireRepository = commentaireRepository;
            _pieceJointeService = pieceJointeService;
            _userRepository = userRepository;
            _ticketRepository = ticketRepository;  // Ajouter
            _notificationService = notificationService;  // Ajouter
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
                            Fichier = fichier
                        };

                        // Utiliser la méthode pour commentaires
                        var pieceJointe = await _pieceJointeService.SauvegarderFichierPourCommentaireAsync(
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
                TicketId = commentaire.TicketId,
                TicketReference = commentaire.Ticket?.ReferenceTicket,
                PiecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                {
                    Id = p.Id,
                    NomFichier = p.NomFichier,
                    
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
                TicketId = commentaire.TicketId,
                TicketReference = commentaire.Ticket?.ReferenceTicket,
                PiecesJointes = commentaire.PiecesJointes?.Select(p => new PieceJointeDTO
                {
                    Id = p.Id,
                    NomFichier = p.NomFichier,
                    
                    DateAjout = p.DateAjout
                }).ToList() ?? new()
            };

            return dto;
        }

        public async Task<CommentaireDTO> CreateCommentaireAsync(Guid ticketId, CreateCommentaireDTO dto, Guid userId)
        {
            _logger.LogInformation("Création commentaire pour ticket {TicketId}", ticketId);

            // 1. Récupérer le ticket et l'utilisateur
            var ticket = await _ticketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} non trouvé", ticketId);
                throw new Exception("Ticket non trouvé");
            }

            // 2. Récupérer le rôle de l'utilisateur
            var userRoles = await _userRepository.GetUserRolesAsync(userId);
            var isAdmin = userRoles.Contains("Admin");
            var isTechnicien = userRoles.Contains("Technicien");
            var isCommercant = userRoles.Contains("Commercant");

            // 3. Récupérer l'auteur
            var auteur = await _userRepository.GetByIdAsync(userId);
            string auteurNom = auteur != null ? $"{auteur.Nom} {auteur.Prenom}" : "Un utilisateur";

            // 4. Créer le commentaire
            var commentaire = new CommentaireTicket
            {
                Id = Guid.NewGuid(),
                Message = dto.Message ?? string.Empty,
                DateCreation = DateTime.UtcNow,
                EstInterne = dto.EstInterne,
                TicketId = ticketId,
                AuteurId = userId,
                PiecesJointes = new List<PieceJointe>()
            };

            await _commentaireRepository.AddAsync(commentaire);

            // 5. Ajouter les fichiers si présents
            if (dto.Fichiers != null && dto.Fichiers.Any())
            {
                _logger.LogInformation("Ajout de {Count} fichier(s) au commentaire", dto.Fichiers.Count);

                foreach (var fichier in dto.Fichiers)
                {
                    var pieceDto = new CreatePieceJointeDTO
                    {
                        NomFichier = fichier.FileName,
                        Fichier = fichier
                    };

                    var pieceJointe = await _pieceJointeService.SauvegarderFichierPourCommentaireAsync(
                        pieceDto, commentaire.Id, userId);
                }
            }

            await _commentaireRepository.SaveChangesAsync();

            // ======================================================
            // 🔔 NOTIFICATIONS POUR COMMENTAIRE
            // ======================================================

            string messageCourt = dto.Message?.Length > 50 ? dto.Message.Substring(0, 50) + "..." : dto.Message ?? "(pièce jointe uniquement)";
            bool aDesPieces = dto.Fichiers != null && dto.Fichiers.Any();
            string typeMessage = aDesPieces ? (string.IsNullOrWhiteSpace(dto.Message) ? "a ajouté une pièce jointe" : "a commenté") : "a commenté";

            // 1. Si l'auteur est ADMIN -> notifier le TECHNICIEN assigné
            if (isAdmin)
            {
                if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != userId)
                {
                    await _notificationService.CreateCommentNotificationAsync(
                        ticket.AssigneeId.Value,
                        commentaire.Id,
                        ticketId,
                        $"Nouveau commentaire sur le ticket {ticket.ReferenceTicket}",
                        $"L'administrateur {auteurNom} {typeMessage} sur le ticket '{ticket.TitreTicket}': \"{messageCourt}\""
                    );
                    _logger.LogInformation("Notification envoyée au technicien {TechnicienId}", ticket.AssigneeId.Value);
                }
            }
            // 2. Si l'auteur est TECHNICIEN -> notifier le CREATEUR du ticket
            else if (isTechnicien)
            {
                if (ticket.CreateurId != userId)
                {
                    await _notificationService.CreateCommentNotificationAsync(
                        ticket.CreateurId,
                        commentaire.Id,
                        ticketId,
                        $"Nouveau commentaire sur le ticket {ticket.ReferenceTicket}",
                        $"Le technicien {auteurNom} {typeMessage} sur votre ticket '{ticket.TitreTicket}': \"{messageCourt}\""
                    );
                    _logger.LogInformation("Notification envoyée au créateur {CreateurId} du ticket", ticket.CreateurId);
                }
            }

            // 3. Si le commentaire est interne et que l'auteur n'est pas admin, notifier les ADMINS
            if (dto.EstInterne && !isAdmin)
            {
                var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    if (admin.Id != userId)
                    {
                        await _notificationService.CreateCommentNotificationAsync(
                            admin.Id,
                            commentaire.Id,
                            ticketId,
                            $"Commentaire interne sur le ticket {ticket.ReferenceTicket}",
                            $"{auteurNom} a ajouté un commentaire interne sur le ticket '{ticket.TitreTicket}'"
                        );
                    }
                }
                _logger.LogInformation("Notifications internes envoyées aux admins");
            }

            _logger.LogInformation("Notifications envoyées pour le commentaire {CommentaireId}", commentaire.Id);

            // 6. Retourner le DTO
            var commentaireComplet = await _commentaireRepository.GetCommentaireWithPiecesJointesAsync(commentaire.Id);
            return MapToDto(commentaireComplet);
        }
        private async Task<string> ConvertirFichierEnBase64(IFormFile fichier)
        {
            using var memoryStream = new MemoryStream();
            await fichier.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            return Convert.ToBase64String(bytes);
        }
        #endregion
    }
}