using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
using TicketEntity = projet0.Domain.Entities.Ticket;

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
        private readonly IWebHostEnvironment _environment;
        private readonly ICommentaireRepository _commentaireRepository;
        private readonly IPieceJointeRepository _pieceJointeRepository;// ✅ AJOUTER

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
            IIncidentTPERepository incidentTPERepository,
            IWebHostEnvironment environment,
            IPieceJointeRepository pieceJointeRepository,      // ✅ AJOUTER
            ICommentaireRepository commentaireRepository)
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
            _environment = environment;
            _pieceJointeRepository = pieceJointeRepository;
            _commentaireRepository = commentaireRepository;
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
            dto.SeveriteIncidentLibelle = GetSeveriteLibelle(incident.SeveriteIncident); // ✅ Appel correct
            dto.Emplacement = incident.Emplacement;

            if (incident.CreatedById.HasValue && dto.CreatedByName == null)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            dto.TypeProbleme = incident.TypeProbleme;

            // ✅ AJOUTER LE MAPPAGE DES ENTITÉS IMPACTÉES
            if (incident.EntitesImpactees != null && incident.EntitesImpactees.Any())
            {
                dto.EntitesImpactees = incident.EntitesImpactees
                    .Select(e => new EntiteImpacteeDTO
                    {
                        Id = e.Id,
                        TypeEntiteImpactee = e.TypeEntiteImpactee,
                        // Ajoutez d'autres propriétés si nécessaire
                    })
                    .ToList();
            }
            else
            {
                dto.EntitesImpactees = new List<EntiteImpacteeDTO>();
            }

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
                        ContentType=p.ContentType,
                        // Url sera ajoutée dans le contrôleur
                    })
                    .ToList();
            }

            return dto;
        }

        private string GetStatutLibelle(StatutIncident? statut)
        {
            if (!statut.HasValue || statut == StatutIncident.NonTraite)
                return "Non traité";  
            return statut switch
            {

                StatutIncident.EnCours => "En cours",

                StatutIncident.Ferme => "Fermé",
                _ => statut.ToString()
            };
        }

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
                    (i.CodeIncident != null && i.CodeIncident.ToLower().Contains(term)) ||
                    (i.Emplacement != null && i.Emplacement.ToLower().Contains(term)) ||
                    (matchedUserIds.Any() && i.CreatedById.HasValue && matchedUserIds.Contains(i.CreatedById.Value))
                );
            }

            // ✅ FILTRE PAR STATUT (gère le null)
            if (request.StatutIncident.HasValue)
            {
                query = query.Where(i => i.StatutIncident == request.StatutIncident.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.StatutLibelle))
            {
                switch (request.StatutLibelle.ToLower().Trim())
                {
                    case "nontraite":
                    case "non traité":
                    case "non-traite":
                        // Pour "Non traité", inclure NULL ET 0
                        query = query.Where(i =>
                            i.StatutIncident == null ||
                            i.StatutIncident == StatutIncident.NonTraite);
                        break;
                    case "encours":
                    case "en cours":
                        query = query.Where(i => i.StatutIncident == StatutIncident.EnCours);
                        break;
                    case "ferme":
                    case "fermé":
                        query = query.Where(i => i.StatutIncident == StatutIncident.Ferme);
                        break;
                }
            }
            else if (request.StatutIncident.HasValue)
            {
                // Si c'est un nombre (valeur enum)
                query = query.Where(i => i.StatutIncident == request.StatutIncident.Value);
            }

            // ✅ FILTRE PAR SÉVÉRITÉ (CORRIGÉ)
            if (request.SeveriteIncident.HasValue)
            {
                query = query.Where(i => i.SeveriteIncident == request.SeveriteIncident.Value);
            }
            else if (!string.IsNullOrWhiteSpace(request.SeveriteLibelle))
            {
                switch (request.SeveriteLibelle.ToLower().Trim())
                {
                    case "nondefinie":
                    case "non définie":
                    case "non-definie":
                        // Filtrer les incidents avec SeveriteIncident = 0 (NonDefinie)
                        query = query.Where(i => i.SeveriteIncident == SeveriteIncident.NonDefinie);
                        break;
                    case "faible":
                        query = query.Where(i => i.SeveriteIncident == SeveriteIncident.Faible);
                        break;
                    case "moyenne":
                        query = query.Where(i => i.SeveriteIncident == SeveriteIncident.Moyenne);
                        break;
                    case "forte":
                        query = query.Where(i => i.SeveriteIncident == SeveriteIncident.Forte);
                        break;
                }
            }

            // Filtre par TypeProbleme
            if (request.TypeProbleme.HasValue)
            {
                query = query.Where(i => i.TypeProbleme == request.TypeProbleme.Value);
            }

            // Filtre par année de détection
            if (request.YearDetection.HasValue)
            {
                query = query.Where(i => i.DateDetection.Year == request.YearDetection.Value);
            }

            // Filtre par année de résolution
            if (request.YearResolution.HasValue)
            {
                query = query.Where(i => i.DateResolution.HasValue &&
                                         i.DateResolution.Value.Year == request.YearResolution.Value);
            }
            if (request.DateDetection.HasValue)
            {
                var date = request.DateDetection.Value.Date;
                var nextDay = date.AddDays(1);
                query = query.Where(i => i.DateDetection >= date && i.DateDetection < nextDay);
            }

            // ✅ NOUVEAU : Filtre par date de résolution exacte
            if (request.DateResolution.HasValue)
            {
                var date = request.DateResolution.Value.Date;
                var nextDay = date.AddDays(1);
                query = query.Where(i => i.DateResolution.HasValue &&
                                         i.DateResolution.Value >= date &&
                                         i.DateResolution.Value < nextDay);
            }


            return query;
        }

        // Appliquer le tri pour SearchIncidentsAsync
        private IQueryable<IncidentEntity> ApplySorting(IQueryable<IncidentEntity> query, string sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "DateDetection";

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
                    StatutIncident = StatutIncident.NonTraite,  // ← AUCUN STATUT À LA CRÉATION
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

                // RÈGLE : Si c'est un commerçant, vérifier si l'incident est modifiable
                if (isCommercant && !isAdmin)
                {
                    // ✅ CORRECTION : Vérifier si l'incident est en cours (StatutIncident = 1) ou fermé (StatutIncident = 2)
                    // "Non traité" = 0 → modifiable
                    // "En cours" = 1 → non modifiable
                    // "Fermé/Résolu" = 2 → non modifiable

                    if (incident.StatutIncident == StatutIncident.EnCours ||
                        incident.StatutIncident == StatutIncident.Ferme)
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
                            "Cet incident ne peut pas être modifié car il est déjà lié à un ticket.",
                            resultCode: 71
                        );
                    }
                }


                // Gestion de la modification du TypeProbleme
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

                // Mise à jour des autres champs (admin ou commerçant)
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

                // MISE À JOUR DE L'ENTITÉ IMPACTÉE si le TypeProbleme a changé
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

                var ticketsLies = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);

                if (ticketsLies != null && ticketsLies.Any())
                {
                    var aUnTicketEnCours = ticketsLies.Any(t => t.StatutTicket == StatutTicket.EnCours);
                    var aUnTicketAssigne = ticketsLies.Any(t => t.StatutTicket == StatutTicket.Assigne);
                    var tousLesTicketsResolus = ticketsLies.All(t => t.StatutTicket == StatutTicket.Resolu);

                    if (tousLesTicketsResolus)
                    {
                        // ✅ TOUS les tickets sont résolus → incident fermé
                        incident.StatutIncident = StatutIncident.Ferme;
                        incident.DateResolution = DateTime.UtcNow;
                        _logger.LogInformation("Incident {IncidentId} fermé car tous ses tickets sont résolus", incidentId);
                    }
                    else if (aUnTicketEnCours)
                    {
                        // ✅ Au moins un ticket en cours → incident en cours
                        incident.StatutIncident = StatutIncident.EnCours;
                        incident.DateResolution = null;
                        _logger.LogInformation("Incident {IncidentId} en cours", incidentId);
                    }
                    else if (aUnTicketAssigne && !aUnTicketEnCours)
                    {
                        // ✅ Tickets assignés mais aucun en cours → incident reste en cours
                        incident.StatutIncident = StatutIncident.EnCours;
                        incident.DateResolution = null;
                    }
                }
                else
                {
                    // ✅ Plus aucun ticket lié → incident sans statut
                    incident.StatutIncident = null;
                    incident.DateResolution = null;
                    _logger.LogInformation("Incident {IncidentId} : plus de tickets liés, statut remis à null", incidentId);
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

                var userRoles = await _userRepository.GetUserRolesAsync(userId);
                var isAdmin = userRoles.Contains("Admin");
                var isCommercant = userRoles.Contains("Commercant");

                // Récupérer les tickets liés AVANT suppression
                var ticketsLies = new List<TicketEntity>();
                if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                {
                    ticketsLies = incident.IncidentTickets
                        .Select(it => it.Ticket)
                        .Where(t => t != null)
                        .ToList();
                }

                // Règles pour le commerçant
                if (isCommercant && !isAdmin)
                {
                    // 1. Vérifier que c'est SON incident
                    if (incident.CreatedById != userId)
                    {
                        return ApiResponse<bool>.Failure("Vous ne pouvez supprimer que vos propres incidents.", resultCode: 72);
                    }

                    // 2. Vérifier que l'incident n'est PAS FERMÉ
                    if (incident.StatutIncident == StatutIncident.Ferme)
                    {
                        return ApiResponse<bool>.Failure(
                            "Vous ne pouvez pas supprimer un incident fermé.",
                            resultCode: 48
                        );
                    }

                    // ✅ Plus de vérification sur les tickets liés
                    // Le commerçant peut supprimer son incident même s'il est lié à des tickets
                }

                // Admin : supprimer les liaisons
                if (isAdmin)
                {
                    if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                    {
                        foreach (var lien in incident.IncidentTickets.ToList())
                        {
                            await _incidentTicketRepository.DeleteAsync(lien);
                        }
                    }
                }

                // Supprimer l'incident
                await _incidentRepository.DeleteAsync(incident);
                await _incidentRepository.SaveChangesAsync();

                // ✅ Gérer les tickets qui n'ont plus d'incidents
                foreach (var ticket in ticketsLies)
                {
                    var ticketMisAJour = await _ticketRepository.GetTicketWithDetailsAsync(ticket.Id);
                    if (ticketMisAJour == null) continue;

                    var incidentsRestants = ticketMisAJour.IncidentTickets?.Select(it => it.Incident).Where(i => i != null).ToList() ?? new List<IncidentEntity>();

                    if (!incidentsRestants.Any())
                    {
                        _logger.LogInformation("Ticket {TicketId} n'a plus d'incidents liés - suppression", ticket.Id);

                        // ✅ Supprimer les commentaires et leurs pièces jointes
                        if (ticketMisAJour.Commentaires != null && ticketMisAJour.Commentaires.Any())
                        {
                            foreach (var commentaire in ticketMisAJour.Commentaires.ToList())
                            {
                                // Supprimer les pièces jointes du commentaire
                                if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                                {
                                    foreach (var piece in commentaire.PiecesJointes.ToList())
                                    {
                                        // Supprimer via le service
                                        await _pieceJointeService.SupprimerFichierAsync(piece.Id);
                                    }
                                }
                                // Supprimer le commentaire
                                await _commentaireRepository.DeleteAsync(commentaire);
                            }
                        }

                        // Supprimer le ticket
                        await _ticketRepository.DeleteAsync(ticketMisAJour);
                        _logger.LogInformation("Ticket {TicketId} supprimé définitivement", ticket.Id);
                    }
                }

                await _ticketRepository.SaveChangesAsync();

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

        // Dans IncidentService.cs - Ajouter cette méthode

        public async Task<ApiResponse<IncidentDashboardDTO>> GetIncidentDashboardAsync()
        {
            return await MeasureAsync(nameof(GetIncidentDashboardAsync), null, async () =>
            {
                try
                {
                    _logger.LogInformation("Récupération du dashboard incidents");

                    // Récupérer tous les incidents
                    var incidents = await _incidentRepository.GetAllAsync();
                    var incidentsList = incidents.ToList();

                    // ============================================
                    // 1. STATISTIQUES GLOBALES
                    // ============================================
                    var total = incidentsList.Count;
                    var nonTraite = incidentsList.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite);
                    var enCours = incidentsList.Count(i => i.StatutIncident == StatutIncident.EnCours);
                    var ferme = incidentsList.Count(i => i.StatutIncident == StatutIncident.Ferme);

                    var overview = new IncidentDashboardOverviewDTO
                    {
                        TotalIncidents = total,
                        IncidentsNonTraite = nonTraite,
                        IncidentsEnCours = enCours,
                        IncidentsFerme = ferme,
                        TauxNonTraite = total > 0 ? Math.Round((double)nonTraite / total * 100, 1) : 0,
                        TauxEnCours = total > 0 ? Math.Round((double)enCours / total * 100, 1) : 0,
                        TauxFerme = total > 0 ? Math.Round((double)ferme / total * 100, 1) : 0
                    };

                    // ============================================
                    // 2. STATISTIQUES PAR STATUT (pour graphique)
                    // ============================================
                    var statsParStatut = new List<IncidentStatutStatDTO>
            {
                new IncidentStatutStatDTO
                {
                    Statut = "Non traité",
                    Count = nonTraite,
                    Color = "#ffc107",  // Jaune
                    Pourcentage = total > 0 ? Math.Round((double)nonTraite / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "En cours",
                    Count = enCours,
                    Color = "#17a2b8",  // Bleu
                    Pourcentage = total > 0 ? Math.Round((double)enCours / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "Fermé",
                    Count = ferme,
                    Color = "#28a745",  // Vert
                    Pourcentage = total > 0 ? Math.Round((double)ferme / total * 100, 1) : 0
                }
            };

                    // ============================================
                    // 3. STATISTIQUES PAR JOUR (7 derniers jours)
                    // ============================================
                    var statsParJour = new List<IncidentJournalierDTO>();
                    var today = DateTime.Today;

                    for (int i = 6; i >= 0; i--)
                    {
                        var date = today.AddDays(-i);
                        var incidentsDuJour = incidentsList.Where(i => i.DateDetection.Date == date).ToList();

                        statsParJour.Add(new IncidentJournalierDTO
                        {
                            Date = date,
                            Crees = incidentsDuJour.Count,
                            NonTraite = incidentsDuJour.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsDuJour.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsDuJour.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    // ============================================
                    // 4. STATISTIQUES PAR SEMAINE (4 dernières semaines)
                    // ============================================
                    var statsParSemaine = new List<IncidentJournalierDTO>();
                    var todayWeek = DateTime.Today;

                    for (int i = 3; i >= 0; i--)
                    {
                        var debutSemaine = todayWeek.AddDays(-(int)todayWeek.DayOfWeek - (i * 7));
                        var finSemaine = debutSemaine.AddDays(6);
                        var incidentsSemaine = incidentsList.Where(i => i.DateDetection.Date >= debutSemaine && i.DateDetection.Date <= finSemaine).ToList();

                        statsParSemaine.Add(new IncidentJournalierDTO
                        {
                            Date = debutSemaine,
                            Crees = incidentsSemaine.Count,
                            NonTraite = incidentsSemaine.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    // ============================================
                    // 5. STATISTIQUES PAR MOIS (6 derniers mois)
                    // ============================================
                    var statsParMois = new List<IncidentJournalierDTO>();
                    var todayMonth = DateTime.Today;

                    for (int i = 5; i >= 0; i--)
                    {
                        var dateMois = todayMonth.AddMonths(-i);
                        var debutMois = new DateTime(dateMois.Year, dateMois.Month, 1);
                        var finMois = debutMois.AddMonths(1).AddDays(-1);
                        var incidentsMois = incidentsList.Where(i => i.DateDetection.Date >= debutMois && i.DateDetection.Date <= finMois).ToList();

                        statsParMois.Add(new IncidentJournalierDTO
                        {
                            Date = debutMois,
                            Crees = incidentsMois.Count,
                            NonTraite = incidentsMois.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsMois.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsMois.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    var dashboard = new IncidentDashboardDTO
                    {
                        Overview = overview,
                        StatsParStatut = statsParStatut,
                        StatsParJour = statsParJour,
                        StatsParSemaine = statsParSemaine,
                        StatsParMois = statsParMois
                    };

                    _logger.LogInformation("Dashboard incidents généré avec succès - Total: {Total}, Non traité: {NonTraite}, En cours: {EnCours}, Fermé: {Ferme}",
                        total, nonTraite, enCours, ferme);

                    return ApiResponse<IncidentDashboardDTO>.Success(dashboard);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la génération du dashboard incidents");
                    return ApiResponse<IncidentDashboardDTO>.Failure("Erreur interne du serveur");
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

                    // RÈGLE : Ne peut supprimer la liaison que si l'incident n'a PAS de statut (null)
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
        private string GetSeveriteLibelle(SeveriteIncident severite)
        {
            return severite switch
            {
                SeveriteIncident.NonDefinie => "Non définie",
                SeveriteIncident.Faible => "Faible",
                SeveriteIncident.Moyenne => "Moyenne",
                SeveriteIncident.Forte => "Forte",
                _ => severite.ToString()
            };
        }

        // Dans IncidentService.cs
        public async Task<ApiResponse<PagedResult<IncidentDTO>>> GetMyIncidentsPagedAsync(IncidentSearchRequest request, Guid userId)
        {
            return await MeasureAsync(nameof(GetMyIncidentsPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogInformation("Début GetMyIncidentsPagedAsync - UserId: {UserId}, Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
                        userId, request.Page, request.PageSize, request.SearchTerm);

                    // 1. Obtenir la requête de base avec les détails
                    var query = _incidentRepository.QueryWithDetails();

                    // 2. Filtrer par l'utilisateur connecté (ses propres incidents)
                    query = query.Where(i => i.CreatedById == userId);

                    // 3. Recherche utilisateurs pour le SearchTerm (si nécessaire)
                    List<Guid> matchedUserIds = new();
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

                    // 4. Appliquer tous les filtres (comme dans SearchIncidentsAsync)
                    query = ApplySearchFilters(query, request, matchedUserIds);

                    // 5. Compter le total AVANT pagination
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total incidents trouvés pour l'utilisateur {UserId}: {TotalCount}", userId, totalCount);

                    // 6. Appliquer le tri
                    query = ApplySorting(query, request.SortBy, request.SortDescending);

                    // 7. Appliquer la pagination
                    var pagedIncidents = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    _logger.LogInformation("{Count} incidents récupérés pour la page {Page}", pagedIncidents.Count, request.Page);

                    // 8. Mapper vers DTO
                    var dtos = new List<IncidentDTO>();
                    foreach (var incident in pagedIncidents)
                    {
                        dtos.Add(await MapToDto(incident));
                    }

                    // 9. Créer le résultat paginé
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
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des incidents de l'utilisateur {UserId}", userId);
                    return ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        #endregion
    }
}
