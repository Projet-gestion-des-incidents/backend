// Fichier: projet0.Application/Services/PieceJointeService.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;  // ← AJOUTER
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.IO;
using System.Threading.Tasks;


namespace projet0.Application.Services
{
    public class PieceJointeService : IPieceJointeService
    {
        private readonly IPieceJointeRepository _pieceJointeRepository;  
        private readonly ICommentaireRepository _commentaireRepository;   
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _uploadPath = "uploads/pieces-jointes";
        private readonly ILogger<PieceJointeService> _logger;

        public PieceJointeService(
            IPieceJointeRepository pieceJointeRepository, 
            ICommentaireRepository commentaireRepository,   
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor, ILogger<PieceJointeService> logger)  

        {
            _pieceJointeRepository = pieceJointeRepository;
            _commentaireRepository = commentaireRepository;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;  

        }

        /// <summary>
        /// Sauvegarde un fichier et ses métadonnées
        /// </summary>
        public async Task<PieceJointe> SauvegarderFichierAsync(
    CreatePieceJointeDTO dto,
    Guid commentaireId,
    Guid uploadedById)
        {
            _logger.LogInformation("Sauvegarde fichier - CommentaireId: {CommentaireId}, Nom: {NomFichier}",
                commentaireId, dto.NomFichier);

            try
            {
                // 1. Vérifier que le commentaire existe
                var commentaire = await _commentaireRepository.GetByIdAsync(commentaireId);
                if (commentaire == null)
                {
                    _logger.LogWarning("Commentaire {CommentaireId} non trouvé", commentaireId);
                    throw new ArgumentException($"Le commentaire avec ID {commentaireId} n'existe pas");
                }

                // 2. Sauvegarder le fichier physiquement
                _logger.LogInformation("Sauvegarde physique du fichier");
                string cheminFichier = await SauvegarderFichierPhysique(dto);
                _logger.LogInformation("Fichier sauvegardé physiquement: {Chemin}", cheminFichier);

                // 3. Créer l'entité PieceJointe
                var pieceJointe = new PieceJointe
                {
                    Id = Guid.NewGuid(),
                    NomFichier = dto.NomFichier,
                    CheminStockage = cheminFichier,
                    Taille = dto.Taille,
                    ContentType = dto.ContentType,
                    TypePieceJointe = dto.TypePieceJointe,
                    DateAjout = DateTime.UtcNow,
                    CommentaireId = commentaireId,
                    UploadedById = uploadedById
                };

                // 4. Sauvegarder dans la base via le repository
                _logger.LogInformation("Sauvegarde en base");
                await _pieceJointeRepository.AddAsync(pieceJointe);
                await _pieceJointeRepository.SaveChangesAsync();

                _logger.LogInformation("Fichier sauvegardé avec succès ID: {PieceJointeId}", pieceJointe.Id);
                return pieceJointe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans SauvegarderFichierAsync pour {NomFichier}", dto.NomFichier);
                throw;
            }
        }

        /// <summary>
        /// Récupère l'URL d'un fichier
        /// </summary>
        public async Task<string> GetUrlFichierAsync(Guid pieceJointeId)
        {
            // Utiliser le repository pour récupérer les métadonnées
            var pieceJointe = await _pieceJointeRepository.GetMetadataAsync(pieceJointeId);
            if (pieceJointe == null)
                return null;

            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/{pieceJointe.CheminStockage.Replace("\\", "/")}";
        }

        /// <summary>
        /// Supprime un fichier (physique et base de données)
        /// </summary>
        // Dans PieceJointeService.cs, méthode SupprimerFichierAsync

        public async Task<bool> SupprimerFichierAsync(Guid pieceJointeId)
        {
            // 1. Récupérer la pièce jointe via le repository
            var pieceJointe = await _pieceJointeRepository.GetByIdAsync(pieceJointeId);
            if (pieceJointe == null)
                return false;

            // 2. Construire le chemin complet du fichier
            // NOUVEAU: Utiliser ContentRootPath (racine du projet)
            var filePath = Path.Combine(_environment.ContentRootPath, pieceJointe.CheminStockage);

            _logger.LogInformation("Tentative de suppression du fichier: {FilePath}", filePath);

            // 3. Supprimer le fichier physique s'il existe
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Fichier physique supprimé");
            }
            else
            {
                _logger.LogWarning("Fichier non trouvé à l'emplacement: {FilePath}", filePath);
            }

            // 4. Supprimer l'entité via le repository
            await _pieceJointeRepository.DeleteAsync(pieceJointe);
            await _pieceJointeRepository.SaveChangesAsync();

            _logger.LogInformation("Entité supprimée de la base");
            return true;
        }

        /// <summary>
        /// Récupère toutes les pièces jointes d'un commentaire
        /// </summary>
        public async Task<List<PieceJointeDTO>> GetPiecesJointesByCommentaireAsync(Guid commentaireId)
        {
            var pieces = await _pieceJointeRepository.GetByCommentaireIdAsync(commentaireId);

            return pieces.Select(p => new PieceJointeDTO
            {
                Id = p.Id,
                NomFichier = p.NomFichier,
                Taille = p.Taille,
                ContentType = p.ContentType,
                TypePieceJointe = p.TypePieceJointe,
                DateAjout = p.DateAjout,
                Url = GetUrlForPiece(p)
            }).ToList();
        }

        #region Méthodes privées

        /// <summary>
        /// Sauvegarde le fichier physique
        /// </summary>
        private async Task<string> SauvegarderFichierPhysique(CreatePieceJointeDTO dto)
        {
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "pieces-jointes");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.NomFichier}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            if (string.IsNullOrEmpty(dto.ContenuBase64))
                throw new ArgumentException("Aucun fichier fourni");

            // Nettoyer si data:image/...;base64,...
            var base64Data = dto.ContenuBase64.Contains(",")
                ? dto.ContenuBase64.Split(',')[1]
                : dto.ContenuBase64;

            var fileBytes = Convert.FromBase64String(base64Data);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            return Path.Combine("uploads", "pieces-jointes", uniqueFileName);
        }
        /// <summary>
        /// Génère l'URL pour une pièce jointe
        /// </summary>
        private string GetUrlForPiece(PieceJointe piece)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/{piece.CheminStockage.Replace("\\", "/")}";
        }

        public async Task<bool> SupprimerPiecesJointesAsync(List<Guid> pieceJointeIds)
        {
            _logger.LogInformation("Suppression de {Count} pièce(s) jointe(s)", pieceJointeIds.Count);

            bool success = true;

            foreach (var id in pieceJointeIds)
            {
                try
                {
                    var result = await SupprimerFichierAsync(id);
                    if (!result)
                    {
                        _logger.LogWarning("Échec de suppression pour la pièce jointe {Id}", id);
                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression de la pièce jointe {Id}", id);
                    success = false;
                }
            }

            return success;
        }
        #endregion
    }
}