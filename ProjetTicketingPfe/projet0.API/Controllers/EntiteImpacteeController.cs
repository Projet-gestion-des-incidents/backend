using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;

namespace projet0.API.Controllers
{
    [ApiController]
    [Route("api/entites-impactees")]
    [Authorize]
    public class EntiteImpacteeController : ControllerBase
    {
        private readonly IEntiteImpacteeRepository _entiteImpacteeRepository;
        private readonly ILogger<EntiteImpacteeController> _logger;
        private readonly IMapper _mapper;

        public EntiteImpacteeController(
            IEntiteImpacteeRepository entiteImpacteeRepository,
            ILogger<EntiteImpacteeController> logger , IMapper mapper)
        {
            _entiteImpacteeRepository = entiteImpacteeRepository;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Créer une nouvelle entité impactée
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<EntiteImpacteeDTO>>> Create([FromBody] CreateEntiteImpacteeDTO dto)
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

                await _entiteImpacteeRepository.AddAsync(entite);
                await _entiteImpacteeRepository.SaveChangesAsync();

                var dtoResult = _mapper.Map<EntiteImpacteeDTO>(entite);
                return Ok(ApiResponse<EntiteImpacteeDTO>.Success(dtoResult, "Entité impactée créée avec succès", 201));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'entité impactée");
                return StatusCode(500, ApiResponse<EntiteImpacteeDTO>.Failure("Erreur interne du serveur"));
            }
        }

        /// <summary>
        /// Récupérer toutes les entités impactées disponibles
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<EntiteImpacteeDTO>>>> GetAll()
        {
            try
            {
                var entites = await _entiteImpacteeRepository.GetAllAsync();
                var dtos = _mapper.Map<List<EntiteImpacteeDTO>>(entites);
                return Ok(ApiResponse<List<EntiteImpacteeDTO>>.Success(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des entités impactées");
                return StatusCode(500, ApiResponse<List<EntiteImpacteeDTO>>.Failure("Erreur interne du serveur"));
            }
        }
    }
}
