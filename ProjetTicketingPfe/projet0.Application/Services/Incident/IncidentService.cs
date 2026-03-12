using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.DTOs.Ticket;
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
        private readonly ITPERepository _tpeRepository;          
        private readonly IPieceJointeService _pieceJointeService;
        private readonly IIncidentTicketRepository _incidentTicketRepository;
        private readonly ITicketRepository _ticketRepository;

        public IncidentService(
            IIncidentRepository incidentRepository,
            IUserRepository userRepository,
            IEntiteImpacteeRepository entiteImpacteeRepository,
            ILogger<IncidentService> logger,
            ITPERepository tpeRepository,
            IPieceJointeService pieceJointeService,
            IMapper mapper, 
            IIncidentTicketRepository incidentTicketRepository,
            ITicketRepository ticketRepository)
        {
            _incidentRepository = incidentRepository;
            _userRepository = userRepository;
            _entiteImpacteeRepository = entiteImpacteeRepository;
            _logger = logger;
            _tpeRepository = tpeRepository;
            _pieceJointeService = pieceJointeService;
            _mapper = mapper;
            _incidentTicketRepository = incidentTicketRepository;
            _ticketRepository = ticketRepository;

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
        public async Task<IncidentDTO> MapToDto(IncidentEntity incident)
        {
            var dto = _mapper.Map<IncidentDTO>(incident);

            dto.StatutIncidentLibelle = GetStatutLibelle(incident.StatutIncident);
            dto.Emplacement = incident.Emplacement; // Pas besoin si AutoMapper le fait

            // Ajouter le libellé du type de problème

            if (incident.CreatedById.HasValue && dto.CreatedByName == null)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            dto.NombreEntitesImpactees = incident.EntitesImpactees?.Count ?? 0;
            dto.NombreTickets = incident.IncidentTickets?.Count ?? 0;

            return dto;
        }

        //transformer un IncidentEntity en IncidentDetailDTO
        private async Task<IncidentDetailDTO> MapToDetailDto(IncidentEntity incident)
        {
            var dto = _mapper.Map<IncidentDetailDTO>(incident);

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
                        StatutTicket = it.Ticket?.StatutTicket,

                    })
                    .ToList() ?? new List<IncidentTicketDTO>();
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

        private string GetStatutLibelle(StatutIncident? statut)
        {
            if (!statut.HasValue)
                return "Non traité";  
            return statut switch
            {

                StatutIncident.EnCours => "En cours",

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
            // Filtre par SearchTerm (Code, Emplacement, Créateur)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();

                query = query.Where(i =>
                    // Recherche dans le CodeIncident
                    (i.CodeIncident != null && i.CodeIncident.ToLower().Contains(term)) ||

                    // Recherche dans l'Emplacement
                    (i.Emplacement != null && i.Emplacement.ToLower().Contains(term)) ||                    

                    // Recherche par nom du créateur (via matchedUserIds)
                    (matchedUserIds.Any() && i.CreatedById.HasValue && matchedUserIds.Contains(i.CreatedById.Value))
                );
            }

            //  Filtre par TypeProbleme 
            if (request.TypeProbleme.HasValue)
            {
                query = query.Where(i => i.TypeProbleme == request.TypeProbleme.Value);
            }

            //  Filtre par sévérité
            if (request.SeveriteIncident.HasValue)
                query = query.Where(i => i.SeveriteIncident == request.SeveriteIncident.Value);

            //  Filtre par statut
            if (request.StatutIncident.HasValue)
                query = query.Where(i => i.StatutIncident == request.StatutIncident.Value);

            //  Filtre par année de détection
            if (request.YearDetection.HasValue)
            {
                query = query.Where(i => i.DateDetection.Year == request.YearDetection.Value);
            }

            //  Filtre par année de résolution
            if (request.YearResolution.HasValue)
            {
                query = query.Where(i => i.DateResolution.HasValue &&
                                         i.DateResolution.Value.Year == request.YearResolution.Value);
            }

            return query;
        }

        // Appliquer le tri pour SearchIncidentsAsync
        private IQueryable<IncidentEntity> ApplySorting(IQueryable<IncidentEntity> query, string sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "DateDetection";  // Garder le nom exact de la propriété

            // Normaliser pour la comparaison
            var sortByLower = sortBy.ToLower();

            return (sortByLower, descending) switch
            {
                ("code", false) => query.OrderBy(i => i.CodeIncident),
                ("code", true) => query.OrderByDescending(i => i.CodeIncident),

                ("severite", false) => query.OrderBy(i => i.SeveriteIncident).ThenBy(i => i.DateDetection),
                ("severite", true) => query.OrderByDescending(i => i.SeveriteIncident).ThenBy(i => i.DateDetection),

                ("statut", false) => query.OrderBy(i => i.StatutIncident).ThenBy(i => i.DateDetection),
                ("statut", true) => query.OrderByDescending(i => i.StatutIncident).ThenBy(i => i.DateDetection),

                //  Maintenant on compare avec "datedetection"
                ("datedetection", false) => query.OrderBy(i => i.DateDetection),
                ("datedetection", true) => query.OrderByDescending(i => i.DateDetection),

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
            _logger.LogWarning("SORT PARAM - SortBy: {SortBy}, Descending: {Descending}",
            request.SortBy, request.SortDescending);
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
            _logger.LogInformation("CreateIncident START | TypeProbleme: {TypeProbleme}", dto.TypeProbleme);

            try
            {
                // 1. Vérifier l'utilisateur
                var createur = await _userRepository.GetByIdAsync(createdById);
                if (createur == null)
                    return ApiResponse<IncidentDTO>.Failure("Utilisateur non trouvé");

                // 2. Vérifier les TPEs
                var roles = await _userRepository.GetUserRolesAsync(createdById);
                var isCommercant = roles.Contains("Commercant");

                if (isCommercant && dto.TPEIds != null && dto.TPEIds.Any())
                {
                    var tpesDuCommercant = await _tpeRepository.GetByCommercantIdAsync(createdById);
                    var tpeIdsDuCommercant = tpesDuCommercant.Select(t => t.Id).ToList();

                    var tpeIdsNonAutorises = dto.TPEIds.Except(tpeIdsDuCommercant).ToList();
                    if (tpeIdsNonAutorises.Any())
                    {
                        _logger.LogWarning("Tentative d'utilisation de TPEs non autorisés | UserId: {UserId}, TPEs: {@TpeIds}",
                            createdById, tpeIdsNonAutorises);
                        return ApiResponse<IncidentDTO>.Failure(
                            "Vous ne pouvez déclarer un incident que sur vos propres TPEs",
                            resultCode: 47
                        );
                    }
                }

                // 3. Mapper le TypeProbleme vers TypeEntiteImpactee (un seul)
                var typeEntiteImpactee = MapTypeProblemeToTypeEntiteImpactee(dto.TypeProbleme);

                // 4. Générer le code incident
                var code = await _incidentRepository.GenerateCodeIncidentAsync();

                // 5. Créer l'incident (avec un seul TypeProbleme)
                var incident = new IncidentEntity
                {
                    Id = Guid.NewGuid(),
                    CodeIncident = code,
                    DescriptionIncident = dto.DescriptionIncident ?? "",
                    Emplacement = dto.Emplacement,
                    TypeProbleme = dto.TypeProbleme,  // Un seul type
                    StatutIncident = null,  // ← AUCUN STATUT À LA CRÉATION
                    DateDetection = DateTime.UtcNow,
                    CreatedById = createdById,
                    EntitesImpactees = new List<EntiteImpactee>(),
                    IncidentTPEs = new List<IncidentTPE>()
                };

                // 6. Ajouter UNE SEULE entité impactée
                incident.EntitesImpactees.Add(new EntiteImpactee
                {
                    Id = Guid.NewGuid(),
                    TypeEntiteImpactee = typeEntiteImpactee,
                    IncidentId = incident.Id
                });

                // 7. Ajouter les TPEs concernés
                if (dto.TPEIds != null)
                {
                    foreach (var tpeId in dto.TPEIds)
                    {
                        incident.IncidentTPEs.Add(new IncidentTPE
                        {
                            IncidentId = incident.Id,
                            TPEId = tpeId,
                            DateAssociation = DateTime.UtcNow
                        });
                    }
                }

                // 8. Sauvegarder
                await _incidentRepository.AddAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                // 9. Gérer les pièces jointes si présentes (vérification plus robuste)
                if (dto.PiecesJointes != null && dto.PiecesJointes.Any())
                {
                    foreach (var fichier in dto.PiecesJointes)
                    {
                        if (fichier != null && fichier.Length > 0)  // Vérifier que le fichier n'est pas vide
                        {
                            var pieceDto = new CreatePieceJointeDTO
                            {
                                Fichier = fichier
                            };

                            await _pieceJointeService.SauvegarderFichierAsync(
                                pieceDto,
                                incident.Id,
                                createdById
                            );
                        }
                    }
                }

                var result = await MapToDto(incident);

                sw.Stop();
                _logger.LogInformation("CreateIncident SUCCESS | Code: {Code} | Entité: {Entite} | Duration: {Ms} ms",
                    code, typeEntiteImpactee, sw.ElapsedMilliseconds);

                return ApiResponse<IncidentDTO>.Success(result, $"Incident {code} créé avec succès");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CreateIncident ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
                return ApiResponse<IncidentDTO>.Failure("Erreur interne du serveur");
            }
        }

        private TypeEntiteImpactee MapTypeProblemeToTypeEntiteImpactee(TypeProbleme typeProbleme)
        {
            return typeProbleme switch
            {
                TypeProbleme.PaiementRefuse => TypeEntiteImpactee.FluxTransactionnel,
                TypeProbleme.TerminalHorsLigne => TypeEntiteImpactee.MachineTPE,
                TypeProbleme.Lenteur => TypeEntiteImpactee.Reseau,
                TypeProbleme.BugAffichage => TypeEntiteImpactee.MachineTPE,
                TypeProbleme.ConnexionReseau => TypeEntiteImpactee.Reseau,
                TypeProbleme.ErreurFluxTransactionnel => TypeEntiteImpactee.FluxTransactionnel,
                TypeProbleme.ProblemeLogicielTPE => TypeEntiteImpactee.ServiceApplicatif,
                _ => TypeEntiteImpactee.MachineTPE
            };
        }

        public async Task<ApiResponse<IncidentDTO>> UpdateIncidentAsync(
    Guid incidentId,
    UpdateIncidentDTO dto,
    Guid userId)
        {
            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                if (incident == null)
                    return ApiResponse<IncidentDTO>.Failure("Incident introuvable");

                // Vérifier le rôle de l'utilisateur
                var userRoles = await _userRepository.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                // 🔴 RÈGLE : Si c'est un commerçant, vérifier si l'incident est modifiable
                if (isCommercant && !isAdmin)
                {
                    // Vérifier 1 : L'incident a-t-il déjà un statut ? (EnCours ou Fermé)
                    if (incident.StatutIncident.HasValue)
                    {
                        return ApiResponse<IncidentDTO>.Failure(
                            "Vous ne pouvez pas modifier un incident qui est déjà en cours ou fermé.",
                            resultCode: 70
                        );
                    }

                    // Vérifier 2 : L'incident est-il lié à des tickets ?
                    var ticketsLies = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);
                    if (ticketsLies != null && ticketsLies.Any())
                    {
                        _logger.LogWarning("Commerçant {UserId} a tenté de modifier l'incident {IncidentId} qui est lié à {Count} ticket(s)",
                            userId, incidentId, ticketsLies.Count());

                        return ApiResponse<IncidentDTO>.Failure(
                            "Vous ne pouvez pas modifier un incident qui est déjà lié à un ticket.",
                            resultCode: 71
                        );
                    }
                }

                // ✅ Le commerçant peut modifier ces champs (si les vérifications sont passées)
                if (isCommercant || isAdmin)
                {
                    if (!string.IsNullOrWhiteSpace(dto.DescriptionIncident))
                        incident.DescriptionIncident = dto.DescriptionIncident;

                    if (!string.IsNullOrWhiteSpace(dto.Emplacement))
                        incident.Emplacement = dto.Emplacement;

                    if (dto.TypeProbleme.HasValue)
                        incident.TypeProbleme = dto.TypeProbleme.Value;
                }

                // Seul l'admin peut modifier la sévérité
                if (isAdmin && dto.SeveriteIncident.HasValue)
                {
                    incident.SeveriteIncident = dto.SeveriteIncident.Value;
                }

                incident.UpdatedById = userId;
                incident.UpdatedAt = DateTime.UtcNow;

                await _incidentRepository.SaveChangesAsync();

                var result = await MapToDto(incident);
                return ApiResponse<IncidentDTO>.Success(result, "Incident mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur mise à jour incident {IncidentId}", incidentId);
                return ApiResponse<IncidentDTO>.Failure("Erreur interne serveur");
            }
        }
        /// <summary>
        /// Met à jour le statut d'un incident en fonction de ses tickets
        /// </summary>
        // Dans IncidentService.cs - Méthode à appeler quand un ticket change de statut
        // Dans IncidentService.cs - Remplacer MettreAJourStatutIncident

        public async Task<ApiResponse<bool>> MettreAJourStatutIncident(Guid incidentId)
        {
            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                if (incident == null)
                    return ApiResponse<bool>.Failure("Incident non trouvé");

                // Récupérer tous les tickets liés
                var ticketsLies = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);

                if (ticketsLies != null && ticketsLies.Any())
                {
                    // Si l'incident a au moins un ticket en cours, il est en cours
                    var aUnTicketEnCours = ticketsLies.Any(t => t.StatutTicket == StatutTicket.EnCours);

                    if (aUnTicketEnCours && incident.StatutIncident != StatutIncident.Ferme)
                    {
                        incident.StatutIncident = StatutIncident.EnCours;
                    }
                    // Si tous les tickets sont résolus, l'incident peut être fermé
                    // (mais c'est le technicien qui décide de fermer chaque incident individuellement)
                }
                else
                {
                    // Pas de ticket, pas de statut
                    incident.StatutIncident = null;
                    incident.DateResolution = null;
                }

                await _incidentRepository.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Statut de l'incident mis à jour");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut de l'incident {IncidentId}", incidentId);
                return ApiResponse<bool>.Failure("Erreur interne");
            }
        }

        /// <summary>
        /// Marque un incident comme Fermé (appelé quand tous ses tickets sont résolus)
        /// </summary>
        public async Task<ApiResponse<bool>> FermerIncident(Guid incidentId)
        {
            try
            {
                var incident = await _incidentRepository.GetByIdAsync(incidentId);
                if (incident == null)
                    return ApiResponse<bool>.Failure("Incident non trouvé");

                incident.StatutIncident = StatutIncident.Ferme;
                incident.DateResolution = DateTime.UtcNow;

                await _incidentRepository.SaveChangesAsync();

                _logger.LogInformation("Incident {IncidentId} marqué comme Fermé", incidentId);
                return ApiResponse<bool>.Success(true, "Incident fermé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la fermeture de l'incident {IncidentId}", incidentId);
                return ApiResponse<bool>.Failure("Erreur interne");
            }
        }
        // Dans IncidentService.cs
        public async Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("DeleteIncident START | Id: {Id}", id);

            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(id);

                if (incident == null)
                {
                    _logger.LogWarning("DeleteIncident | Incident introuvable | Id: {Id}", id);
                    return ApiResponse<bool>.Failure("Incident introuvable");
                }

                // 🔴 RÈGLE 1 : Un incident ne peut être supprimé que s'il n'a PAS de statut
                if (incident.StatutIncident.HasValue)
                {
                    _logger.LogWarning("DeleteIncident | Incident déjà traité | Id: {Id}, Statut: {Statut}",
                        id, incident.StatutIncident);
                    return ApiResponse<bool>.Failure(
                        "Impossible de supprimer un incident déjà traité (en cours, résolu ou fermé)",
                        resultCode: 48
                    );
                }

                // Vérifier si l'incident est lié à des tickets
                if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                {
                    // Option 1: Interdire la suppression
                    return ApiResponse<bool>.Failure(
                        "Impossible de supprimer un incident lié à des tickets. Supprimez d'abord les liens.",
                        resultCode: 49
                    );

                    // Option 2: Supprimer automatiquement les liens (si vous préférez)
                    // foreach (var lien in incident.IncidentTickets.ToList())
                    // {
                    //     _incidentTicketRepository.Delete(lien);
                    // }
                }

                await _incidentRepository.DeleteAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                sw.Stop();
                _logger.LogInformation("DeleteIncident SUCCESS | Duration: {Ms} ms", sw.ElapsedMilliseconds);

                return ApiResponse<bool>.Success(true, "Incident supprimé avec succès");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "DeleteIncident ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
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

        // Dans IncidentService.cs

        /// <summary>
        /// Marque un incident comme résolu (appelé par le technicien)
        /// </summary>
        public async Task<ApiResponse<bool>> ResoudreIncident(Guid incidentId, Guid userId)
        {
            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                if (incident == null)
                    return ApiResponse<bool>.Failure("Incident non trouvé");

                // Vérifier que l'incident est lié à un ticket assigné au technicien
                var ticketsLies = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);
                var ticketDuTechnicien = ticketsLies.FirstOrDefault(t => t.AssigneeId == userId);

                if (ticketDuTechnicien == null)
                {
                    return ApiResponse<bool>.Failure(
                        "Vous ne pouvez résoudre que les incidents liés à vos tickets assignés.",
                        resultCode: 60
                    );
                }

                // Vérifier que l'incident peut être résolu (statut EnCours)
                if (incident.StatutIncident != StatutIncident.EnCours)
                {
                    return ApiResponse<bool>.Failure(
                        $"L'incident doit être en cours pour être résolu (statut actuel: {incident.StatutIncident})",
                        resultCode: 61
                    );
                }

                // Marquer comme résolu
                incident.StatutIncident = StatutIncident.Ferme;
                incident.DateResolution = DateTime.UtcNow;

                await _incidentRepository.SaveChangesAsync();

                // Vérifier si tous les incidents du ticket sont résolus
                await VerifierEtCloturerTicket(ticketDuTechnicien.Id);

                _logger.LogInformation("Incident {IncidentId} marqué comme résolu par le technicien {UserId}",
                    incidentId, userId);

                return ApiResponse<bool>.Success(true, "Incident résolu avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la résolution de l'incident {IncidentId}", incidentId);
                return ApiResponse<bool>.Failure("Erreur interne");
            }
        }

        /// <summary>
        /// Vérifie si tous les incidents d'un ticket sont résolus et clôture le ticket si nécessaire
        /// </summary>
        private async Task VerifierEtCloturerTicket(Guid ticketId)
        {
            var incidents = await _incidentTicketRepository.GetIncidentsByTicketIdAsync(ticketId);

            if (incidents.All(i => i.StatutIncident == StatutIncident.Ferme))
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                if (ticket != null)
                {
                    ticket.StatutTicket = StatutTicket.Resolu;
                    ticket.DateCloture = DateTime.UtcNow;
                    await _ticketRepository.SaveChangesAsync();

                    _logger.LogInformation("Ticket {TicketId} automatiquement clôturé car tous ses incidents sont résolus",
                        ticketId);
                }
            }
        }

        #endregion
    }
}
