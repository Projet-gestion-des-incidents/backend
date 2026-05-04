using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.TPE;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using TpeEntity = projet0.Domain.Entities.TPE;

namespace projet0.Application.Services.TPEService
{
    public class TPEService : ITPEService
    {
        private readonly ITPERepository _tpeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IIncidentTPERepository _incidentTPERepository;
        private readonly INotificationService _notificationService;  // Ajouter
        private readonly ILogger<TPEService> _logger;

        public TPEService(
            ITPERepository tpeRepository,
            IUserRepository userRepository,
            IIncidentTPERepository incidentTPERepository,
            INotificationService notificationService,  // Ajouter
            ILogger<TPEService> logger)
        {
            _tpeRepository = tpeRepository;
            _userRepository = userRepository;

            _incidentTPERepository = incidentTPERepository;
            _notificationService = notificationService;  // Ajouter
            _logger = logger;
        }

        private async Task<T> MeasureAsync<T>(string actionName, object input, Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("START {Action} | Input = {@Input}", actionName, input);

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR {Action} | Input = {@Input}", actionName, input);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogDebug("END {Action} | {Elapsed} ms", actionName, sw.ElapsedMilliseconds);
            }
        }

        public async Task<ApiResponse<TPEDto>> CreateAsync(CreateTPEDto dto, Guid userId)
        {
            return await MeasureAsync("CreateTPE", dto, async () =>
            {
                // 1. Vérifier que l'utilisateur existe
                var commercant = await _userRepository.GetByIdAsync(dto.CommercantId);
                if (commercant == null)
                {
                    _logger.LogWarning("User not found | UserId = {UserId}", dto.CommercantId);
                    return ApiResponse<TPEDto>.Failure(
                        message: "L'utilisateur spécifié n'existe pas",
                        resultCode: 40
                    );
                }

                // 2. Vérifier que l'utilisateur a le rôle "Commercant"
                var roles = await _userRepository.GetUserRolesAsync(dto.CommercantId);
                if (!roles.Contains("Commercant"))
                {
                    _logger.LogWarning("User is not a commercant | UserId = {UserId}, Roles: {@Roles}",
                        dto.CommercantId, roles);
                    return ApiResponse<TPEDto>.Failure(
                        message: "Seuls les utilisateurs avec le rôle 'Commerçant' peuvent avoir des TPEs",
                        resultCode: 45
                    );
                }

                // 3. Générer le numéro de série automatiquement
                var numSerie = await _tpeRepository.GenerateNumSerieAsync(dto.Modele);

                _logger.LogInformation("Numéro de série généré pour le modèle {Modele}: {NumSerie}",
                    dto.Modele, numSerie);

                // 4. Générer le numéro de série complet avec abréviation
                var abbreviation = ModeleTPEHelper.GetAbbreviation(dto.Modele);
                var numSerieComplet = $"{abbreviation}-{numSerie}";

                // 5. Récupérer l'utilisateur connecté (admin)
                var admin = await _userRepository.GetByIdAsync(userId);

                // 6. Créer le TPE
                var tpe = new TpeEntity
                {
                    Id = Guid.NewGuid(),
                    NumSerie = numSerie,
                    NumSerieComplet = numSerieComplet,
                    Modele = dto.Modele,
                    CommercantId = dto.CommercantId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = userId,
                    UpdatedAt = null,
                    UpdatedById = null
                };

                await _tpeRepository.AddAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                // ======================================================
                // 🔔 NOTIFICATION POUR LE COMMERCANT PROPRIÉTAIRE
                // ======================================================

                // Notification au commerçant propriétaire
                await _notificationService.CreateTPENotificationAsync(
                    dto.CommercantId,
                    tpe.Id,
                    TypeNotification.TPECree,
                    $"Nouveau TPE ajouté : {numSerieComplet}",
                    $"Un nouveau TPE de modèle '{dto.Modele}' (Série: {numSerieComplet}) a été ajouté à votre compte par l'administrateur {admin?.Nom} {admin?.Prenom}."
                );

                _logger.LogInformation("Notification envoyée au commerçant {CommercantId} pour le TPE {TPEId}",
                    dto.CommercantId, tpe.Id);

                // 7. Mapper vers DTO
                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
                    CreatedAt = tpe.CreatedAt,
                    CreatedByNom = admin != null ? $"{admin.Nom} {admin.Prenom}" : "Inconnu",
                    UpdatedAt = null,
                    UpdatedByNom = null
                };

                _logger.LogInformation(
                    "TPE created successfully | Id: {Id} | NumSerieComplet: {NumSerieComplet} | Commercant: {Commercant}",
                    tpe.Id, tpe.NumSerieComplet, commercant.Email
                );

                return ApiResponse<TPEDto>.Success(
                    data: tpeDto,
                    message: $"TPE créé avec succès. Numéro complet: {numSerieComplet}",
                    resultCode: 0
                );
            });
        }

        public async Task<ApiResponse<TPEDto>> UpdateAsync(Guid id, UpdateTPEDto dto, Guid userId)
        {
            return await MeasureAsync("UpdateTPE", new { id, dto }, async () =>
            {
                var tpe = await _tpeRepository.GetByIdAsync(id);
                if (tpe == null)
                {
                    return ApiResponse<TPEDto>.Failure("TPE non trouvé", resultCode: 42);
                }

                // Récupérer les informations de l'admin pour l'audit
                var admin = await _userRepository.GetByIdAsync(userId);
                var createdByUser = tpe.CreatedById.HasValue ? await _userRepository.GetByIdAsync(tpe.CreatedById.Value) : null;

                // Gérer le changement de modèle
                bool modeleChanged = tpe.Modele != dto.Modele;

                if (modeleChanged)
                {
                    var newNumSerie = await _tpeRepository.GenerateNumSerieAsync(dto.Modele);
                    var abbreviation = ModeleTPEHelper.GetAbbreviation(dto.Modele);

                    tpe.NumSerie = newNumSerie;
                    tpe.NumSerieComplet = $"{abbreviation}-{newNumSerie}";
                    tpe.Modele = dto.Modele;
                }

                // Gérer le changement de commerçant (peut être null)
                if (dto.CommercantId.HasValue)
                {
                    var nouveauCommercant = await _userRepository.GetByIdAsync(dto.CommercantId.Value);
                    if (nouveauCommercant == null)
                    {
                        return ApiResponse<TPEDto>.Failure("Le nouveau commerçant spécifié n'existe pas", resultCode: 40);
                    }

                    var roles = await _userRepository.GetUserRolesAsync(dto.CommercantId.Value);
                    if (!roles.Contains("Commercant"))
                    {
                        return ApiResponse<TPEDto>.Failure("Le nouveau propriétaire doit avoir le rôle 'Commerçant'", resultCode: 45);
                    }

                    tpe.CommercantId = dto.CommercantId.Value;
                }
                else
                {
                    tpe.CommercantId = null;
                }

                // Mettre à jour les champs d'audit
                tpe.UpdatedAt = DateTime.UtcNow;
                tpe.UpdatedById = userId;

                await _tpeRepository.UpdateAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                // Préparer la réponse
                ApplicationUser commercant = null;
                if (tpe.CommercantId.HasValue)
                {
                    commercant = await _userRepository.GetByIdAsync(tpe.CommercantId.Value);
                }

                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "Non assigné",
                    CreatedAt = tpe.CreatedAt,
                    CreatedByNom = createdByUser != null ? $"{createdByUser.Nom} {createdByUser.Prenom}" : "Inconnu",
                    UpdatedAt = tpe.UpdatedAt,
                    UpdatedByNom = admin != null ? $"{admin.Nom} {admin.Prenom}" : null
                };

                // Construire le message de succès
                string message = "TPE mis à jour avec succès";
                if (modeleChanged)
                {
                    message = $"TPE mis à jour avec succès. Modèle changé, nouveau numéro: {tpe.NumSerieComplet}";
                }

                _logger.LogInformation("TPE updated | Id: {Id} | New NumSerieComplet: {NumSerieComplet} | New Commercant: {CommercantId}",
                    tpe.Id, tpe.NumSerieComplet, tpe.CommercantId);

                return ApiResponse<TPEDto>.Success(
                    data: tpeDto,
                    message: message,
                    resultCode: 0
                );
            });

        }

        public async Task<ApiResponse<string>> DeleteAsync(Guid id)
        {
            return await MeasureAsync("DeleteTPE", new { id }, async () =>
            {
                var tpe = await _tpeRepository.GetByIdAsync(id);
                if (tpe == null)
                {
                    _logger.LogWarning("TPE not found | Id = {Id}", id);
                    return ApiResponse<string>.Failure(
                        message: "TPE non trouvé",
                        resultCode: 42
                    );
                }

                await _tpeRepository.DeleteAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                _logger.LogInformation("TPE deleted | Id: {Id} | NumSerie: {NumSerie}", id, tpe.NumSerie);

                return ApiResponse<string>.Success(
                    message: "TPE supprimé avec succès",
                    resultCode: 0
                );
            });
        }

        public async Task<ApiResponse<TPEDto>> GetByIdAsync(Guid id)
        {
            return await MeasureAsync("GetTPEById", new { id }, async () =>
            {
                var tpe = await _tpeRepository.GetByIdAsync(id);
                if (tpe == null)
                {
                    return ApiResponse<TPEDto>.Failure(
                        message: "TPE non trouvé",
                        resultCode: 42
                    );
                }

                ApplicationUser commercant = null;
                if (tpe.CommercantId.HasValue)
                {
                    commercant = await _userRepository.GetByIdAsync(tpe.CommercantId.Value);
                }

                ApplicationUser createdBy = null;
                if (tpe.CreatedById.HasValue)
                {
                    createdBy = await _userRepository.GetByIdAsync(tpe.CreatedById.Value);
                }

                ApplicationUser updatedBy = null;
                if (tpe.UpdatedById.HasValue)
                {
                    updatedBy = await _userRepository.GetByIdAsync(tpe.UpdatedById.Value);
                }

                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "Non assigné",
                    CreatedAt = tpe.CreatedAt,
                    CreatedByNom = createdBy != null ? $"{createdBy.Nom} {createdBy.Prenom}" : "Inconnu",
                    UpdatedAt = tpe.UpdatedAt,
                    UpdatedByNom = updatedBy != null ? $"{updatedBy.Nom} {updatedBy.Prenom}" : null
                };

                return ApiResponse<TPEDto>.Success(
                    data: tpeDto,
                    message: "TPE récupéré avec succès",
                    resultCode: 0
                );
            });
        }

        public async Task<ApiResponse<IEnumerable<TPEDto>>> GetByCommercantIdAsync(Guid commercantId)
        {
            return await MeasureAsync("GetTPEByCommercant", new { commercantId }, async () =>
            {
                var commercant = await _userRepository.GetByIdAsync(commercantId);
                if (commercant == null)
                {
                    return ApiResponse<IEnumerable<TPEDto>>.Failure(
                        message: "Commerçant non trouvé",
                        resultCode: 40
                    );
                }

                var tpes = await _tpeRepository.GetByCommercantIdAsync(commercantId);
                var tpeDtos = new List<TPEDto>();

                foreach (var tpe in tpes)
                {
                    // Récupérer les infos de création et modification
                    ApplicationUser createdBy = null;
                    if (tpe.CreatedById.HasValue)
                    {
                        createdBy = await _userRepository.GetByIdAsync(tpe.CreatedById.Value);
                    }

                    ApplicationUser updatedBy = null;
                    if (tpe.UpdatedById.HasValue)
                    {
                        updatedBy = await _userRepository.GetByIdAsync(tpe.UpdatedById.Value);
                    }

                    tpeDtos.Add(new TPEDto
                    {
                        Id = tpe.Id,
                        NumSerie = tpe.NumSerie,
                        NumSerieComplet = tpe.NumSerieComplet,
                        Modele = tpe.Modele,
                        CommercantId = tpe.CommercantId,
                        CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
                        // ✅ AJOUTER LES CHAMPS D'AUDIT
                        CreatedAt = tpe.CreatedAt,
                        CreatedByNom = createdBy != null ? $"{createdBy.Nom} {createdBy.Prenom}" : "Inconnu",
                        UpdatedAt = tpe.UpdatedAt,
                        UpdatedByNom = updatedBy != null ? $"{updatedBy.Nom} {updatedBy.Prenom}" : null
                    });
                }

                return ApiResponse<IEnumerable<TPEDto>>.Success(
                    data: tpeDtos,
                    message: $"{tpeDtos.Count} TPE(s) trouvé(s)",
                    resultCode: 0
                );
            });
        }

        public async Task<ApiResponse<IEnumerable<TPEDto>>> GetAllAsync()
        {
            return await MeasureAsync("GetAllTPEs", null, async () =>
            {
                var tpes = await _tpeRepository.GetAllAsync();
                var tpeDtos = new List<TPEDto>();

                foreach (var tpe in tpes)
                {
                    ApplicationUser commercant = null;
                    if (tpe.CommercantId.HasValue)
                    {
                        commercant = await _userRepository.GetByIdAsync(tpe.CommercantId.Value);
                    }

                    // Récupérer les infos de création et modification
                    ApplicationUser createdBy = null;
                    if (tpe.CreatedById.HasValue)
                    {
                        createdBy = await _userRepository.GetByIdAsync(tpe.CreatedById.Value);
                    }

                    ApplicationUser updatedBy = null;
                    if (tpe.UpdatedById.HasValue)
                    {
                        updatedBy = await _userRepository.GetByIdAsync(tpe.UpdatedById.Value);
                    }

                    tpeDtos.Add(new TPEDto
                    {
                        Id = tpe.Id,
                        NumSerie = tpe.NumSerie,
                        NumSerieComplet = tpe.NumSerieComplet,
                        Modele = tpe.Modele,
                        CommercantId = tpe.CommercantId,
                        CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "Non assigné",
                        CreatedAt = tpe.CreatedAt,
                        CreatedByNom = createdBy != null ? $"{createdBy.Nom} {createdBy.Prenom}" : "Inconnu",
                        UpdatedAt = tpe.UpdatedAt,
                        UpdatedByNom = updatedBy != null ? $"{updatedBy.Nom} {updatedBy.Prenom}" : null
                    });
                }

                return ApiResponse<IEnumerable<TPEDto>>.Success(
                    data: tpeDtos,
                    message: $"{tpeDtos.Count} TPE(s) trouvé(s)",
                    resultCode: 0
                );
            });
        }

        // MÉTHODE PAGINÉE
        // MÉTHODE PAGINÉE CORRIGÉE
        public async Task<ApiResponse<PagedResult<TPEDto>>> GetTPEsPagedAsync(TPEPagedRequest request)
        {
            return await MeasureAsync(nameof(GetTPEsPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogInformation("Récupération paginée des TPEs - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, Modele: {Modele}",
                        request.Page, request.PageSize, request.SearchTerm, request.Modele);

                    // 1. Récupérer la requête avec les relations
                    var query = await _tpeRepository.QueryWithDetailsAsync();

                    // 2. Appliquer les filtres
                    if (request.Modele.HasValue)
                    {
                        query = query.Where(t => t.Modele == request.Modele.Value);
                        _logger.LogInformation("Filtre appliqué: Modele = {Modele}", request.Modele.Value);
                    }

                    if (request.CommercantId.HasValue)
                    {
                        query = query.Where(t => t.CommercantId == request.CommercantId.Value);
                        _logger.LogInformation("Filtre appliqué: CommercantId = {CommercantId}", request.CommercantId.Value);
                    }

                    // ✅ CORRECTION: Filtre par date de création exacte (pas Debut/Fin)
                    if (request.CreatedAt.HasValue)
                    {
                        var date = request.CreatedAt.Value.Date;
                        var nextDay = date.AddDays(1);
                        query = query.Where(t => t.CreatedAt >= date && t.CreatedAt < nextDay);
                        _logger.LogInformation("Filtre appliqué: CreatedAt = {CreatedAt}", request.CreatedAt.Value);
                    }

                    // ✅ CORRECTION: Filtre par date de modification exacte
                    if (request.UpdatedAt.HasValue)
                    {
                        var date = request.UpdatedAt.Value.Date;
                        var nextDay = date.AddDays(1);
                        query = query.Where(t => t.UpdatedAt >= date && t.UpdatedAt < nextDay);
                        _logger.LogInformation("Filtre appliqué: UpdatedAt = {UpdatedAt}", request.UpdatedAt.Value);
                    }

                    // ✅ NOUVEAU: Filtre par créateur
                    if (request.CreatedById.HasValue)
                    {
                        query = query.Where(t => t.CreatedById == request.CreatedById.Value);
                        _logger.LogInformation("Filtre appliqué: CreatedById = {CreatedById}", request.CreatedById.Value);
                    }

                    // ✅ NOUVEAU: Filtre par modificateur
                    if (request.UpdatedById.HasValue)
                    {
                        query = query.Where(t => t.UpdatedById == request.UpdatedById.Value);
                        _logger.LogInformation("Filtre appliqué: UpdatedById = {UpdatedById}", request.UpdatedById.Value);
                    }

                    // ✅ CORRECTION: Recherche simplifiée (sans SearchUsersByTermAsync)
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        
                        var term = request.SearchTerm.ToLower();
                        query = query.Where(t =>
                            t.NumSerie.ToLower().Contains(term) ||
                            t.NumSerieComplet.ToLower().Contains(term) ||
                            t.Modele.ToString().ToLower().Contains(term) ||
                            (t.Commercant != null &&
                                (t.Commercant.Nom.ToLower().Contains(term) ||
                                 t.Commercant.Prenom.ToLower().Contains(term) ||
                                 t.Commercant.UserName.ToLower().Contains(term) ||  // ✅ AJOUTER
                                 (t.Commercant.Nom + " " + t.Commercant.Prenom).ToLower().Contains(term) ||
                                 (t.Commercant.Prenom + " " + t.Commercant.Nom).ToLower().Contains(term))) ||
                            (t.CreatedBy != null &&
                                (t.CreatedBy.Nom.ToLower().Contains(term) ||
                                 t.CreatedBy.Prenom.ToLower().Contains(term) ||
                                 t.CreatedBy.UserName.ToLower().Contains(term) ||  // ✅ AJOUTER
                                 (t.CreatedBy.Nom + " " + t.CreatedBy.Prenom).ToLower().Contains(term))) ||
                            (t.UpdatedBy != null &&
                                (t.UpdatedBy.Nom.ToLower().Contains(term) ||
                                 t.UpdatedBy.Prenom.ToLower().Contains(term) ||
                                 t.UpdatedBy.UserName.ToLower().Contains(term) ||  // ✅ AJOUTER
                                 (t.UpdatedBy.Nom + " " + t.UpdatedBy.Prenom).ToLower().Contains(term)))
                        );
                        _logger.LogInformation("Filtre appliqué: SearchTerm = {SearchTerm}", request.SearchTerm);
                    }

                    // 3. Compter le total AVANT pagination
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total TPEs trouvés: {TotalCount}", totalCount);

                    // 4. Appliquer le tri
                    query = ApplySortingToQuery(query, request.SortBy, request.SortDescending);

                    // 5. Appliquer la pagination
                    var items = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    _logger.LogInformation("{Count} TPEs récupérés pour la page {Page}", items.Count, request.Page);

                    // 6. Mapper vers DTO
                    var dtos = new List<TPEDto>();
                    foreach (var tpe in items)
                    {
                        ApplicationUser createdBy = null;
                        if (tpe.CreatedById.HasValue)
                        {
                            createdBy = await _userRepository.GetByIdAsync(tpe.CreatedById.Value);
                        }

                        ApplicationUser updatedBy = null;
                        if (tpe.UpdatedById.HasValue)
                        {
                            updatedBy = await _userRepository.GetByIdAsync(tpe.UpdatedById.Value);
                        }

                        dtos.Add(new TPEDto
                        {
                            Id = tpe.Id,
                            NumSerie = tpe.NumSerie,
                            NumSerieComplet = tpe.NumSerieComplet,
                            Modele = tpe.Modele,
                            CommercantId = tpe.CommercantId,
                            CommercantNom = tpe.Commercant != null ? $"{tpe.Commercant.Nom} {tpe.Commercant.Prenom}" : "Non assigné",
                            CreatedAt = tpe.CreatedAt,
                            CreatedByNom = createdBy != null ? $"{createdBy.Nom} {createdBy.Prenom}" : "Inconnu",
                            UpdatedAt = tpe.UpdatedAt,
                            UpdatedByNom = updatedBy != null ? $"{updatedBy.Nom} {updatedBy.Prenom}" : null
                        });
                    }

                    // 7. Créer le résultat paginé
                    var pagedResult = new PagedResult<TPEDto>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };

                    return ApiResponse<PagedResult<TPEDto>>.Success(pagedResult,
                        $"{dtos.Count} TPE(s) trouvé(s) sur {totalCount}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des TPEs");
                    return ApiResponse<PagedResult<TPEDto>>.Failure("Erreur interne du serveur");
                }
            });
        }

        private IQueryable<TpeEntity> ApplySortingToQuery(IQueryable<TpeEntity> query, string? sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "NumSerieComplet";

            var sortByLower = sortBy.ToLower();

            return (sortByLower, descending) switch
            {
                ("numserie", false) => query.OrderBy(t => t.NumSerie),
                ("numserie", true) => query.OrderByDescending(t => t.NumSerie),

                ("numseriecomplet", false) => query.OrderBy(t => t.NumSerieComplet),
                ("numseriecomplet", true) => query.OrderByDescending(t => t.NumSerieComplet),

                ("modele", false) => query.OrderBy(t => t.Modele),
                ("modele", true) => query.OrderByDescending(t => t.Modele),

                ("commercant", false) => query.OrderBy(t => t.Commercant.Nom).ThenBy(t => t.Commercant.Prenom),
                ("commercant", true) => query.OrderByDescending(t => t.Commercant.Nom).ThenByDescending(t => t.Commercant.Prenom),

                _ => query.OrderBy(t => t.NumSerieComplet)
            };
        }


        // ============================================
        // DASHBOARD TPE
        // ============================================

        public async Task<ApiResponse<TPEDashboardDTO>> GetTPEDashboardAsync()
        {
            return await MeasureAsync(nameof(GetTPEDashboardAsync), null, async () =>
            {
                try
                {
                    _logger.LogInformation("Récupération du dashboard TPE");

                    // 1. Récupérer tous les TPEs avec leurs commerçants
                    var tpes = await _tpeRepository.GetAllAsync();
                    var tpesList = tpes.ToList();

                    // 2. Récupérer tous les incidents liés aux TPEs
                    // Pour cela, on a besoin des liaisons IncidentTPE
                    var incidentTPEs = await GetIncidentTPEsAsync();

                    // 3. Compter le nombre d'incidents par TPE
                    var incidentsParTPE = incidentTPEs
                        .GroupBy(it => it.TPEId)
                        .ToDictionary(g => g.Key, g => g.Count());

                    // ============================================
                    // STATISTIQUES GLOBALES
                    // ============================================
                    var totalTPEs = tpesList.Count;
                    var totalIncidents = incidentTPEs.Count;
                    var tauxGlobalPanne = totalTPEs > 0
                        ? Math.Round((double)totalIncidents / totalTPEs * 100, 1)
                        : 0;

                    var overview = new TPEDashboardOverviewDTO
                    {
                        TotalTPEs = totalTPEs,
                        TotalIncidentsLiees = totalIncidents,
                        TauxGlobalPanne = tauxGlobalPanne
                    };

                    // ============================================
                    // TAUX DE PANNE PAR MODÈLE
                    // ============================================
                    var pannesParModele = new List<TPEPanneParModeleDTO>();
                    var couleursParModele = new Dictionary<ModeleTPE, string>
            {
                { ModeleTPE.Ingenico, "#17a2b8" },  // Bleu
                { ModeleTPE.Verifone, "#28a745" },  // Vert
                { ModeleTPE.PAX, "#ffc107" }        // Jaune
            };

                    foreach (ModeleTPE modele in Enum.GetValues(typeof(ModeleTPE)))
                    {
                        var tpesModele = tpesList.Where(t => t.Modele == modele).ToList();
                        var nombreTPEsModele = tpesModele.Count;

                        // Compter les incidents pour ce modèle
                        var tpeIdsModele = tpesModele.Select(t => t.Id).ToList();
                        var incidentsModele = incidentTPEs.Count(it => tpeIdsModele.Contains(it.TPEId));

                        var tauxPanne = nombreTPEsModele > 0
                            ? Math.Round((double)incidentsModele / nombreTPEsModele * 100, 1)
                            : 0;

                        pannesParModele.Add(new TPEPanneParModeleDTO
                        {
                            Modele = modele.ToString(),
                            NombreTPEs = nombreTPEsModele,
                            NombreIncidents = incidentsModele,
                            TauxPanne = tauxPanne,
                            Color = couleursParModele[modele]
                        });
                    }

                    // Trier par taux de panne (décroissant)
                    pannesParModele = pannesParModele
                        .OrderByDescending(m => m.TauxPanne)
                        .ToList();

                    // ============================================
                    // TAUX DE PANNE PAR ADRESSE (via commerçant)
                    // ============================================
                    var pannesParAdresse = new List<TPEPanneParAdresseDTO>();

                    // Grouper les TPEs par commerçant
                    var tpesParCommercant = tpesList
                        .Where(t => t.CommercantId.HasValue)
                        .GroupBy(t => t.CommercantId.Value)
                        .ToList();

                    foreach (var groupe in tpesParCommercant)
                    {
                        var commercantId = groupe.Key;
                        var commercant = await _userRepository.GetByIdAsync(commercantId);

                        if (commercant == null) continue;

                        var tpesCommercant = groupe.ToList();
                        var nombreTPEs = tpesCommercant.Count;

                        // Compter les incidents pour ce commerçant
                        var tpeIds = tpesCommercant.Select(t => t.Id).ToList();
                        var incidentsCommercant = incidentTPEs.Count(it => tpeIds.Contains(it.TPEId));

                        var tauxPanne = nombreTPEs > 0
                            ? Math.Round((double)incidentsCommercant / nombreTPEs * 100, 1)
                            : 0;

                        var pourcentageTPEsTotal = totalTPEs > 0
                            ? Math.Round((double)nombreTPEs / totalTPEs * 100, 1)
                            : 0;

                        pannesParAdresse.Add(new TPEPanneParAdresseDTO
                        {
                            CommercantId = commercantId,
                            CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
                            Adresse = commercant.Adresse ?? "Adresse non renseignée",
                            NombreTPEs = nombreTPEs,
                            NombreIncidents = incidentsCommercant,
                            TauxPanne = tauxPanne,
                            PourcentageTPEsTotal = pourcentageTPEsTotal
                        });
                    }

                    // Trier par taux de panne (décroissant)
                    pannesParAdresse = pannesParAdresse
                        .OrderByDescending(a => a.TauxPanne)
                        .ToList();

                    // ============================================
                    // DASHBOARD COMPLET
                    // ============================================
                    var dashboard = new TPEDashboardDTO
                    {
                        Overview = overview,
                        PannesParModele = pannesParModele,
                        PannesParAdresse = pannesParAdresse
                    };

                    _logger.LogInformation("Dashboard TPE généré - Total TPEs: {TotalTPEs}, Total Incidents: {TotalIncidents}, Taux global: {Taux}%",
                        totalTPEs, totalIncidents, tauxGlobalPanne);

                    return ApiResponse<TPEDashboardDTO>.Success(dashboard);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la génération du dashboard TPE");
                    return ApiResponse<TPEDashboardDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        // Méthode helper pour récupérer les liaisons IncidentTPE
        private async Task<List<IncidentTPE>> GetIncidentTPEsAsync()
        {
            // Vous devez injecter IIncidentTPERepository dans le constructeur
            // Si ce n'est pas déjà fait, ajoutez-le :
            // private readonly IIncidentTPERepository _incidentTPERepository;

            var incidentTPEs = await _incidentTPERepository.GetAllAsync();
            return incidentTPEs.ToList();
        }

        // Application/Services/TPEService/TPEService.cs

        /// <summary>
        /// Récupère les TPEs du commerçant connecté avec pagination, recherche et filtres
        /// </summary>
        public async Task<ApiResponse<PagedResult<TPEDto>>> GetMesTPEsPagedAsync(TPEPagedRequest request, Guid commercantId)
        {
            return await MeasureAsync(nameof(GetMesTPEsPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogInformation("Récupération paginée des TPEs du commerçant {CommercantId} - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, Modele: {Modele}",
                        commercantId, request.Page, request.PageSize, request.SearchTerm, request.Modele);

                    // 1. Vérifier que l'utilisateur existe et a le rôle Commercant
                    var commercant = await _userRepository.GetByIdAsync(commercantId);
                    if (commercant == null)
                    {
                        return ApiResponse<PagedResult<TPEDto>>.Failure("Commerçant non trouvé", resultCode: 40);
                    }

                    var roles = await _userRepository.GetUserRolesAsync(commercantId);
                    if (!roles.Contains("Commercant"))
                    {
                        return ApiResponse<PagedResult<TPEDto>>.Failure("L'utilisateur n'a pas le rôle Commerçant", resultCode: 45);
                    }

                    // 2. Récupérer la requête de base avec les relations
                    var query = await _tpeRepository.QueryWithDetailsAsync();

                    // 3. ⚠️ IGNORER request.CommercantId - utiliser le userId du token
                    query = query.Where(t => t.CommercantId == commercantId);

                    // 4. Appliquer les filtres
                    if (request.Modele.HasValue)
                    {
                        query = query.Where(t => t.Modele == request.Modele.Value);
                        _logger.LogInformation("Filtre appliqué: Modele = {Modele}", request.Modele.Value);
                    }

                    // 5. Filtre par date de création exacte
                    if (request.CreatedAt.HasValue)
                    {
                        var date = request.CreatedAt.Value.Date;
                        var nextDay = date.AddDays(1);
                        query = query.Where(t => t.CreatedAt >= date && t.CreatedAt < nextDay);
                        _logger.LogInformation("Filtre appliqué: CreatedAt = {CreatedAt}", request.CreatedAt.Value);
                    }

                    // 6. Filtre par date de modification exacte
                    if (request.UpdatedAt.HasValue)
                    {
                        var date = request.UpdatedAt.Value.Date;
                        var nextDay = date.AddDays(1);
                        query = query.Where(t => t.UpdatedAt >= date && t.UpdatedAt < nextDay);
                        _logger.LogInformation("Filtre appliqué: UpdatedAt = {UpdatedAt}", request.UpdatedAt.Value);
                    }

                    // 7. Recherche par SearchTerm (numéro de série ou date)
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var term = request.SearchTerm.ToLower();

                        // Essayer de parser la recherche comme une date
                        DateTime? searchDate = null;
                        if (DateTime.TryParse(request.SearchTerm, out var parsedDate))
                        {
                            searchDate = parsedDate.Date;
                        }

                        query = query.Where(t =>
                            t.NumSerie.ToLower().Contains(term) ||
                            t.NumSerieComplet.ToLower().Contains(term) ||
                            (searchDate.HasValue && t.CreatedAt.Date == searchDate.Value)
                        );
                        _logger.LogInformation("Filtre appliqué: SearchTerm = {SearchTerm}", request.SearchTerm);
                    }

                    // 8. Compter le total AVANT pagination
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total TPEs trouvés: {TotalCount}", totalCount);

                    // 9. Appliquer le tri
                    query = ApplySortingToQuery(query, request.SortBy, request.SortDescending);

                    // 10. Appliquer la pagination
                    var items = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    _logger.LogInformation("{Count} TPEs récupérés pour la page {Page}", items.Count, request.Page);

                    // 11. Mapper vers DTO
                    var dtos = new List<TPEDto>();
                    foreach (var tpe in items)
                    {
                        ApplicationUser createdBy = null;
                        if (tpe.CreatedById.HasValue)
                        {
                            createdBy = await _userRepository.GetByIdAsync(tpe.CreatedById.Value);
                        }

                        ApplicationUser updatedBy = null;
                        if (tpe.UpdatedById.HasValue)
                        {
                            updatedBy = await _userRepository.GetByIdAsync(tpe.UpdatedById.Value);
                        }

                        dtos.Add(new TPEDto
                        {
                            Id = tpe.Id,
                            NumSerie = tpe.NumSerie,
                            NumSerieComplet = tpe.NumSerieComplet,
                            Modele = tpe.Modele,
                            CommercantId = tpe.CommercantId,
                            CommercantNom = tpe.Commercant != null ? $"{tpe.Commercant.Nom} {tpe.Commercant.Prenom}" : "Non assigné",
                            CreatedAt = tpe.CreatedAt,
                            CreatedByNom = createdBy != null ? $"{createdBy.Nom} {createdBy.Prenom}" : "Inconnu",
                            UpdatedAt = tpe.UpdatedAt,
                            UpdatedByNom = updatedBy != null ? $"{updatedBy.Nom} {updatedBy.Prenom}" : null
                        });
                    }

                    // 12. Créer le résultat paginé
                    var pagedResult = new PagedResult<TPEDto>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };

                    return ApiResponse<PagedResult<TPEDto>>.Success(pagedResult,
                        $"{dtos.Count} TPE(s) trouvé(s) sur {totalCount}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des TPEs du commerçant {CommercantId}", commercantId);
                    return ApiResponse<PagedResult<TPEDto>>.Failure("Erreur interne du serveur");
                }
            });
        }
    }

    // ModeleTPEHelper - CLASSE STATIQUE SÉPARÉE
    public static class ModeleTPEHelper
    {
        private static readonly Dictionary<ModeleTPE, string> _abbreviations = new()
        {
            { ModeleTPE.Ingenico, "ICT" },
            { ModeleTPE.Verifone, "VX" },
            { ModeleTPE.PAX, "PAX" },
        };

        public static string GetAbbreviation(ModeleTPE modele)
        {
            return _abbreviations.TryGetValue(modele, out var abbr) ? abbr : "TPE";
        }
        
    }

}
