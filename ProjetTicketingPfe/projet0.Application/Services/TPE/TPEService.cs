using Microsoft.Extensions.Logging;
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
// Ajouter un alias pour éviter le conflit avec l'espace de noms
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

                // 2. 🔴 NOUVELLE VÉRIFICATION : Vérifier que l'utilisateur a le rôle "Commercant"
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

                // 3. Vérifier l'unicité du numéro de série pour ce modèle
                if (!await _tpeRepository.IsNumSerieUniqueForModeleAsync(dto.NumSerie, dto.Modele))
                {
                    _logger.LogWarning("NumSerie already used for this model | {NumSerie} | Modele: {Modele}",
                        dto.NumSerie, dto.Modele);
                    return ApiResponse<TPEDto>.Failure(
                        message: $"Ce numéro de série est déjà utilisé pour le modèle {dto.Modele}",
                        resultCode: 41
                    );
                }

                // 4. Générer le numéro de série complet avec abréviation
                var abbreviation = ModeleTPEHelper.GetAbbreviation(dto.Modele);
                var numSerieComplet = $"{abbreviation}-{dto.NumSerie}";

                // 5. Créer le TPE
                var tpe = new TpeEntity
                {
                    Id = Guid.NewGuid(),
                    NumSerie = dto.NumSerie,
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

                    // 🔴 Vérifier que le nouveau propriétaire a le rôle "Commercant"
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

        // Déplacer ModeleTPEHelper en dehors de la classe ou la garder comme classe interne
    }

    // ModeleTPEHelper peut être une classe séparée dans le même fichier
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