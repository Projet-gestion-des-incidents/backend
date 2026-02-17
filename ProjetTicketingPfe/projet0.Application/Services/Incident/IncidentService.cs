using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using IncidentEntity = projet0.Domain.Entities.Incident;

namespace projet0.Application.Services.Incident
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEntiteImpacteeRepository _entiteImpacteeRepository;
        private readonly ILogger<IncidentService> _logger;
        private readonly IMapper _mapper;

        public IncidentService(
            IIncidentRepository incidentRepository,
            IUserRepository userRepository,
            IEntiteImpacteeRepository entiteImpacteeRepository,
            ILogger<IncidentService> logger,
            IMapper mapper)
        {
            _incidentRepository = incidentRepository;
            _userRepository = userRepository;
            _entiteImpacteeRepository = entiteImpacteeRepository;
            _logger = logger;
            _mapper = mapper;
        }

        #region Private Methods

        //mesurer et logger l’exécution d’une action asynchrone.
        private async Task<T> MeasureAsync<T>(string actionName, object input, Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("START {Action} | Input = {@Input}", actionName, input);
            try
            {
                var result = await action();
                sw.Stop();
                _logger.LogDebug("END {Action} | Elapsed: {Elapsed}ms | Success: {Success}",
                    actionName, sw.ElapsedMilliseconds, result != null);
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "ERROR {Action} | Elapsed: {Elapsed}ms | Error: {Error}",
                    actionName, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        //transformer un IncidentEntity en IncidentDTO.
        private async Task<IncidentDTO> MapToDto(IncidentEntity incident)
        {
            var dto = _mapper.Map<IncidentDTO>(incident);

            // Enrichir avec des libellés
            dto.SeveriteIncidentLibelle = GetSeveriteLibelle(incident.SeveriteIncident);
            dto.StatutIncidentLibelle = GetStatutLibelle(incident.StatutIncident);

            // Récupérer le nom du créateur si nécessaire
            if (incident.CreatedById.HasValue && dto.CreatedByName == null)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }
            if (incident.EntitesImpactees == null)
            {
                _logger.LogWarning("EntitesImpactees non chargé pour l'incident {IncidentId}", incident.Id);
                dto.NombreEntitesImpactees = 0;
            }
            else
            {
                dto.NombreEntitesImpactees = incident.EntitesImpactees.Count;
            }
            // Compter les relations
            dto.NombreTickets = incident.IncidentTickets?.Count ?? 0;

            return dto;
        }

        //transformer un IncidentEntity en IncidentDetailDTO
        private async Task<IncidentDetailDTO> MapToDetailDto(IncidentEntity incident)
        {
            var dto = _mapper.Map<IncidentDetailDTO>(incident);

            dto.SeveriteIncidentLibelle = GetSeveriteLibelle(incident.SeveriteIncident);
            dto.StatutIncidentLibelle = GetStatutLibelle(incident.StatutIncident);

            if (incident.CreatedById.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            // Mapper les tickets
            if (incident.IncidentTickets != null)
            {
                dto.Tickets = incident.IncidentTickets
                    .Select(it => new IncidentTicketDTO
                    {
                        TicketId = it.TicketId,
                        ReferenceTicket = it.Ticket?.ReferenceTicket,
                        TitreTicket = it.Ticket?.TitreTicket,
                        StatutTicket = it.Ticket?.StatutTicket ?? StatutTicket.Nouveau,
                        PrioriteTicket = it.Ticket?.PrioriteTicket ?? PrioriteTicket.Normale
                    })
                    .ToList();
            }

            // Mapper les entités impactées
            if (incident.EntitesImpactees != null)
            {
                dto.EntitesImpactees = _mapper.Map<List<EntiteImpacteeDTO>>(incident.EntitesImpactees);
            }

            dto.NombreTickets = dto.Tickets?.Count ?? 0;
            dto.NombreEntitesImpactees = dto.EntitesImpactees?.Count ?? 0;

            return dto;
        }

        private string GetSeveriteLibelle(SeveriteIncident severite)
        {
            return severite switch
            {
                SeveriteIncident.Faible => "Faible",
                SeveriteIncident.Moyenne => "Moyenne",
                SeveriteIncident.Forte => "Forte",
                _ => severite.ToString()
            };
        }

        private string GetStatutLibelle(StatutIncident statut)
        {
            return statut switch
            {
                StatutIncident.Nouveau => "Nouveau",
                StatutIncident.Assigne => "Assigné",
                StatutIncident.EnCours => "En cours",
                StatutIncident.EnAttente => "En attente",
                StatutIncident.Resolu => "Résolu",
                StatutIncident.Ferme => "Fermé",
                _ => statut.ToString()
            };
        }

        // Appliquer les filtres pour SearchIncidentsAsync
        private IQueryable<IncidentEntity> ApplySearchFilters(
       IQueryable<IncidentEntity> query,
       IncidentSearchRequest request,
       List<Guid> matchedUserIds)
        {
            // Filtre par SearchTerm
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();

                query = query.Where(i =>
                    (i.CodeIncident != null && i.CodeIncident.ToLower().Contains(term)) ||
                    (i.TitreIncident != null && i.TitreIncident.ToLower().Contains(term)) ||
                    i.DateDetection.Year.ToString().Contains(term) ||
                    (matchedUserIds.Any() && i.CreatedById.HasValue && matchedUserIds.Contains(i.CreatedById.Value))
                );
            }

            // Filtre par sévérité si renseignée
            if (request.SeveriteIncident.HasValue)
                query = query.Where(i => i.SeveriteIncident == request.SeveriteIncident.Value);

            // Filtre par statut si renseigné
            if (request.StatutIncident.HasValue)
                query = query.Where(i => i.StatutIncident == request.StatutIncident.Value);

            return query;
        }


        // Appliquer le tri pour SearchIncidentsAsync
        private IQueryable<IncidentEntity> ApplySorting(IQueryable<IncidentEntity> query, string sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(i => i.DateDetection);

            sortBy = sortBy.ToLower();

            return (sortBy, descending) switch
            {
                ("code", false) => query.OrderBy(i => i.CodeIncident),
                ("code", true) => query.OrderByDescending(i => i.CodeIncident),
                ("titre", false) => query.OrderBy(i => i.TitreIncident),
                ("titre", true) => query.OrderByDescending(i => i.TitreIncident),
                ("severite", false) => query.OrderBy(i => i.SeveriteIncident),
                ("severite", true) => query.OrderByDescending(i => i.SeveriteIncident),
                ("statut", false) => query.OrderBy(i => i.StatutIncident),
                ("statut", true) => query.OrderByDescending(i => i.StatutIncident),
                ("date", false) => query.OrderBy(i => i.DateDetection),
                ("date", true) => query.OrderByDescending(i => i.DateDetection),
                _ => query.OrderByDescending(i => i.DateDetection)
            };
        }

        #endregion

        #region CRUD Operations

        public async Task<ApiResponse<IncidentDTO>> GetIncidentByIdAsync(Guid id)
        {
            return await MeasureAsync(nameof(GetIncidentByIdAsync), new { id }, async () =>
            {
                try
                {
                    var incident = await _incidentRepository.GetByIdAsync(id);

                    if (incident == null)
                        return ApiResponse<IncidentDTO>.Failure($"Incident avec ID {id} non trouvé");

                    var dto = await MapToDto(incident);
                    return ApiResponse<IncidentDTO>.Success(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération de l'incident {Id}", id);
                    return ApiResponse<IncidentDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<IncidentDetailDTO>> GetIncidentDetailAsync(Guid id)
        {
            return await MeasureAsync(nameof(GetIncidentDetailAsync), new { id }, async () =>
            {
                try
                {
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(id);

                    if (incident == null)
                        return ApiResponse<IncidentDetailDTO>.Failure($"Incident avec ID {id} non trouvé");

                    var dto = await MapToDetailDto(incident);
                    return ApiResponse<IncidentDetailDTO>.Success(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération détaillée de l'incident {Id}", id);
                    return ApiResponse<IncidentDetailDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<List<IncidentDTO>>> GetAllIncidentsAsync()
        {
            return await MeasureAsync(nameof(GetAllIncidentsAsync), null, async () =>
            {
                try
                {
                    var incidents = await _incidentRepository.GetAllAsync();
                    var dtos = new List<IncidentDTO>();

                    foreach (var incident in incidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    return ApiResponse<List<IncidentDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération de tous les incidents");
                    return ApiResponse<List<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<PagedResult<IncidentDTO>>> SearchIncidentsAsync(IncidentSearchRequest request)
        {
            return await MeasureAsync(nameof(SearchIncidentsAsync), request, async () =>
            {
                try
                {
                    var query = _incidentRepository.QueryWithDetails();

                    List<Guid> matchedUserIds = new();

                    // Recherche utilisateurs uniquement si SearchTerm est renseigné
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var userSearchRequest = new UserSearchRequest
                        {
                            SearchTerm = request.SearchTerm,
                            Page = 1,
                            PageSize = 1000
                        };

                        var (users, _) = await _userRepository.SearchUsersAsync(userSearchRequest);
                        matchedUserIds = users.Select(u => u.Id).ToList();
                    }

                    // Appliquer tous les filtres, SearchTerm est optionnel
                    query = ApplySearchFilters(query, request, matchedUserIds);

                    var totalCount = await query.CountAsync();

                    query = ApplySorting(query, request.SortBy, request.SortDescending);

                    var pagedIncidents = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    var dtos = new List<IncidentDTO>();
                    foreach (var incident in pagedIncidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    var result = new PagedResult<IncidentDTO>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };

                    return ApiResponse<PagedResult<IncidentDTO>>.Success(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la recherche d'incidents");
                    return ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<IncidentDTO>> CreateIncidentAsync(CreateIncidentDTO dto, Guid createdById)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("CreateIncident START | Titre: {Titre}", dto.TitreIncident);

            try
            {
                if (string.IsNullOrWhiteSpace(dto.TitreIncident))
                    return ApiResponse<IncidentDTO>.Failure("Le titre est requis");

                var code = await _incidentRepository.GenerateCodeIncidentAsync();

                var incident = new IncidentEntity
                {
                    Id = Guid.NewGuid(),
                    CodeIncident = code,
                    TitreIncident = dto.TitreIncident,
                    DescriptionIncident = dto.DescriptionIncident,
                    SeveriteIncident = dto.SeveriteIncident,
                    StatutIncident = StatutIncident.Nouveau,
                    DateDetection = DateTime.UtcNow,
                    CreatedById = createdById,
                    EntitesImpactees = new List<EntiteImpactee>()
                };

                if (dto.EntitesImpactees != null)
                {
                    foreach (var e in dto.EntitesImpactees)
                    {
                        incident.EntitesImpactees.Add(new EntiteImpactee
                        {
                            Nom = e.Nom,
                            TypeEntiteImpactee = e.TypeEntiteImpactee,
                            IncidentId = incident.Id
                        });
                    }
                }

                await _incidentRepository.AddAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                var result = await MapToDto(incident);

                sw.Stop();
                _logger.LogInformation("CreateIncident SUCCESS | Code: {Code} | Duration: {Ms} ms",
                    code, sw.ElapsedMilliseconds);

                return ApiResponse<IncidentDTO>.Success(result, $"Incident {code} créé");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CreateIncident ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
                return ApiResponse<IncidentDTO>.Failure("Erreur interne du serveur");
            }
        }
        public async Task<ApiResponse<IncidentDTO>> UpdateIncidentAsync(
     Guid incidentId,
     UpdateIncidentDTO dto,
     Guid updatedById)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("UpdateIncident START | Id: {IncidentId}", incidentId);

            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);

                if (incident == null)
                {
                    _logger.LogWarning("UpdateIncident | Incident introuvable | Id: {IncidentId}", incidentId);
                    return ApiResponse<IncidentDTO>.Failure("Incident introuvable");
                }

                // 🔹 1️⃣ Mise à jour des champs simples
                _logger.LogDebug("Updating basic fields");
                incident.TitreIncident = dto.TitreIncident ?? incident.TitreIncident;
                incident.DescriptionIncident = dto.DescriptionIncident ?? incident.DescriptionIncident;
                incident.SeveriteIncident = dto.SeveriteIncident;
                incident.StatutIncident = dto.StatutIncident;
                incident.UpdatedAt = DateTime.UtcNow;
                incident.UpdatedById = updatedById;


                if (dto.EntitesImpactees != null)
                {
                    _logger.LogDebug("Processing {Count} entitesImpactees from DTO", dto.EntitesImpactees.Count);

                    var existingEntities = incident.EntitesImpactees.ToList();

                    var dtoIds = dto.EntitesImpactees.Where(e => e.Id.HasValue).Select(e => e.Id.Value).ToHashSet();
                    var toRemove = existingEntities.Where(e => !dtoIds.Contains(e.Id)).ToList();
                    _logger.LogDebug("Found {Count} entities to remove", toRemove.Count);

                    foreach (var e in toRemove)
                    {
                        incident.EntitesImpactees.Remove(e);
                        _logger.LogDebug("Removing entity | Id: {Id}, Nom: {Nom}", e.Id, e.Nom);

                    }

                    // 🔹 Mettre à jour les existantes
                    foreach (var eDto in dto.EntitesImpactees.Where(e => e.Id.HasValue))
                    {
                        var existing = incident.EntitesImpactees.FirstOrDefault(e => e.Id == eDto.Id.Value);
                        if (existing != null)
                        {
                            existing.Nom = eDto.Nom;
                            existing.TypeEntiteImpactee = eDto.TypeEntiteImpactee;
                        }
                    }

                    // 🔹 Ajouter les nouvelles
                    foreach (var eDto in dto.EntitesImpactees.Where(e => !e.Id.HasValue))
                    {
                        var newEntite = new EntiteImpactee
                        {
                            Id = Guid.NewGuid(),
                            Nom = eDto.Nom,
                            TypeEntiteImpactee = eDto.TypeEntiteImpactee,
                            IncidentId = incident.Id
                        };
                        incident.EntitesImpactees.Add(newEntite);
                    }
                }
                else
                {
                    _logger.LogDebug("DTO entitesImpactees is null");
                }

                _logger.LogDebug("Saving changes to repository");
                await _incidentRepository.SaveChangesAsync();

                var resultDto = await MapToDto(incident);

                sw.Stop();
                _logger.LogInformation("UpdateIncident SUCCESS | Duration: {Ms} ms", sw.ElapsedMilliseconds);

                return ApiResponse<IncidentDTO>.Success(resultDto, "Incident mis à jour avec succès");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "UpdateIncident ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);

                // 🔹 Pour débogage, on peut renvoyer le message d'exception (en dev uniquement)
                return ApiResponse<IncidentDTO>.Failure($"Erreur interne du serveur: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("DeleteIncident START | Id: {Id}", id);

            try
            {
                var incident = await _incidentRepository.GetByIdAsync(id);

                if (incident == null)
                {
                    _logger.LogWarning("DeleteIncident | Incident introuvable | Id: {Id}", id);
                    return ApiResponse<bool>.Failure("Incident introuvable");
                }

                await _incidentRepository.DeleteAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                sw.Stop();
                _logger.LogInformation("DeleteIncident SUCCESS | Duration: {Ms} ms",
                    sw.ElapsedMilliseconds);

                return ApiResponse<bool>.Success(true, "Incident supprimé avec ses entités liées");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "DeleteIncident ERROR | Duration: {Ms} ms",
                    sw.ElapsedMilliseconds);

                return ApiResponse<bool>.Failure("Erreur interne du serveur");
            }
        }
        #endregion

        #region Specific Methods


        public async Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByStatutAsync(StatutIncident statut)
        {
            return await MeasureAsync(nameof(GetIncidentsByStatutAsync), new { statut }, async () =>
            {
                try
                {
                    var incidents = await _incidentRepository.GetIncidentsByStatutAsync(statut);
                    var dtos = new List<IncidentDTO>();

                    foreach (var incident in incidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    return ApiResponse<List<IncidentDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des incidents par statut {Statut}", statut);
                    return ApiResponse<List<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<List<IncidentDTO>>> GetIncidentsBySeveriteAsync(SeveriteIncident severite)
        {
            return await MeasureAsync(nameof(GetIncidentsBySeveriteAsync), new { severite }, async () =>
            {
                try
                {
                    var incidents = await _incidentRepository.GetIncidentsBySeveriteAsync(severite);
                    var dtos = new List<IncidentDTO>();

                    foreach (var incident in incidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    return ApiResponse<List<IncidentDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des incidents par sévérité {Severite}", severite);
                    return ApiResponse<List<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<List<IncidentDTO>>> GetIncidentsByCreatedByAsync(Guid createdById)
        {
            return await MeasureAsync(nameof(GetIncidentsByCreatedByAsync), new { createdById }, async () =>
            {
                try
                {
                    var incidents = await _incidentRepository.GetIncidentsByCreatedByAsync(createdById);
                    var dtos = new List<IncidentDTO>();

                    foreach (var incident in incidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    return ApiResponse<List<IncidentDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des incidents par créateur {CreatedById}", createdById);
                    return ApiResponse<List<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }




        #endregion
    }
}