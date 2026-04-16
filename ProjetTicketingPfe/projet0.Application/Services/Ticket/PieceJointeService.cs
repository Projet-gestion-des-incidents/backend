using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; 
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
        private readonly string _uploadPath = "uploads/incidents";
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
            Guid incidentId,
            Guid uploadedById)
        {
            if (dto.Fichier == null || dto.Fichier.Length == 0)
                throw new ArgumentException("Aucun fichier fourni");

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, _uploadPath);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Générer un nom unique pour éviter les conflits
            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Sauvegarder le fichier
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Fichier.CopyToAsync(fileStream);
            }

            // Créer l'entité
            var pieceJointe = new PieceJointe
            {
                Id = Guid.NewGuid(),
                NomFichier = dto.Fichier.FileName,
                ContentType = dto.Fichier.ContentType,
                DateAjout = DateTime.UtcNow,
                IncidentId = incidentId,
                UploadedById = uploadedById
            };

            await _pieceJointeRepository.AddAsync(pieceJointe);
            await _pieceJointeRepository.SaveChangesAsync();

            return pieceJointe;
        }

        /// <summary>
        /// Supprime un fichier (physique et base de données)
        /// </summary>

        public async Task<bool> SupprimerFichierAsync(Guid pieceJointeId)
        {
            var pieceJointe = await _pieceJointeRepository.GetByIdAsync(pieceJointeId);
            if (pieceJointe == null)
                return false;

            var filePath = Path.Combine(_environment.ContentRootPath, _uploadPath, pieceJointe.NomFichier);

            _logger.LogInformation("Tentative de suppression du fichier: {FilePath}", filePath);

            // Supprimer le fichier physique s'il existe
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Fichier physique supprimé");
            }
            else
            {
                _logger.LogWarning("Fichier non trouvé à l'emplacement: {FilePath}", filePath);
            }

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

                DateAjout = p.DateAjout,
                Url = GetUrlForPiece(p)
            }).ToList();
        }

        #region Méthodes privées
        
        /// <summary>
        /// Génère l'URL pour une pièce jointe
        /// </summary>        

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

        public async Task<string> GetUrlFichierAsync(Guid pieceJointeId)
        {
            var pieceJointe = await _pieceJointeRepository.GetMetadataAsync(pieceJointeId);
            if (pieceJointe == null)
                return null;

            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/uploads/pieces-jointes/{pieceJointe.NomFichier}";
        }

        public async Task<PieceJointe> SauvegarderFichierPourCommentaireAsync(
    CreatePieceJointeDTO dto,
    Guid commentaireId,
    Guid uploadedById)
        {
            if (dto.Fichier == null || dto.Fichier.Length == 0)
                throw new ArgumentException("Aucun fichier fourni");

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "commentaires");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Fichier.CopyToAsync(fileStream);
            }

            var pieceJointe = new PieceJointe
            {
                Id = Guid.NewGuid(),
                NomFichier = dto.NomFichier,
                DateAjout = DateTime.UtcNow,
                CommentaireId = commentaireId,
                UploadedById = uploadedById
            };

            await _pieceJointeRepository.AddAsync(pieceJointe);
            await _pieceJointeRepository.SaveChangesAsync();

            return pieceJointe;
        }

        /// <summary>
        /// Sauvegarde un fichier pour un incident
        /// </summary>
        public async Task<PieceJointe> SauvegarderFichierPourIncidentAsync(
            CreatePieceJointeDTO dto,
            Guid incidentId,
            Guid uploadedById)
        {
            if (dto.Fichier == null || dto.Fichier.Length == 0)
                throw new ArgumentException("Aucun fichier fourni");

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "incidents");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Fichier.CopyToAsync(fileStream);
            }

            var pieceJointe = new PieceJointe
            {
                Id = Guid.NewGuid(),
                NomFichier = dto.Fichier.FileName,
                ContentType = dto.Fichier.ContentType,  // ✅ AJOUTER CETTE LIGNE

                DateAjout = DateTime.UtcNow,
                IncidentId = incidentId,
                UploadedById = uploadedById
            };

            await _pieceJointeRepository.AddAsync(pieceJointe);
            await _pieceJointeRepository.SaveChangesAsync();

            return pieceJointe;
        }

        /// <summary>
        /// Récupère toutes les pièces jointes d'un incident
        /// </summary>
        public async Task<List<PieceJointeDTO>> GetPiecesJointesByIncidentIdAsync(Guid incidentId)
        {
            var pieces = await _pieceJointeRepository.GetByIncidentIdAsync(incidentId);

            return pieces.Select(p => new PieceJointeDTO
            {
                Id = p.Id,
                NomFichier = p.NomFichier,
                DateAjout = p.DateAjout,
                Url = GetUrlForPiece(p)
            }).ToList();
        }

        /// <summary>
        /// Supprime plusieurs pièces jointes d'un incident
        /// </summary>
        public async Task<bool> SupprimerPiecesJointesIncidentAsync(List<Guid> pieceJointeIds)
        {
            _logger.LogInformation("Suppression de {Count} pièce(s) jointe(s) d'incident", pieceJointeIds.Count);

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

        // Méthode helper pour l'URL
        private string GetUrlForPiece(PieceJointe piece)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/api/pieces-jointes/{piece.Id}";
        }
        // Dans PieceJointeService.cs
        public async Task<PieceJointe> GetMetadataAsync(Guid pieceJointeId)
        {
            return await _pieceJointeRepository.GetMetadataAsync(pieceJointeId);
        }
        #endregion
    }
}