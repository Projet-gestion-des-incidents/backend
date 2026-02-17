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
            return await MeasureAsync(nameof(CreateIncidentAsync), new { dto, createdById }, async () =>
            {
                try
                {
                    // Validation
                    if (string.IsNullOrWhiteSpace(dto.TitreIncident))
                        return ApiResponse<IncidentDTO>.Failure("Le titre de l'incident est requis");

                    // Générer le code unique
                    var code = await _incidentRepository.GenerateCodeIncidentAsync();

                    // Créer l'incident
                    var incident = new IncidentEntity
                    {
                        Id = Guid.NewGuid(),
                        CodeIncident = code,
                        TitreIncident = dto.TitreIncident,
                        DescriptionIncident = dto.DescriptionIncident,
                        SeveriteIncident = dto.SeveriteIncident,
                        StatutIncident = StatutIncident.Nouveau, // toujours Nouveau à la création
                        DateDetection = DateTime.Now,
                        CreatedById = createdById,
                        IncidentTickets = new List<IncidentTicket>(),
                        EntitesImpactees = new List<EntiteImpactee>(),
                        Notifications = new List<Notification>()
                    };

                    // Ajouter les entités impactées si spécifiées
                    if (dto.EntitesImpactees != null && dto.EntitesImpactees.Any())
                    {
                        foreach (var eDto in dto.EntitesImpactees)
                        {
                            var entite = new EntiteImpactee
                            {
                                IncidentId = incident.Id,
                                TypeEntiteImpactee = eDto.TypeEntiteImpactee,
                                Nom = eDto.Nom
                            };
                            incident.EntitesImpactees.Add(entite);
                        }
                    }

                    await _incidentRepository.AddAsync(incident);
                    await _incidentRepository.SaveChangesAsync();

                    var resultDto = await MapToDto(incident);

                    return ApiResponse<IncidentDTO>.Success(resultDto, $"Incident {code} créé avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la création de l'incident");
                    return ApiResponse<IncidentDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<IncidentDTO>> UpdateIncidentAsync(Guid incidentId, UpdateIncidentDTO dto, Guid updatedById)
        {
            return await MeasureAsync(nameof(UpdateIncidentAsync), new { incidentId, dto }, async () =>
            {
                try
                {
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<IncidentDTO>.Failure("Incident introuvable");

                    // Mise à jour des champs simples
                    incident.TitreIncident = dto.TitreIncident ?? incident.TitreIncident;
                    incident.DescriptionIncident = dto.DescriptionIncident ?? incident.DescriptionIncident;
                    incident.SeveriteIncident = dto.SeveriteIncident;
                    incident.StatutIncident = dto.StatutIncident;
                    incident.UpdatedAt = DateTime.UtcNow;

                    if (dto.EntitesImpactees != null)
                    {
                        // ✅ 1️⃣ Récupérer les IDs du DTO
                        var dtoIds = dto.EntitesImpactees
                            .Where(e => e.Id.HasValue)
                            .Select(e => e.Id.Value)
                            .ToHashSet();

                        // ✅ 2️⃣ Traiter les MISES À JOUR (entités existantes)
                        foreach (var eDto in dto.EntitesImpactees.Where(e => e.Id.HasValue))
                        {
                            // Chercher l'entité dans la COLLECTION ACTUELLE (pas en base)
                            var entiteExistante = incident.EntitesImpactees
                                .FirstOrDefault(e => e.Id == eDto.Id.Value);

                            if (entiteExistante != null)
                            {
                                // ✅ MISE À JOUR DIRECTE de l'entité dans la collection
                                entiteExistante.Nom = eDto.Nom;
                                entiteExistante.TypeEntiteImpactee = eDto.TypeEntiteImpactee;
                                // IncidentId reste inchangé (déjà associé)
                            }
                            else
                            {
                                // Cas rare: entité avec ID mais pas dans la collection (réassociation)
                                var entite = await _entiteImpacteeRepository.GetByIdAsync(eDto.Id.Value);
                                if (entite != null)
                                {
                                    entite.Nom = eDto.Nom;
                                    entite.TypeEntiteImpactee = eDto.TypeEntiteImpactee;
                                    entite.IncidentId = incident.Id;
                                    incident.EntitesImpactees.Add(entite);
                                }
                            }
                        }

                        // ✅ 3️⃣ Ajouter les NOUVELLES entités (sans ID)
                        foreach (var eDto in dto.EntitesImpactees.Where(e => !e.Id.HasValue))
                        {
                            var newEntite = new EntiteImpactee
                            {
                                Nom = eDto.Nom,
                                TypeEntiteImpactee = eDto.TypeEntiteImpactee,
                                IncidentId = incident.Id
                            };
                            incident.EntitesImpactees.Add(newEntite);
                        }

                        // ✅ 4️⃣ SUPPRESSION UNIQUEMENT (quand l'utilisateur clique sur supprimer)
                        var entitesASupprimer = incident.EntitesImpactees
                            .Where(e => !dtoIds.Contains(e.Id))
                            .ToList();

                        foreach (var entite in entitesASupprimer)
                        {
                            entite.IncidentId = null; // Dissociation
                                                      // Optionnel: la retirer de la collection si vous voulez
                                                      // incident.EntitesImpactees.Remove(entite);
                        }
                    }

                    await _incidentRepository.SaveChangesAsync();

                    var resultDto = await MapToDto(incident);
                    return ApiResponse<IncidentDTO>.Success(resultDto, "Incident mis à jour avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la mise à jour de l'incident");
                    return ApiResponse<IncidentDTO>.Failure("Erreur interne du serveur");
                }
            });
        }
        public async Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id)
        {
            return await MeasureAsync(nameof(DeleteIncidentAsync), new { id }, async () =>
            {
                try
                {
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(id);
                    if (incident == null)
                        return ApiResponse<bool>.Failure($"Incident avec ID {id} non trouvé");

                    // 1️⃣ Dissocier toutes les entités impactées liées à cet incident
                    if (incident.EntitesImpactees != null)
                    {
                        foreach (var entite in incident.EntitesImpactees)
                        {
                            entite.IncidentId = null; // Dissociation au lieu de suppression
                        }
                    }

                    // 2️⃣ Supprimer uniquement l'incident
                    await _incidentRepository.DeleteAsync(incident);
                    await _incidentRepository.SaveChangesAsync();

                    return ApiResponse<bool>.Success(true, "Incident supprimé avec succès, entités impactées conservées");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression de l'incident {Id}", id);
                    return ApiResponse<bool>.Failure("Erreur interne du serveur");
                }
            });
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