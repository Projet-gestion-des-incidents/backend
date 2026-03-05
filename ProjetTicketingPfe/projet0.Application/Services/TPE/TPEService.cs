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
                // 1. Vérifier que le commerçant existe
                var commercant = await _userRepository.GetByIdAsync(dto.CommercantId);
                if (commercant == null)
                {
                    _logger.LogWarning("Commercant not found | CommercantId = {CommercantId}", dto.CommercantId);
                    return ApiResponse<TPEDto>.Failure(
                        message: "Le commerçant spécifié n'existe pas",
                        resultCode: 40
                    );
                }

                // 2. Vérifier l'unicité du numéro de série
                if (!await _tpeRepository.IsNumSerieUniqueAsync(dto.NumSerie))
                {
                    _logger.LogWarning("NumSerie already used | {NumSerie}", dto.NumSerie);
                    return ApiResponse<TPEDto>.Failure(
                        message: "Ce numéro de série est déjà utilisé",
                        resultCode: 41
                    );
                }

                // 3. Créer le TPE
                var tpe = new projet0.Domain.Entities.TPE
                {
                    Id = Guid.NewGuid(),
                    NumSerie = dto.NumSerie,
                    Modele = dto.Modele,
                    CommercantId = dto.CommercantId,
                };

                await _tpeRepository.AddAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                // 4. Mapper vers DTO
                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
                    Modele = tpe.Modele,
                    CommercantId = tpe.CommercantId,
                    CommercantNom = $"{commercant.Nom} {commercant.Prenom}",
                    
                };

                _logger.LogInformation(
                    "TPE created successfully | Id: {Id} | NumSerie: {NumSerie} | Commercant: {Commercant}",
                    tpe.Id, tpe.NumSerie, commercant.Email
                );

                return ApiResponse<TPEDto>.Success(
                    data: tpeDto,
                    message: "TPE créé avec succès",
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

                // Vérifier unicité du numéro de série si modifié
                if (tpe.NumSerie != dto.NumSerie)
                {
                    if (!await _tpeRepository.IsNumSerieUniqueAsync(dto.NumSerie, id))
                    {
                        _logger.LogWarning("NumSerie already used | {NumSerie}", dto.NumSerie);
                        return ApiResponse<TPEDto>.Failure(
                            message: "Ce numéro de série est déjà utilisé",
                            resultCode: 41
                        );
                    }
                }

                tpe.NumSerie = dto.NumSerie;
                tpe.Modele = dto.Modele;
                

                await _tpeRepository.UpdateAsync(tpe);
                await _tpeRepository.SaveChangesAsync();

                var commercant = await _userRepository.GetByIdAsync(tpe.CommercantId);
                var tpeDto = new TPEDto
                {
                    Id = tpe.Id,
                    NumSerie = tpe.NumSerie,
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

        

        
    }
}