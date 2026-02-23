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
        private readonly IPieceJointeRepository _pieceJointeRepository;  // ← Plus de DbContext
        private readonly ICommentaireRepository _commentaireRepository;    // ← Pour vérifier le commentaire
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _uploadPath = "uploads/pieces-jointes";
        private readonly ILogger<PieceJointeService> _logger;


        public PieceJointeService(
            IPieceJointeRepository pieceJointeRepository,  // ← Injection du repository
            ICommentaireRepository commentaireRepository,   // ← Pour vérifications
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor, ILogger<PieceJointeService> logger)  // ← AJOUTER CE PARAMÈTRE

        {
            _pieceJointeRepository = pieceJointeRepository;
            _commentaireRepository = commentaireRepository;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;  // ← INITIALISER _logger

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
        public async Task<bool> SupprimerFichierAsync(Guid pieceJointeId)
        {
            // 1. Récupérer la pièce jointe via le repository
            var pieceJointe = await _pieceJointeRepository.GetByIdAsync(pieceJointeId);
            if (pieceJointe == null)
                return false;

            // 2. Supprimer le fichier physique
            var filePath = Path.Combine(_environment.WebRootPath, pieceJointe.CheminStockage);
            if (File.Exists(filePath))
                File.Delete(filePath);

            // 3. Supprimer l'entité via le repository
            await _pieceJointeRepository.DeleteAsync(pieceJointe);
            await _pieceJointeRepository.SaveChangesAsync();

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
            _logger.LogInformation("Dossier upload: {UploadFolder}", uploadsFolder);

            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation("Création du dossier {UploadFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.NomFichier}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            _logger.LogInformation("Chemin complet du fichier: {FilePath}", filePath);

            // Cas 1: Fichier uploadé via IFormFile
            if (dto.Fichier != null && dto.Fichier.Length > 0)
            {
                _logger.LogInformation("Sauvegarde via IFormFile, taille: {Taille}", dto.Fichier.Length);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Fichier.CopyToAsync(fileStream);
                }
            }
            // Cas 2: Fichier encodé en base64
            else if (!string.IsNullOrEmpty(dto.ContenuBase64))
            {
                _logger.LogInformation("Sauvegarde via Base64");
                var base64Data = dto.ContenuBase64.Contains(",")
                    ? dto.ContenuBase64.Split(',')[1]
                    : dto.ContenuBase64;
                var fileBytes = Convert.FromBase64String(base64Data);
                await File.WriteAllBytesAsync(filePath, fileBytes);
            }
            else
            {
                _logger.LogError("Aucun fichier fourni");
                throw new ArgumentException("Aucun fichier fourni");
            }

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

        #endregion
    }
}