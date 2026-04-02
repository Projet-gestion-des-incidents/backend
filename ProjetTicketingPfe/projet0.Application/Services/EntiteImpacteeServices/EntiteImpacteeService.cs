using AutoMapper;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Enums;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.EntiteImpacteeServices
{ 
    public class EntiteImpacteeService : IEntiteImpacteeService
    {
        private readonly IEntiteImpacteeRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<EntiteImpacteeService> _logger;

        public EntiteImpacteeService(
            IEntiteImpacteeRepository repository,
            IMapper mapper,
            ILogger<EntiteImpacteeService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<EntiteImpacteeDTO>>> GetAllAsync()
        {
            try
            {
                var entites = await _repository.GetAllAsync();
                var dtos = _mapper.Map<List<EntiteImpacteeDTO>>(entites);
                return ApiResponse<List<EntiteImpacteeDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des entités impactées");
                return ApiResponse<List<EntiteImpacteeDTO>>.Failure("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<List<EntiteImpacteeDTO>>> GetByTypeAsync(TypeEntiteImpactee type)
        {
            try
            {
                var entites = await _repository.GetByTypeAsync(type);
                var dtos = _mapper.Map<List<EntiteImpacteeDTO>>(entites);
                return ApiResponse<List<EntiteImpacteeDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des entités impactées de type {type}");
                return ApiResponse<List<EntiteImpacteeDTO>>.Failure("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<List<EntiteImpacteeDTO>>> GetByIncidentIdAsync(Guid incidentId)
        {
            try
            {
                var entites = await _repository.GetByIncidentIdAsync(incidentId);
                var dtos = _mapper.Map<List<EntiteImpacteeDTO>>(entites);
                return ApiResponse<List<EntiteImpacteeDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des entités impactées pour l'incident {incidentId}");
                return ApiResponse<List<EntiteImpacteeDTO>>.Failure("Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Ajouter une entité impactée à un incident existant
        /// </summary>
     
        public async Task<ApiResponse<EntiteImpacteeDTO>> AddToIncidentAsync(AddEntiteImpacteeDTO dto)
        {
            try
            {              
                // Vérifier que cette entité n'existe pas déjà pour cet incident
                var entitesExistantes = await _repository.GetByIncidentIdAsync(dto.IncidentId);
                if (entitesExistantes.Any(e => e.TypeEntiteImpactee == dto.TypeEntiteImpactee))
                {
                    _logger.LogWarning("Cette entité impactée existe déjà pour l'incident {IncidentId}", dto.IncidentId);
                    // Retourner une erreur ou permettre le doublon selon votre choix
                    return ApiResponse<EntiteImpacteeDTO>.Failure(
                        "Cette entité impactée existe déjà pour cet incident",
                        resultCode: 409
                    );
                }

                // Créer la nouvelle entité
                var entite = new EntiteImpactee
                {
                    Id = Guid.NewGuid(),
                    TypeEntiteImpactee = dto.TypeEntiteImpactee,
                    IncidentId = dto.IncidentId
                };

                await _repository.AddAsync(entite);
                await _repository.SaveChangesAsync();

                var dtoResult = _mapper.Map<EntiteImpacteeDTO>(entite);

                _logger.LogInformation(
                    "Entité impactée ajoutée à l'incident {IncidentId} | Type: {Type}",
                    dto.IncidentId, dto.TypeEntiteImpactee
                );

                return ApiResponse<EntiteImpacteeDTO>.Success(
                    dtoResult,
                    "Entité impactée ajoutée avec succès"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'entité impactée à l'incident {IncidentId}", dto.IncidentId);
                return ApiResponse<EntiteImpacteeDTO>.Failure("Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Supprimer une entité impactée d'un incident
        /// </summary>
        public async Task<ApiResponse<bool>> RemoveFromIncidentAsync(Guid entiteImpacteeId)
        {
            try
            {
                var entite = await _repository.GetByIdAsync(entiteImpacteeId);
                if (entite == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Entité impactée non trouvée",
                        resultCode: 404
                    );
                }

                await _repository.DeleteAsync(entite);
                await _repository.SaveChangesAsync();

                _logger.LogInformation(
                    "Entité impactée {EntiteId} supprimée de l'incident {IncidentId}",
                    entiteImpacteeId, entite.IncidentId
                );

                return ApiResponse<bool>.Success(
                    true,
                    "Entité impactée supprimée avec succès"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'entité impactée {EntiteId}", entiteImpacteeId);
                return ApiResponse<bool>.Failure("Erreur interne du serveur");
            }
        }        
    }
}

