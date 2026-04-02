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
        private readonly IIncidentTPERepository _incidentTPERepository;
        public IncidentService(
            IIncidentRepository incidentRepository,
            IUserRepository userRepository,
            IEntiteImpacteeRepository entiteImpacteeRepository,
            ILogger<IncidentService> logger,
            ITPERepository tpeRepository,
            IPieceJointeService pieceJointeService,
            IMapper mapper, 
            IIncidentTicketRepository incidentTicketRepository,
            ITicketRepository ticketRepository,
            IIncidentTPERepository incidentTPERepository)
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
            _incidentTPERepository = incidentTPERepository;

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
            dto.Emplacement = incident.Emplacement;

            if (incident.CreatedById.HasValue && dto.CreatedByName == null)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            // ✅ Remplir correctement les compteurs
            dto.NombreEntitesImpactees = incident.EntitesImpactees?.Count ?? 0;
            dto.NombreTickets = incident.IncidentTickets?.Count ?? 0;

            // ✅ Ajouter aussi le type de problème si nécessaire
            dto.TypeProbleme = incident.TypeProbleme;

            return dto;
        }

        //transformer un IncidentEntity en IncidentDetailDTO
        private async Task<IncidentDetailDTO> MapToDetailDto(IncidentEntity incident)
        {
            // 1. Laisser AutoMapper mapper les propriétés de base (sauf celles ignorées)
            var dto = _mapper.Map<IncidentDetailDTO>(incident);

            // 2. Enrichir avec les libellés
            dto.StatutIncidentLibelle = GetStatutLibelle(incident.StatutIncident);

            // 3. Nom du créateur
            if (incident.CreatedById.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            // 4. Initialiser les listes pour éviter les nulls
            dto.Tickets = new List<IncidentTicketDTO>();
            dto.EntitesImpactees = new List<EntiteImpacteeDTO>();
            dto.TPEs = new List<IncidentTPEDTO>();
            dto.PiecesJointes = new List<PieceJointeDTO>();

            // 5. Mapper les tickets (si existants)
            if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
            {
                dto.Tickets = incident.IncidentTickets
                    .Where(it => it.Ticket != null)
                    .Select(it => new IncidentTicketDTO
                    {
                        TicketId = it.TicketId,
                        ReferenceTicket = it.Ticket.ReferenceTicket,
                        TitreTicket = it.Ticket.TitreTicket,
                        StatutTicket = it.Ticket.StatutTicket,
                    })
                    .ToList();
            }

            // 6. Mapper les entités impactées (via AutoMapper)
            if (incident.EntitesImpactees != null && incident.EntitesImpactees.Any())
            {
                dto.EntitesImpactees = _mapper.Map<List<EntiteImpacteeDTO>>(incident.EntitesImpactees);
            }

            // 7. Mapper les TPEs
            if (incident.IncidentTPEs != null && incident.IncidentTPEs.Any())
            {
                dto.TPEs = incident.IncidentTPEs
                    .Where(it => it.TPE != null)
                    .Select(it => new IncidentTPEDTO
                    {
                        TPEId = it.TPEId,
                        NumSerie = it.TPE.NumSerie,
                        NumSerieComplet = it.TPE.NumSerieComplet,
                        Modele = it.TPE.Modele,
                        DateAssociation = it.DateAssociation
                    })
                    .ToList();
            }

            // 8. Mapper les pièces jointes
            if (incident.PiecesJointes != null && incident.PiecesJointes.Any())
            {
                dto.PiecesJointes = incident.PiecesJointes
                    .Select(p => new PieceJointeDTO
                    {
                        Id = p.Id,
                        NomFichier = p.NomFichier,
                        DateAjout = p.DateAjout,
                        // Url sera ajoutée dans le contrôleur
                    })
                    .ToList();
            }

            // 9. Compter les relations
            dto.NombreTickets = dto.Tickets.Count;
            dto.NombreEntitesImpactees = dto.EntitesImpactees.Count;

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
                    // Vérifier 1 : L'incident a-t-il déjà un statut ?
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

                // ✅ Gestion de la modification du TypeProbleme
                bool typeProblemeModifie = false;
                TypeEntiteImpactee? nouveauTypeEntiteImpactee = null;

                if (dto.TypeProbleme.HasValue && dto.TypeProbleme.Value != incident.TypeProbleme)
                {
                    typeProblemeModifie = true;
                    nouveauTypeEntiteImpactee = MapTypeProblemeToTypeEntiteImpactee(dto.TypeProbleme.Value);
                    incident.TypeProbleme = dto.TypeProbleme.Value;
                    _logger.LogInformation("TypeProbleme modifié de {Ancien} à {Nouveau}",
                        incident.TypeProbleme, dto.TypeProbleme.Value);
                }

                // ✅ Mise à jour des autres champs (admin ou commerçant)
                if (isCommercant || isAdmin)
                {
                    if (!string.IsNullOrWhiteSpace(dto.DescriptionIncident))
                        incident.DescriptionIncident = dto.DescriptionIncident;

                    if (!string.IsNullOrWhiteSpace(dto.Emplacement))
                        incident.Emplacement = dto.Emplacement;
                }

                // Seul l'admin peut modifier la sévérité
                if (isAdmin && dto.SeveriteIncident.HasValue)
                {
                    incident.SeveriteIncident = dto.SeveriteIncident.Value;
                }

                incident.UpdatedById = userId;
                incident.UpdatedAt = DateTime.UtcNow;

                // ✅ MISE À JOUR DE L'ENTITÉ IMPACTÉE si le TypeProbleme a changé
                if (typeProblemeModifie && nouveauTypeEntiteImpactee.HasValue)
                {
                    // Récupérer l'entité impactée existante (il y en a normalement une seule)
                    var entiteImpactee = incident.EntitesImpactees?.FirstOrDefault();

                    if (entiteImpactee != null)
                    {
                        // Mettre à jour le type de l'entité impactée
                        entiteImpactee.TypeEntiteImpactee = nouveauTypeEntiteImpactee.Value;
                        _logger.LogInformation("Entité impactée mise à jour de {AncienType} à {NouveauType}",
                            entiteImpactee.TypeEntiteImpactee, nouveauTypeEntiteImpactee.Value);
                    }
                    else
                    {
                        // Si pour une raison quelconque il n'y a pas d'entité impactée, on en crée une
                        _logger.LogWarning("Aucune entité impactée trouvée pour l'incident {IncidentId}, création d'une nouvelle", incidentId);

                        incident.EntitesImpactees ??= new List<EntiteImpactee>();
                        incident.EntitesImpactees.Add(new EntiteImpactee
                        {
                            Id = Guid.NewGuid(),
                            TypeEntiteImpactee = nouveauTypeEntiteImpactee.Value,
                            IncidentId = incident.Id
                        });
                    }
                }

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
        public async Task<ApiResponse<bool>> DeleteIncidentAsync(Guid id, Guid userId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("DeleteIncident START | Id: {Id} | UserId: {UserId}", id, userId);

            try
            {
                var incident = await _incidentRepository.GetIncidentWithDetailsAsync(id);

                if (incident == null)
                {
                    _logger.LogWarning("DeleteIncident | Incident introuvable | Id: {Id}", id);
                    return ApiResponse<bool>.Failure("Incident introuvable");
                }

                // Vérifier le rôle de l'utilisateur
                var userRoles = await _userRepository.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                // 🔴 Si c'est un commerçant, vérifier que c'est son incident
                if (isCommercant && !isAdmin && incident.CreatedById != userId)
                {
                    _logger.LogWarning("DeleteIncident | Commerçant tente de supprimer un incident qui ne lui appartient pas | UserId: {UserId}, Incident créé par: {CreatedById}",
                        userId, incident.CreatedById);
                    return ApiResponse<bool>.Failure(
                        "Vous ne pouvez supprimer que vos propres incidents.",
                        resultCode: 72
                    );
                }

                // Règles pour le commerçant
                if (isCommercant && !isAdmin)
                {
                    // RÈGLE 1 : L'incident ne doit pas avoir de statut
                    if (incident.StatutIncident.HasValue)
                    {
                        _logger.LogWarning("DeleteIncident | Commerçant tente de supprimer un incident avec statut | Id: {Id}, Statut: {Statut}",
                            id, incident.StatutIncident);
                        return ApiResponse<bool>.Failure(
                            "Vous ne pouvez pas supprimer un incident qui est déjà en cours ou fermé.",
                            resultCode: 48
                        );
                    }

                    // RÈGLE 2 : L'incident ne doit pas être lié à des tickets
                    if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                    {
                        // Vérifier si les tickets liés ont un statut
                        var ticketsAvecStatut = incident.IncidentTickets
                            .Select(it => it.Ticket)
                            .Where(t => t != null && t.StatutTicket.HasValue)
                            .ToList();

                        if (ticketsAvecStatut.Any())
                        {
                            var statuts = string.Join(", ", ticketsAvecStatut.Select(t => $"{t.ReferenceTicket}({t.StatutTicket})"));
                            _logger.LogWarning("DeleteIncident | Commerçant tente de supprimer un incident lié à des tickets avec statut | Id: {Id}, Tickets: {Tickets}",
                                id, statuts);
                            return ApiResponse<bool>.Failure(
                                "Vous ne pouvez pas supprimer un incident lié à des tickets qui ont déjà un statut.",
                                resultCode: 73
                            );
                        }

                        // Si les tickets sont sans statut, on peut supprimer mais il faut d'abord supprimer les liens
                        return ApiResponse<bool>.Failure(
                            "Impossible de supprimer un incident lié à des tickets. Veuillez d'abord supprimer les liens.",
                            resultCode: 49
                        );
                    }
                }

                // Règles pour l'admin (plus permissives)
                if (isAdmin)
                {
                    // L'admin peut supprimer même avec statut ? À vous de voir
                    // Si vous voulez que l'admin puisse tout supprimer, enlevez cette condition
                    if (incident.StatutIncident.HasValue)
                    {
                        _logger.LogWarning("DeleteIncident | Admin supprime un incident avec statut | Id: {Id}, Statut: {Statut}",
                            id, incident.StatutIncident);
                        // return ApiResponse<bool>.Failure("L'admin peut tout supprimer, même avec statut", 74);
                    }

                    // L'admin peut supprimer même avec des liens ? À vous de voir
                    if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                    {
                        _logger.LogWarning("DeleteIncident | Admin supprime un incident lié à des tickets | Id: {Id}, NbTickets: {Count}",
                            id, incident.IncidentTickets.Count);
                        // Option: Supprimer automatiquement les liens
                        foreach (var lien in incident.IncidentTickets.ToList())
                        {
                            await _incidentTicketRepository.DeleteAsync(lien);
                        }
                    }
                }

                // Suppression effective
                await _incidentRepository.DeleteAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                sw.Stop();
                _logger.LogInformation("DeleteIncident SUCCESS | Id: {Id} | Rôle: {Role} | Duration: {Ms} ms",
                    id, isAdmin ? "Admin" : "Commercant", sw.ElapsedMilliseconds);

                return ApiResponse<bool>.Success(true, "Incident supprimé avec succès");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "DeleteIncident ERROR | Id: {Id} | Duration: {Ms} ms", id, sw.ElapsedMilliseconds);
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
        public async Task<ApiResponse<bool>> DelierTPEAsync(Guid incidentId, Guid tpeId, Guid userId)
        {
            return await MeasureAsync(nameof(DelierTPEAsync), new { incidentId, tpeId }, async () =>
            {
                try
                {
                    // Vérifier que l'incident existe
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<bool>.Failure($"Incident {incidentId} non trouvé");

                    // 🔴 RÈGLE : Ne peut supprimer la liaison que si l'incident n'a PAS de statut (null)
                    if (incident.StatutIncident.HasValue)
                    {
                        _logger.LogWarning("Tentative de suppression liaison TPE pour incident avec statut {Statut} | IncidentId: {IncidentId}",
                            incident.StatutIncident, incidentId);

                        return ApiResponse<bool>.Failure(
                            "Impossible de supprimer la liaison TPE : l'incident a déjà un statut (en cours ou fermé).",
                            resultCode: 91
                        );
                    }

                    // Vérifier que le TPE existe
                    var tpe = await _tpeRepository.GetByIdAsync(tpeId);
                    if (tpe == null)
                        return ApiResponse<bool>.Failure($"TPE {tpeId} non trouvé");

                    // Vérifier que la liaison existe
                    var existe = await _incidentTPERepository.ExistsAsync(incidentId, tpeId);
                    if (!existe)
                        return ApiResponse<bool>.Failure("Ce TPE n'est pas lié à cet incident");

                    // Supprimer la liaison
                    var supprime = await _incidentTPERepository.DeleteLiaisonAsync(incidentId, tpeId);

                    if (!supprime)
                        return ApiResponse<bool>.Failure("Erreur lors de la suppression de la liaison");

                    _logger.LogInformation("Liaison TPE supprimée entre incident {IncidentId} et TPE {TPEId} par {UserId}",
                        incidentId, tpeId, userId);

                    return ApiResponse<bool>.Success(true, "TPE retiré de l'incident avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression de la liaison TPE");
                    return ApiResponse<bool>.Failure("Erreur interne du serveur");
                }
            });
        }
        public async Task<IList<string>> GetUserRolesAsync(Guid userId)
        {
            return await _userRepository.GetUserRolesAsync(userId);
        }
        /// <summary>
        /// Lie plusieurs TPEs à un incident et retourne la liste des TPEs liés
        /// </summary>
        public async Task<ApiResponse<List<IncidentTPEDTO>>> LierTPEsAsync(
            Guid incidentId,
            List<Guid> tpeIds,
            Guid userId)
        {
            return await MeasureAsync(nameof(LierTPEsAsync), new { incidentId, tpeIds }, async () =>
            {
                try
                {
                    // 1. Vérifier que l'incident existe
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<List<IncidentTPEDTO>>.Failure($"Incident {incidentId} non trouvé");

                    // 2. Vérifier les permissions (comme avant)
                    var userRoles = await _userRepository.GetUserRolesAsync(userId);
                    var isAdmin = userRoles.Contains("Admin");
                    var isCommercant = userRoles.Contains("Commercant");

                    if (isCommercant && !isAdmin)
                    {
                        if (incident.CreatedById != userId)
                            return ApiResponse<List<IncidentTPEDTO>>.Failure(
                                "Vous ne pouvez modifier que vos propres incidents.", resultCode: 75);

                        if (incident.StatutIncident.HasValue)
                            return ApiResponse<List<IncidentTPEDTO>>.Failure(
                                "Vous ne pouvez pas ajouter de TPE à un incident qui a déjà un statut.", resultCode: 76);
                    }

                    var tpesLies = new List<IncidentTPEDTO>();
                    var erreurs = new List<string>();

                    // 3. Pour chaque TPE
                    foreach (var tpeId in tpeIds)
                    {
                        // Vérifier que le TPE existe
                        var tpe = await _tpeRepository.GetByIdAsync(tpeId);
                        if (tpe == null)
                        {
                            erreurs.Add($"TPE {tpeId} non trouvé");
                            continue;
                        }

                        // Vérifier que le TPE appartient au commerçant (si nécessaire)
                        if (isCommercant && !isAdmin)
                        {
                            var tpesDuCommercant = await _tpeRepository.GetByCommercantIdAsync(userId);
                            if (!tpesDuCommercant.Any(t => t.Id == tpeId))
                            {
                                erreurs.Add($"TPE {tpe.NumSerie} ne vous appartient pas");
                                continue;
                            }
                        }

                        // Vérifier que la liaison n'existe pas déjà
                        var existeDeja = await _incidentTPERepository.ExistsAsync(incidentId, tpeId);
                        if (existeDeja)
                        {
                            erreurs.Add($"TPE {tpe.NumSerie} déjà lié à l'incident");
                            continue;
                        }

                        // Créer la liaison
                        var liaison = new IncidentTPE
                        {
                            IncidentId = incidentId,
                            TPEId = tpeId,
                            DateAssociation = DateTime.UtcNow
                        };

                        await _incidentTPERepository.AddAsync(liaison);

                        // Ajouter à la liste des résultats
                        tpesLies.Add(new IncidentTPEDTO
                        {
                            TPEId = tpe.Id,
                            NumSerie = tpe.NumSerie,
                            NumSerieComplet = tpe.NumSerieComplet,
                            Modele = tpe.Modele,
                            DateAssociation = liaison.DateAssociation
                        });
                    }

                    await _incidentTPERepository.SaveChangesAsync();

                    // Construire le message
                    string message = $"{tpesLies.Count} TPE(s) lié(s) avec succès";
                    if (erreurs.Any())
                        message += $". {erreurs.Count} erreur(s): {string.Join("; ", erreurs)}";

                    _logger.LogInformation("{Message} pour l'incident {IncidentId}", message, incidentId);

                    return ApiResponse<List<IncidentTPEDTO>>.Success(tpesLies, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la liaison multiple de TPEs");
                    return ApiResponse<List<IncidentTPEDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        #endregion
    }
}
