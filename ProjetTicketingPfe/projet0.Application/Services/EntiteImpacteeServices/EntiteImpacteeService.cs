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

        public async Task<ApiResponse<EntiteImpacteeDTO>> CreateAsync(CreateEntiteImpacteeDTO dto)
        {
            try
            {
                var entite = new EntiteImpactee
                {
                    Id = Guid.NewGuid(),
                    TypeEntiteImpactee = dto.TypeEntiteImpactee,
                    Nom = dto.Nom,
                    IncidentId = null
                };

                await _repository.AddAsync(entite);
                await _repository.SaveChangesAsync();

                var dtoResult = _mapper.Map<EntiteImpacteeDTO>(entite);
                return ApiResponse<EntiteImpacteeDTO>.Success(dtoResult, "Entité impactée créée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'entité impactée");
                return ApiResponse<EntiteImpacteeDTO>.Failure("Erreur interne du serveur");
            }
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
    }
}

