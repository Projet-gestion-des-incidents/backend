//// Fichier: projet0.Application/Services/PieceJointeService.cs
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;  // ← AJOUTER
//using projet0.Application.Commun.DTOs.Ticket;
//using projet0.Application.Interfaces;
//using projet0.Domain.Entities;
//using projet0.Domain.Enums;
//using System;
//using System.IO;
//using System.Threading.Tasks;


//namespace projet0.Application.Services
//{
//    public class PieceJointeService : IPieceJointeService
//    {
//        private readonly IPieceJointeRepository _pieceJointeRepository;
//        private readonly ICommentaireRepository _commentaireRepository;
//        private readonly IWebHostEnvironment _environment;
//        private readonly IHttpContextAccessor _httpContextAccessor;
//        private readonly string _uploadPath = "uploads/pieces-jointes";
//        private readonly ILogger<PieceJointeService> _logger;

//        public PieceJointeService(
//            IPieceJointeRepository pieceJointeRepository,
//            ICommentaireRepository commentaireRepository,
//            IWebHostEnvironment environment,
//            IHttpContextAccessor httpContextAccessor, ILogger<PieceJointeService> logger)

//        {
//            _pieceJointeRepository = pieceJointeRepository;
//            _commentaireRepository = commentaireRepository;
//            _environment = environment;
//            _httpContextAccessor = httpContextAccessor;
//            _logger = logger;

//        }

//        /// <summary>
//        /// Sauvegarde un fichier et ses métadonnées
//        /// </summary>
//        public async Task<PieceJointe> SauvegarderFichierAsync(
//    CreatePieceJointeDTO dto,
//    Guid commentaireId,
//    Guid uploadedById)
//        {
//            // ✅ Utiliser directement dto.Fichier (IFormFile)
//            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "pieces-jointes");
//            if (!Directory.Exists(uploadsFolder))
//                Directory.CreateDirectory(uploadsFolder);

//            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
//            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

//            using (var fileStream = new FileStream(filePath, FileMode.Create))
//            {
//                await dto.Fichier.CopyToAsync(fileStream);
//            }

//            var pieceJointe = new PieceJointe
//            {
//                Id = Guid.NewGuid(),
//                NomFichier = dto.Fichier.FileName,

//                DateAjout = DateTime.UtcNow,
//                CommentaireId = commentaireId,
//                UploadedById = uploadedById
//            };

//            await _pieceJointeRepository.AddAsync(pieceJointe);
//            await _pieceJointeRepository.SaveChangesAsync();

//            return pieceJointe;
//        }
//        /// <summary>
//        /// Récupère l'URL d'un fichier
//        /// </summary>
//        /*public async Task<string> GetUrlFichierAsync(Guid pieceJointeId)
//        {
//            // Utiliser le repository pour récupérer les métadonnées
//            var pieceJointe = await _pieceJointeRepository.GetMetadataAsync(pieceJointeId);
//            if (pieceJointe == null)
//                return null;

//            var request = _httpContextAccessor.HttpContext.Request;
//            var baseUrl = $"{request.Scheme}://{request.Host}";
//            return $"{baseUrl}/{pieceJointe.CheminStockage.Replace("\\", "/")}";
//        }*/

//        /// <summary>
//        /// Supprime un fichier (physique et base de données)
//        /// </summary>
//        // Dans PieceJointeService.cs, méthode SupprimerFichierAsync

//        public async Task<bool> SupprimerFichierAsync(Guid pieceJointeId)
//        {
//            // 1. Récupérer la pièce jointe via le repository
//            var pieceJointe = await _pieceJointeRepository.GetByIdAsync(pieceJointeId);
//            if (pieceJointe == null)
//                return false;

//            // 2. Construire le chemin complet du fichier
//            // NOUVEAU: Utiliser ContentRootPath (racine du projet)
//            //var filePath = Path.Combine(_environment.ContentRootPath, pieceJointe.CheminStockage);

//            _logger.LogInformation("Tentative de suppression du fichier: {FilePath}", filePath);

//            // 3. Supprimer le fichier physique s'il existe
//            /*if (File.Exists(filePath))
//            {
//                File.Delete(filePath);
//                _logger.LogInformation("Fichier physique supprimé");
//            }*/
//            else
//            {
//                _logger.LogWarning("Fichier non trouvé à l'emplacement: {FilePath}", filePath);
//            }

//            // 4. Supprimer l'entité via le repository
//            await _pieceJointeRepository.DeleteAsync(pieceJointe);
//            await _pieceJointeRepository.SaveChangesAsync();

//            _logger.LogInformation("Entité supprimée de la base");
//            return true;
//        }

//        /// <summary>
//        /// Récupère toutes les pièces jointes d'un commentaire
//        /// </summary>
//        public async Task<List<PieceJointeDTO>> GetPiecesJointesByCommentaireAsync(Guid commentaireId)
//        {
//            var pieces = await _pieceJointeRepository.GetByCommentaireIdAsync(commentaireId);

//            return pieces.Select(p => new PieceJointeDTO
//            {
//                Id = p.Id,
//                NomFichier = p.NomFichier,

//                DateAjout = p.DateAjout,
//                Url = GetUrlForPiece(p)
//            }).ToList();
//        }

//        #region Méthodes privées

//        /// <summary>
//        /// Sauvegarde le fichier physique
//        /// </summary>
//        // Fichier: projet0.Application/Services/Ticket/PieceJointeService.cs

//        private async Task<string> SauvegarderFichierPhysique(CreatePieceJointeDTO dto)
//        {
//            // ✅ Vérifier les deux possibilités : Fichier ou ContenuBase64
//            if (dto.Fichier != null && dto.Fichier.Length > 0)
//            {
//                // Cas 1: Upload direct via IFormFile
//                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "pieces-jointes");
//                if (!Directory.Exists(uploadsFolder))
//                    Directory.CreateDirectory(uploadsFolder);

//                var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
//                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

//                using (var fileStream = new FileStream(filePath, FileMode.Create))
//                {
//                    await dto.Fichier.CopyToAsync(fileStream);
//                }

//                _logger.LogInformation("Fichier sauvegardé physiquement via IFormFile: {Chemin}", filePath);
//                return Path.Combine("uploads", "pieces-jointes", uniqueFileName);
//            }
//            else if (!string.IsNullOrEmpty(dto.ContenuBase64))
//            {
//                // Cas 2: Upload via Base64
//                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "pieces-jointes");
//                if (!Directory.Exists(uploadsFolder))
//                    Directory.CreateDirectory(uploadsFolder);

//                var uniqueFileName = $"{Guid.NewGuid()}_{dto.NomFichier}";
//                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

//                // Nettoyer la chaîne base64 (enlever le préfixe data:image/jpeg;base64, etc.)
//                var base64Data = dto.ContenuBase64.Contains(",")
//                    ? dto.ContenuBase64.Split(',')[1]
//                    : dto.ContenuBase64;

//                var fileBytes = Convert.FromBase64String(base64Data);
//                await File.WriteAllBytesAsync(filePath, fileBytes);

//                _logger.LogInformation("Fichier sauvegardé physiquement via Base64: {Chemin}", filePath);
//                return Path.Combine("uploads", "pieces-jointes", uniqueFileName);
//            }
//            else
//            {
//                _logger.LogError("Aucun fichier fourni - Fichier: null, ContenuBase64: {Base64Status}",
//                    string.IsNullOrEmpty(dto.ContenuBase64) ? "vide" : "non vide");
//                throw new ArgumentException("Aucun fichier fourni");
//            }
//        }
//        /// <summary>
//        /// Génère l'URL pour une pièce jointe
//        /// </summary>
//        private string GetUrlForPiece(PieceJointe piece)
//        {
//            var request = _httpContextAccessor.HttpContext.Request;
//            var baseUrl = $"{request.Scheme}://{request.Host}";
//            return $"{baseUrl}/{piece.CheminStockage.Replace("\\", "/")}";
//        }

//        public async Task<bool> SupprimerPiecesJointesAsync(List<Guid> pieceJointeIds)
//        {
//            _logger.LogInformation("Suppression de {Count} pièce(s) jointe(s)", pieceJointeIds.Count);

//            bool success = true;

//            foreach (var id in pieceJointeIds)
//            {
//                try
//                {
//                    var result = await SupprimerFichierAsync(id);
//                    if (!result)
//                    {
//                        _logger.LogWarning("Échec de suppression pour la pièce jointe {Id}", id);
//                        success = false;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Erreur lors de la suppression de la pièce jointe {Id}", id);
//                    success = false;
//                }
//            }

//            return success;
//        }
//        #endregion
//    }
//}