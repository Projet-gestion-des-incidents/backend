using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;


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
            _logger.LogInformation("=== DÉBUT SauvegarderFichierPourCommentaireAsync ===");
            _logger.LogInformation("CommentaireId: {CommentaireId}", commentaireId);
            _logger.LogInformation("UploadedById: {UploadedById}", uploadedById);

            // 1. Vérification du fichier
            if (dto.Fichier == null || dto.Fichier.Length == 0)
            {
                _logger.LogError(" Aucun fichier fourni ou fichier vide");
                throw new ArgumentException("Aucun fichier fourni");
            }

         
            // 2. Création du dossier d'upload
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "commentaires");
            _logger.LogInformation(" Dossier d'upload: {UploadsFolder}", uploadsFolder);

            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation(" Création du dossier: {UploadsFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }
            else
            {
                _logger.LogInformation(" Dossier existe déjà: {UploadsFolder}", uploadsFolder);
            }

            // 3. Génération du nom unique
            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            _logger.LogInformation(" Génération du nom unique:");
            _logger.LogInformation("   - Nom unique: {UniqueFileName}", uniqueFileName);
            _logger.LogInformation("   - Chemin complet: {FilePath}", filePath);

            // 4. Sauvegarde du fichier physique
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    _logger.LogInformation(" Début de la copie du fichier...");
                    await dto.Fichier.CopyToAsync(fileStream);
                    _logger.LogInformation(" Fichier copié avec succès");
                }

                // Vérifier que le fichier a bien été créé
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    _logger.LogInformation(" Fichier physique vérifié: {Size} bytes", fileInfo.Length);
                }
                else
                {
                    _logger.LogError(" Le fichier n'a pas été créé sur le disque !");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la sauvegarde du fichier physique");
                throw;
            }

            // 5. Création de l'entité PieceJointe
            var pieceId = Guid.NewGuid();
            _logger.LogInformation(" Création de l'entité PieceJointe:");
            _logger.LogInformation("   - Id: {PieceId}", pieceId);
            _logger.LogInformation("   - NomFichier (stocké): {NomFichier}", uniqueFileName);
            _logger.LogInformation("   - ContentType: {ContentType}", dto.Fichier.ContentType);
            _logger.LogInformation("   - CommentaireId: {CommentaireId}", commentaireId);
            _logger.LogInformation("   - UploadedById: {UploadedById}", uploadedById);
            _logger.LogInformation("   - DateAjout: {DateAjout}", DateTime.UtcNow);

            var pieceJointe = new PieceJointe
            {
                Id = pieceId,
                NomFichier = uniqueFileName,
                ContentType = dto.Fichier.ContentType,
                DateAjout = DateTime.UtcNow,
                CommentaireId = commentaireId,
                UploadedById = uploadedById
            };

            // 6. Sauvegarde en base de données
            try
            {
                _logger.LogInformation(" Sauvegarde en base de données...");
                await _pieceJointeRepository.AddAsync(pieceJointe);
                _logger.LogInformation(" Entité ajoutée au repository");

                var saveResult = await _pieceJointeRepository.SaveChangesAsync();
                _logger.LogInformation(" SaveChangesAsync terminé: {SaveResult} entité(s) modifiée(s)", saveResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la sauvegarde en base de données");
                throw;
            }

            // 7. Vérification finale
            _logger.LogInformation("=== FIN SauvegarderFichierPourCommentaireAsync ===");
            _logger.LogInformation(" Pièce jointe sauvegardée avec succès:");
            _logger.LogInformation("   - ID: {PieceId}", pieceJointe.Id);
            _logger.LogInformation("   - Nom fichier (base): {NomFichier}", pieceJointe.NomFichier);
            _logger.LogInformation("   - ContentType (base): {ContentType}", pieceJointe.ContentType);
            _logger.LogInformation("   - Chemin physique: {FilePath}", filePath);

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
            _logger.LogInformation("=== DÉBUT SauvegarderFichierPourIncidentAsync ===");
            _logger.LogInformation("IncidentId: {IncidentId}", incidentId);
            _logger.LogInformation("UploadedById: {UploadedById}", uploadedById);

            // 1. Vérification du fichier
            if (dto.Fichier == null || dto.Fichier.Length == 0)
            {
                _logger.LogError(" Aucun fichier fourni ou fichier vide");
                throw new ArgumentException("Aucun fichier fourni");
            }

            _logger.LogInformation(" Fichier reçu:");
            _logger.LogInformation("   - Nom original: {FileName}", dto.Fichier.FileName);
            _logger.LogInformation("   - Taille: {Length} bytes", dto.Fichier.Length);
            _logger.LogInformation("   - ContentType: {ContentType}", dto.Fichier.ContentType);

            // 2. Création du dossier d'upload
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "incidents");
            _logger.LogInformation(" Dossier d'upload: {UploadsFolder}", uploadsFolder);

            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation(" Création du dossier: {UploadsFolder}", uploadsFolder);
                Directory.CreateDirectory(uploadsFolder);
            }
            else
            {
                _logger.LogInformation(" Dossier existe déjà: {UploadsFolder}", uploadsFolder);
            }

            // 3. Génération du nom unique
            var uniqueFileName = $"{Guid.NewGuid()}_{dto.Fichier.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            _logger.LogInformation(" Génération du nom unique:");
            _logger.LogInformation("   - Nom unique: {UniqueFileName}", uniqueFileName);
            _logger.LogInformation("   - Chemin complet: {FilePath}", filePath);

            // 4. Sauvegarde du fichier physique
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    _logger.LogInformation(" Début de la copie du fichier...");
                    await dto.Fichier.CopyToAsync(fileStream);
                    _logger.LogInformation(" Fichier copié avec succès");
                }

                // Vérifier que le fichier a bien été créé
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    _logger.LogInformation(" Fichier physique vérifié: {Size} bytes", fileInfo.Length);
                }
                else
                {
                    _logger.LogError(" Le fichier n'a pas été créé sur le disque !");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la sauvegarde du fichier physique");
                throw;
            }

            // 5. Création de l'entité PieceJointe
            var pieceId = Guid.NewGuid();
            _logger.LogInformation(" Création de l'entité PieceJointe:");
            _logger.LogInformation("   - Id: {PieceId}", pieceId);
            _logger.LogInformation("   - NomFichier (stocké): {NomFichier}", uniqueFileName);  // STOCKER LE NOM UNIQUE
            _logger.LogInformation("   - ContentType: {ContentType}", dto.Fichier.ContentType);
            _logger.LogInformation("   - IncidentId: {IncidentId}", incidentId);
            _logger.LogInformation("   - UploadedById: {UploadedById}", uploadedById);
            _logger.LogInformation("   - DateAjout: {DateAjout}", DateTime.UtcNow);

            var pieceJointe = new PieceJointe
            {
                Id = pieceId,
                NomFichier = uniqueFileName, 
                ContentType = dto.Fichier.ContentType,
                DateAjout = DateTime.UtcNow,
                IncidentId = incidentId,
                UploadedById = uploadedById
            };

            // 6. Sauvegarde en base de données
            try
            {
                _logger.LogInformation(" Sauvegarde en base de données...");
                await _pieceJointeRepository.AddAsync(pieceJointe);
                _logger.LogInformation(" Entité ajoutée au repository");

                var saveResult = await _pieceJointeRepository.SaveChangesAsync();
                _logger.LogInformation(" SaveChangesAsync terminé: {SaveResult} entité(s) modifiée(s)", saveResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Erreur lors de la sauvegarde en base de données");
                throw;
            }

            // 7. Vérification finale
            _logger.LogInformation("=== FIN SauvegarderFichierPourIncidentAsync ===");
            _logger.LogInformation(" Pièce jointe sauvegardée avec succès:");
            _logger.LogInformation("   - ID: {PieceId}", pieceJointe.Id);
            _logger.LogInformation("   - Nom fichier (base): {NomFichier}", pieceJointe.NomFichier);
            _logger.LogInformation("   - ContentType (base): {ContentType}", pieceJointe.ContentType);
            _logger.LogInformation("   - Chemin physique: {FilePath}", filePath);

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
                ContentType = p.ContentType,
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

        private string GetUrlForPiece(PieceJointe piece)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/api/pieces-jointes/{piece.Id}";
        }
        public async Task<PieceJointe> GetMetadataAsync(Guid pieceJointeId)
        {
            return await _pieceJointeRepository.GetMetadataAsync(pieceJointeId);
        }
        #endregion
    }
}