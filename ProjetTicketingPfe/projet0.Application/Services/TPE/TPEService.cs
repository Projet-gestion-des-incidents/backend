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
        private readonly ILogger<TPEService> _logger;

        public TPEService(
            ITPERepository tpeRepository,
            IUserRepository userRepository,
            ILogger<TPEService> logger)
        {
            _tpeRepository = tpeRepository;
            _userRepository = userRepository;
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

        public async Task<ApiResponse<TPEDto>> CreateAsync(CreateTPEDto dto)
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

                // 3. ✅ GÉNÉRER LE NUMÉRO DE SÉRIE AUTOMATIQUEMENT
                var numSerie = await _tpeRepository.GenerateNumSerieAsync(dto.Modele);

                _logger.LogInformation("Numéro de série généré pour le modèle {Modele}: {NumSerie}",
                    dto.Modele, numSerie);

                // 4. Générer le numéro de série complet avec abréviation
                var abbreviation = ModeleTPEHelper.GetAbbreviation(dto.Modele);
                var numSerieComplet = $"{abbreviation}-{numSerie}";

                // 5. Créer le TPE
                var tpe = new TpeEntity
                {
                    Id = Guid.NewGuid(),
                    NumSerie = numSerie,
                    NumSerieComplet = numSerieComplet,
                    Modele = dto.Modele,
                    CommercantId = dto.CommercantId,
                };

                await _tpeRepository.AddAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                // 6. Mapper vers DTO
                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
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

        public async Task<ApiResponse<TPEDto>> UpdateAsync(Guid id, UpdateTPEDto dto)
        {
            return await MeasureAsync("UpdateTPE", new { id, dto }, async () =>
            {
                var tpe = await _tpeRepository.GetByIdAsync(id);
                if (tpe == null)
                {
                    _logger.LogWarning("TPE not found | Id = {Id}", id);
                    return ApiResponse<TPEDto>.Failure(
                        message: "TPE non trouvé",
                        resultCode: 42
                    );
                }
                // Dans UpdateAsync, après la vérification que le TPE existe
                // Vérifier que le numéro de série fait 6 caractères
                if (string.IsNullOrWhiteSpace(dto.NumSerie) || dto.NumSerie.Length != 6)
                {
                    _logger.LogWarning("NumSerie must be exactly 6 characters | NumSerie: {NumSerie}", dto.NumSerie);
                    return ApiResponse<TPEDto>.Failure(
                        message: "Le numéro de série doit contenir exactement 6 caractères",
                        resultCode: 46
                    );
                }
                // Vérifier unicité du numéro de série pour ce modèle si modifié
                bool modeleChanged = tpe.Modele != dto.Modele;
                bool numSerieChanged = tpe.NumSerie != dto.NumSerie;

                if (modeleChanged || numSerieChanged)
                {
                    if (!await _tpeRepository.IsNumSerieUniqueForModeleAsync(dto.NumSerie, dto.Modele, id))
                    {
                        _logger.LogWarning("NumSerie already used for this model | {NumSerie} | Modele: {Modele}",
                            dto.NumSerie, dto.Modele);
                        return ApiResponse<TPEDto>.Failure(
                            message: $"Ce numéro de série est déjà utilisé pour le modèle {dto.Modele}",
                            resultCode: 41
                        );
                    }
                }

                // Vérifier que le nouveau commerçant existe
                ApplicationUser nouveauCommercant = null;
                // Dans UpdateAsync, après avoir vérifié que le nouveau commerçant existe
                if (tpe.CommercantId != dto.CommercantId)
                {
                    nouveauCommercant = await _userRepository.GetByIdAsync(dto.CommercantId);
                    if (nouveauCommercant == null)
                    {
                        _logger.LogWarning("New commercant not found | CommercantId = {CommercantId}", dto.CommercantId);
                        return ApiResponse<TPEDto>.Failure(
                            message: "Le nouveau commerçant spécifié n'existe pas",
                            resultCode: 40
                        );
                    }

                    // Vérifier que le nouveau propriétaire a le rôle "Commercant"
                    var roles = await _userRepository.GetUserRolesAsync(dto.CommercantId);
                    if (!roles.Contains("Commercant"))
                    {
                        _logger.LogWarning("New owner is not a commercant | UserId = {UserId}, Roles: {@Roles}",
                            dto.CommercantId, roles);
                        return ApiResponse<TPEDto>.Failure(
                            message: "Le nouveau propriétaire doit avoir le rôle 'Commerçant'",
                            resultCode: 45
                        );
                    }
                }

                // Mettre à jour les champs
                tpe.NumSerie = dto.NumSerie;
                tpe.Modele = dto.Modele;

                // Regénérer le numéro complet si modèle ou numéro de série a changé
                if (modeleChanged || numSerieChanged)
                {
                    var abbreviation = ModeleTPEHelper.GetAbbreviation(dto.Modele);
                    tpe.NumSerieComplet = $"{abbreviation}-{dto.NumSerie}";
                }

                tpe.CommercantId = dto.CommercantId;

                await _tpeRepository.UpdateAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                var commercant = nouveauCommercant ?? await _userRepository.GetByIdAsync(tpe.CommercantId);

                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "",
                };

                return ApiResponse<TPEDto>.Success(
                    data: tpeDto,
                    message: "TPE mis à jour avec succès",
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

                var commercant = await _userRepository.GetByIdAsync(tpe.CommercantId);
                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    NumSerieComplet = tpe.NumSerieComplet,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "",
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
                var tpeDtos = tpes.Select(t => new TPEDto
                {
                    Id = t.Id,
                    NumSerie = t.NumSerie,
                    NumSerieComplet = t.NumSerieComplet,
                    Modele = t.Modele,
                    CommercantId = t.CommercantId,
                    CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
                });

                return ApiResponse<IEnumerable<TPEDto>>.Success(
                    data: tpeDtos,
                    message: $"{tpeDtos.Count()} TPE(s) trouvé(s)",
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
                    var commercant = await _userRepository.GetByIdAsync(tpe.CommercantId);
                    tpeDtos.Add(new TPEDto
                    {
                        Id = tpe.Id,
                        NumSerie = tpe.NumSerie,
                        NumSerieComplet = tpe.NumSerieComplet,
                        Modele = tpe.Modele,
                        CommercantId = tpe.CommercantId,
                        CommercantNom = commercant != null ? $"{commercant.Nom} {commercant.Prenom}" : "",
                    });
                }

                return ApiResponse<IEnumerable<TPEDto>>.Success(
                    data: tpeDtos,
                    message: $"{tpeDtos.Count} TPE(s) trouvé(s)",
                    resultCode: 0
                );
            });
        }

        // MÉTHODE PAGINÉE - AJOUTÉE ICI (dans la classe, pas dans le helper)
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

                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var term = request.SearchTerm.ToLower();
                        query = query.Where(t =>
                            t.NumSerie.ToLower().Contains(term) ||
                            t.NumSerieComplet.ToLower().Contains(term) ||
                            (t.Commercant != null &&
                                (t.Commercant.Nom.ToLower().Contains(term) ||
                                 t.Commercant.Prenom.ToLower().Contains(term) ||
                                 (t.Commercant.Nom + " " + t.Commercant.Prenom).ToLower().Contains(term)))
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
                    var dtos = items.Select(t => new TPEDto
                    {
                        Id = t.Id,
                        NumSerie = t.NumSerie,
                        NumSerieComplet = t.NumSerieComplet,
                        Modele = t.Modele,
                        CommercantId = t.CommercantId,
                        CommercantNom = t.Commercant != null ? $"{t.Commercant.Nom} {t.Commercant.Prenom}" : "Inconnu"
                    }).ToList();

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