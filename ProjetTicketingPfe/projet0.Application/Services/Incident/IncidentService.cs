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
using static projet0.Application.Commun.DTOs.Incident.IncidentDTO;
using IncidentEntity = projet0.Domain.Entities.Incident;
using TicketEntity = projet0.Domain.Entities.Ticket;

namespace projet0.Application.Services.Incident
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
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
        private readonly IPieceJointeRepository _pieceJointeRepository;
        private readonly IIncidentArchiveRepository _incidentArchiveRepository;
        private readonly INotificationRepository _notificationRepository;  // ← AJOUTER

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
            IPieceJointeRepository pieceJointeRepository,
            INotificationService notificationService,
            INotificationRepository notificationRepository,
            ICommentaireRepository commentaireRepository,
            IIncidentArchiveRepository incidentArchiveRepository  // Ajouter ceci
)
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
            _notificationService = notificationService;
            _incidentArchiveRepository = incidentArchiveRepository;
            _notificationRepository = notificationRepository;  // ← AJOUTER

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
            dto.SeveriteIncidentLibelle = GetSeveriteLibelle(incident.SeveriteIncident);
            dto.Emplacement = incident.Emplacement;

            if (incident.CreatedById.HasValue && dto.CreatedByName == null)
            {
                var user = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                dto.CreatedByName = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
            }

            dto.TypeProbleme = incident.TypeProbleme;

            // MAPPAGE DES ENTITÉS IMPACTÉES
            if (incident.EntitesImpactees != null && incident.EntitesImpactees.Any())
            {
                dto.EntitesImpactees = incident.EntitesImpactees
                    .Select(e => new EntiteImpacteeDTO
                    {
                        Id = e.Id,
                        TypeEntiteImpactee = e.TypeEntiteImpactee,
                    })
                    .ToList();
            }
            else
            {
                dto.EntitesImpactees = new List<EntiteImpacteeDTO>();
            }

            // ✅ AJOUTER LE MAPPAGE DES TICKETS
            if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
            {
                // Filtrer les tickets non archivés
                var activeTickets = incident.IncidentTickets
                    .Where(it => it.Ticket != null )
                    .ToList();

                dto.TicketCount = activeTickets.Count;

                dto.Tickets = activeTickets
                    .Select(it => new IncidentTicketInfoDTO
                    {
                        TicketId = it.TicketId,
                        ReferenceTicket = it.Ticket.ReferenceTicket,
                        TitreTicket = it.Ticket.TitreTicket,
                        StatutTicket = it.Ticket.StatutTicket.ToString()
                    })
                    .ToList();
            }
            else
            {
                dto.TicketCount = 0;
                dto.Tickets = new List<IncidentTicketInfoDTO>();
            }

            return dto;
        }
        public async Task<ApiResponse<CommercantIncidentDashboardDTO>> GetCommercantDashboardAsync(Guid commercantId)
        {
            return await MeasureAsync(nameof(GetCommercantDashboardAsync), null, async () =>
            {
                try
                {
                    _logger.LogInformation("Récupération du dashboard pour le commerçant {CommercantId}", commercantId);

                    // Récupérer les IDs des incidents archivés par ce commerçant
                    var archivedIncidentIds = await _incidentArchiveRepository
                        .GetArchivedIncidentIdsByUserAsync(commercantId);

                    // Récupérer tous les incidents du commerçant
                    var query = _incidentRepository.QueryWithDetails();
                    query = query.Where(i => i.CreatedById == commercantId);
                    var allIncidents = await query.ToListAsync();

                    // Séparer incidents non archivés et archivés
                    var incidentsNonArchives = allIncidents.Where(i => !archivedIncidentIds.Contains(i.Id)).ToList();
                    var incidentsArchives = allIncidents.Where(i => archivedIncidentIds.Contains(i.Id)).ToList();
                    var incidentsNonArchivesList = incidentsNonArchives.ToList();

                    // ============================================
                    // 1. STATISTIQUES GLOBALES
                    // ============================================
                    var total = incidentsNonArchivesList.Count;
                    var nonTraite = incidentsNonArchivesList.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite);
                    var enCours = incidentsNonArchivesList.Count(i => i.StatutIncident == StatutIncident.EnCours);
                    var ferme = incidentsNonArchivesList.Count(i => i.StatutIncident == StatutIncident.Ferme);
                    var archiveCount = incidentsArchives.Count;
                    var nonArchiveCount = incidentsNonArchivesList.Count;

                    // Incident résolus = Fermé
                    var resolus = ferme;
                    var tauxResolution = total > 0 ? Math.Round((double)resolus / total * 100, 1) : 0;

                    var overview = new CommercantIncidentOverviewDTO
                    {
                        TotalIncidents = total,
                        IncidentsNonTraite = nonTraite,
                        IncidentsEnCours = enCours,
                        IncidentsFerme = ferme,
                        IncidentsArchives = archiveCount,
                        IncidentsNonArchive = nonArchiveCount,
                        TauxNonTraite = total > 0 ? Math.Round((double)nonTraite / total * 100, 1) : 0,
                        TauxEnCours = total > 0 ? Math.Round((double)enCours / total * 100, 1) : 0,
                        TauxFerme = total > 0 ? Math.Round((double)ferme / total * 100, 1) : 0,
                        TauxResolution = tauxResolution
                    };

                    // ============================================
                    // 2. STATISTIQUES PAR STATUT
                    // ============================================
                    var statsParStatut = new List<IncidentStatutStatDTO>
            {
                new IncidentStatutStatDTO
                {
                    Statut = "Non traité",
                    Count = nonTraite,
                    Color = "#ffc107",
                    Pourcentage = total > 0 ? Math.Round((double)nonTraite / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "En cours",
                    Count = enCours,
                    Color = "#17a2b8",
                    Pourcentage = total > 0 ? Math.Round((double)enCours / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "Fermé",
                    Count = ferme,
                    Color = "#28a745",
                    Pourcentage = total > 0 ? Math.Round((double)ferme / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "Archivé",
                    Count = archiveCount,
                    Color = "#6c757d",
                    Pourcentage = allIncidents.Count > 0 ? Math.Round((double)archiveCount / allIncidents.Count * 100, 1) : 0
                }
            };

                    // ============================================
                    // 3. STATISTIQUES PAR JOUR (derniers 7 jours)
                    // ============================================
                    var statsParJour = new List<IncidentJournalierDTO>();
                    var today = DateTime.Today;

                    for (int i = 6; i >= 0; i--)
                    {
                        var date = today.AddDays(-i);
                        var incidentsDuJour = incidentsNonArchivesList
                            .Where(i => i.DateDetection.Date == date)
                            .ToList();

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
                    // 4. STATISTIQUES PAR SEMAINE (dernières 4 semaines)
                    // ============================================
                    var statsParSemaine = new List<IncidentJournalierDTO>();
                    var currentYear = DateTime.Today.Year;
                    var firstDayOfYear = new DateTime(currentYear, 1, 1);
                    var startOfFirstWeek = firstDayOfYear;

                    while (startOfFirstWeek.DayOfWeek != DayOfWeek.Monday)
                    {
                        startOfFirstWeek = startOfFirstWeek.AddDays(1);
                    }

                    var weeksCount = 4;

                    for (int weekNumber = weeksCount; weekNumber >= 1; weekNumber--)
                    {
                        var debutSemaine = startOfFirstWeek.AddDays((weekNumber - 1) * 7);
                        var finSemaine = debutSemaine.AddDays(6);

                        var incidentsSemaine = incidentsNonArchivesList
                            .Where(i => i.DateDetection.Date >= debutSemaine && i.DateDetection.Date <= finSemaine)
                            .ToList();

                        statsParSemaine.Add(new IncidentJournalierDTO
                        {
                            Date = debutSemaine,
                            Crees = incidentsSemaine.Count,
                            NonTraite = incidentsSemaine.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    statsParSemaine = statsParSemaine.OrderBy(s => s.Date).ToList();

                    // ============================================
                    // 5. STATISTIQUES PAR MOIS (derniers 6 mois)
                    // ============================================
                    var statsParMois = new List<IncidentJournalierDTO>();

                    for (int i = 5; i >= 0; i--)
                    {
                        var dateMois = today.AddMonths(-i);
                        var debutMois = new DateTime(dateMois.Year, dateMois.Month, 1);
                        var finMois = debutMois.AddMonths(1).AddDays(-1);

                        var incidentsMois = incidentsNonArchivesList
                            .Where(i => i.DateDetection.Date >= debutMois && i.DateDetection.Date <= finMois)
                            .ToList();

                        statsParMois.Add(new IncidentJournalierDTO
                        {
                            Date = debutMois,
                            Crees = incidentsMois.Count,
                            NonTraite = incidentsMois.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsMois.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsMois.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    // ============================================
                    // 6. STATISTIQUES PAR TYPE DE PROBLÈME
                    // ============================================
                    var resolutionParTypeProbleme = new List<ResolutionParTypeProblemeDTO>();
                    var incidentsResolus = incidentsNonArchivesList.Where(i => i.StatutIncident == StatutIncident.Ferme && i.DateResolution.HasValue).ToList();

                    foreach (TypeProbleme type in Enum.GetValues(typeof(TypeProbleme)))
                    {
                        var incidentsType = incidentsNonArchivesList.Where(i => i.TypeProbleme == type).ToList();
                        var incidentsResolusType = incidentsType.Where(i => i.StatutIncident == StatutIncident.Ferme && i.DateResolution.HasValue).ToList();
                        var tempsResolution = incidentsResolusType
                            .Select(i => (i.DateResolution.Value - i.DateDetection).TotalHours)
                            .ToList();

                        double moyenneHeures = tempsResolution.Any() ? Math.Round(tempsResolution.Average(), 1) : 0;
                        double moyenneJours = Math.Round(moyenneHeures / 24, 1);

                        resolutionParTypeProbleme.Add(new ResolutionParTypeProblemeDTO
                        {
                            TypeProbleme = GetTypeProblemeLabel(type),
                            TypeProblemeEnum = type,
                            NombreIncidents = incidentsType.Count,
                            NombreResolus = incidentsResolusType.Count,
                            TempsMoyenResolutionHeures = moyenneHeures,
                            TempsMoyenResolutionJours = moyenneJours,
                            TauxResolution = incidentsType.Count > 0
                                ? Math.Round((double)incidentsResolusType.Count / incidentsType.Count * 100, 1)
                                : 0,
                            PourcentageTotal = total > 0
                                ? Math.Round((double)incidentsType.Count / total * 100, 1)
                                : 0,
                            Color = GetColorForTypeProbleme(type)
                        });
                    }

                    resolutionParTypeProbleme = resolutionParTypeProbleme
                        .OrderByDescending(t => t.NombreIncidents)
                        .ToList();

                    var dashboard = new CommercantIncidentDashboardDTO
                    {
                        Overview = overview,
                        StatsParStatut = statsParStatut,
                        StatsParJour = statsParJour,
                        StatsParSemaine = statsParSemaine,
                        StatsParMois = statsParMois,
                        ResolutionParTypeProbleme = resolutionParTypeProbleme
                    };

                    _logger.LogInformation("Dashboard commerçant généré - Total: {Total}, Résolus: {Resolus}",
                        total, ferme);

                    return ApiResponse<CommercantIncidentDashboardDTO>.Success(dashboard);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la génération du dashboard pour le commerçant");
                    return ApiResponse<CommercantIncidentDashboardDTO>.Failure("Erreur interne du serveur");
                }
            });
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

            // FILTRE PAR STATUT
            if (request.StatutIncident.HasValue)
            {
                if (request.StatutIncident.Value == StatutIncident.NonTraite)
                {
                    query = query.Where(i => i.StatutIncident == null);
                }
                else
                {
                    query = query.Where(i => i.StatutIncident == request.StatutIncident.Value);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.StatutLibelle))
            {
                switch (request.StatutLibelle.ToLower().Trim())
                {
                    case "nontraite":
                    case "non traité":
                    case "non-traite":
                        query = query.Where(i => i.StatutIncident == null);
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

            // FILTRE PAR SÉVÉRITÉ
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

            // ✅ NOUVEAU: FILTRE PAR ENTITÉ IMPACTÉE
            // Filtre sur la table EntitesImpactees
            if (request.EntiteImpactee.HasValue)
            {
                var entiteValue = request.EntiteImpactee.Value;
                query = query.Where(i => i.EntitesImpactees != null &&
                                         i.EntitesImpactees.Any(e => e.TypeEntiteImpactee == entiteValue));
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

            // Filtre par date de détection
            if (request.DateDetection.HasValue)
            {
                var date = request.DateDetection.Value.Date;
                var nextDay = date.AddDays(1);
                query = query.Where(i => i.DateDetection >= date && i.DateDetection < nextDay);
            }

            // Filtre par date de résolution
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

        // Application/Services/Incident/IncidentService.cs
        /// <summary>
        /// Archive un incident résolu
        /// </summary>
        // Application/Services/Incident/IncidentService.cs

        /// <summary>
        /// Archive un incident résolu
        /// </summary>
        public async Task<ApiResponse<IncidentArchiveDTO>> ArchiverIncidentAsync(Guid incidentId, Guid userId)
        {
            return await MeasureAsync(nameof(ArchiverIncidentAsync), new { incidentId }, async () =>
            {
                try
                {
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<IncidentArchiveDTO>.Failure("Incident non trouvé");

                    // Vérifier que l'incident est résolu (FERME)
                    if (incident.StatutIncident != StatutIncident.Ferme)
                    {
                        return ApiResponse<IncidentArchiveDTO>.Failure(
                            "Seuls les incidents résolus (statut FERME) peuvent être archivés.",
                            resultCode: 80
                        );
                    }

                    // Vérifier si l'utilisateur a déjà archivé cet incident
                    var dejaArchive = await _incidentArchiveRepository.ExistsAsync(incidentId, userId);
                    if (dejaArchive)
                    {
                        return ApiResponse<IncidentArchiveDTO>.Failure(
                            "Vous avez déjà archivé cet incident.",
                            resultCode: 81
                        );
                    }

                    // Créer l'archive
                    var archive = new IncidentArchive
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incidentId,
                        ArchiveParId = userId,
                        DateArchivage = DateTime.UtcNow
                    };

                    await _incidentArchiveRepository.AddAsync(archive);
                    await _incidentArchiveRepository.SaveChangesAsync();

                    var archiveur = await _userRepository.GetByIdAsync(userId);
                    string archiveurNom = archiveur != null ? $"{archiveur.Nom} {archiveur.Prenom}" : "Inconnu";

                    var dto = new IncidentArchiveDTO
                    {
                        IncidentId = incident.Id,
                        CodeIncident = incident.CodeIncident,
                        EstArchive = true,
                        DateArchivage = archive.DateArchivage,
                        ArchivePar = archiveurNom
                    };

                    _logger.LogInformation("Incident {CodeIncident} archivé par {Archiveur}",
                        incident.CodeIncident, archiveurNom);

                    return ApiResponse<IncidentArchiveDTO>.Success(dto, "Incident archivé avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'archivage de l'incident {IncidentId}", incidentId);
                    return ApiResponse<IncidentArchiveDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        /// <summary>
        /// Restaure un incident archivé (supprime l'archive)
        /// </summary>


        /// <summary>
        /// Récupère les incidents archivés par l'utilisateur connecté
        /// </summary>

        /// <summary>
        /// Restaure un incident archivé
        /// </summary>
        /// <summary>
        /// Restaure un incident archivé (supprime l'archive de la table IncidentArchives)
        /// </summary>
        public async Task<ApiResponse<IncidentArchiveDTO>> RestaurerIncidentAsync(Guid incidentId, Guid userId)
        {
            return await MeasureAsync(nameof(RestaurerIncidentAsync), new { incidentId }, async () =>
            {
                try
                {
                    // Récupérer l'archive dans la table IncidentArchives
                    var archive = await _incidentArchiveRepository.GetByIncidentAndUserAsync(incidentId, userId);

                    if (archive == null)
                    {
                        return ApiResponse<IncidentArchiveDTO>.Failure(
                            "Cet incident n'est pas archivé par vous.",
                            resultCode: 83
                        );
                    }

                    // Supprimer l'archive de la table
                    await _incidentArchiveRepository.DeleteAsync(archive);
                    await _incidentArchiveRepository.SaveChangesAsync();

                    var dto = new IncidentArchiveDTO
                    {
                        IncidentId = incidentId,
                        EstArchive = false,
                        DateArchivage = null,
                        ArchivePar = null
                    };

                    _logger.LogInformation("Incident {IncidentId} restauré par {UserId}", incidentId, userId);

                    return ApiResponse<IncidentArchiveDTO>.Success(dto, "Incident restauré avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la restauration de l'incident {IncidentId}", incidentId);
                    return ApiResponse<IncidentArchiveDTO>.Failure("Erreur interne du serveur");
                }
            });
        }
        /// <summary>
        /// Récupère les incidents archivés par l'utilisateur connecté (paginated)
        /// Chaque utilisateur ne voit que ses propres archives
        /// </summary>
        public async Task<ApiResponse<PagedResult<IncidentDTO>>> GetMyArchivesPagedAsync(
       IncidentSearchRequest request,
       Guid userId)
        {
            return await MeasureAsync(nameof(GetMyArchivesPagedAsync), request, async () =>
            {
                try
                {
                    // Récupérer les IDs des incidents que l'utilisateur a archivés
                    var archivedIncidentIds = await _incidentArchiveRepository
                        .GetArchivedIncidentIdsByUserAsync(userId);

                    if (!archivedIncidentIds.Any())
                    {
                        return ApiResponse<PagedResult<IncidentDTO>>.Success(
                            new PagedResult<IncidentDTO> { Items = new List<IncidentDTO>(), TotalCount = 0 });
                    }

                    var query = _incidentRepository.QueryWithDetails();
                    query = query.Where(i => archivedIncidentIds.Contains(i.Id));

                    // Appliquer les autres filtres...
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
                    _logger.LogError(ex, "Erreur lors de la récupération des archives pour l'utilisateur {UserId}", userId);
                    return ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }

        /// <summary>
        /// Récupère les incidents archivés par l'utilisateur connecté
        /// </summary>
        /// <summary>
        /// Récupère les incidents archivés par l'utilisateur connecté
        /// </summary>
        public async Task<ApiResponse<PagedResult<IncidentDTO>>> GetIncidentsArchivesPagedAsync(
         IncidentSearchRequest request,
         Guid userId)
        {
            return await MeasureAsync(nameof(GetIncidentsArchivesPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogWarning("=== DÉBUT GetIncidentsArchivesPagedAsync ===");
                    _logger.LogWarning("UserId reçu: {UserId}", userId);

                    // ✅ 1. Récupérer TOUTES les archives avec leurs dates
                    var archives = await _incidentArchiveRepository.GetArchivesByUserAsync(userId);
                    _logger.LogWarning("Archives trouvées: {Count}", archives.Count);

                    // ✅ 2. Créer un dictionnaire IncidentId -> DateArchivage (gérer les doublons)
                    var archiveDict = archives
                        .GroupBy(a => a.IncidentId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Max(a => a.DateArchivage)  // Prend la date la plus récente
                        );

                    var archivedIncidentIds = archiveDict.Keys.ToList();

                    if (!archivedIncidentIds.Any())
                    {
                        _logger.LogWarning("Aucun incident archivé trouvé");
                        return ApiResponse<PagedResult<IncidentDTO>>.Success(
                            new PagedResult<IncidentDTO> { Items = new List<IncidentDTO>(), TotalCount = 0 });
                    }

                    var query = _incidentRepository.QueryWithDetails();
                    query = query.Where(i => archivedIncidentIds.Contains(i.Id));

                    // Appliquer les filtres
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

                    query = ApplySearchFilters(query, request, matchedUserIds);
                    var totalCount = await query.CountAsync();
                    _logger.LogWarning("TotalCount après filtre: {TotalCount}", totalCount);

                    query = ApplySorting(query, request.SortBy, request.SortDescending);

                    var pagedIncidents = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    var dtos = new List<IncidentDTO>();
                    foreach (var incident in pagedIncidents)
                    {
                        var dto = await MapToDto(incident);
                        // ✅ 3. AJOUTER LA DATE D'ARCHIVAGE
                        if (archiveDict.TryGetValue(incident.Id, out var dateArchivage))
                        {
                            dto.DateArchivage = dateArchivage;
                            _logger.LogWarning("DateArchivage assignée pour incident {IncidentId}: {Date}", incident.Id, dateArchivage);
                        }
                        dtos.Add(dto);
                    }

                    var result = new PagedResult<IncidentDTO>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize
                    };

                    _logger.LogWarning("=== FIN GetIncidentsArchivesPagedAsync ===");
                    return ApiResponse<PagedResult<IncidentDTO>>.Success(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des archives pour l'utilisateur {UserId}", userId);
                    return ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
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

        // Dans IncidentService.cs - UNE SEULE MÉTHODE
        public async Task<ApiResponse<List<IncidentDTO>>> GetAllIncidentsAsync(Guid? userId = null)
        {
            return await MeasureAsync(nameof(GetAllIncidentsAsync), null, async () =>
            {
                try
                {
                    var incidents = await _incidentRepository.GetAllAsync();
                    var incidentsList = incidents.ToList();

                    // Si un userId est fourni, filtrer les incidents archivés par cet utilisateur
                    if (userId.HasValue)
                    {
                        var archivedIncidentIds = await _incidentArchiveRepository
                            .GetArchivedIncidentIdsByUserAsync(userId.Value);

                        if (archivedIncidentIds.Any())
                        {
                            incidentsList = incidentsList
                                .Where(i => !archivedIncidentIds.Contains(i.Id))
                                .ToList();
                        }
                    }

                    var dtos = new List<IncidentDTO>();
                    foreach (var incident in incidentsList)
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

        public async Task<ApiResponse<PagedResult<IncidentDTO>>> SearchIncidentsAsync(
    IncidentSearchRequest request,
    Guid userId)  // AJOUTER userId en paramètre
        {
            _logger.LogWarning("SORT PARAM - SortBy: {SortBy}, Descending: {Descending}",
                request.SortBy, request.SortDescending);

            return await MeasureAsync(nameof(SearchIncidentsAsync), request, async () =>
            {
                try
                {
                    // 1. Récupérer les IDs des incidents archivés par l'utilisateur
                    var archivedIncidentIds = await _incidentArchiveRepository
                        .GetArchivedIncidentIdsByUserAsync(userId);

                    var query = _incidentRepository.QueryWithDetails();

                    // 2. EXCLURE les incidents archivés par cet utilisateur
                    if (archivedIncidentIds.Any())
                    {
                        query = query.Where(i => !archivedIncidentIds.Contains(i.Id));
                    }

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

                    // Appliquer tous les filtres
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
                    StatutIncident = StatutIncident.NonTraite,  // AUCUN STATUT À LA CRÉATION
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

                // ======================================================
                // 🔔 NOTIFICATIONS POUR CRÉATION D'INCIDENT
                // ======================================================

                // 1. Notification aux ADMINS
                var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateIncidentNotificationAsync(
                        admin.Id,
                        incident.Id,
                        TypeNotification.IncidentCree,
                        $"Nouvel incident créé : {code}",
                        $"Un nouvel incident de type '{dto.TypeProbleme}' a été créé par {createur.Nom} {createur.Prenom}."
                    );
                }

              

                _logger.LogInformation("Notifications envoyées pour l'incident {IncidentId}", incident.Id);
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
        private List<string> GetModificationsList(UpdateIncidentDTO dto, IncidentEntity incident)
        {
            var modifications = new List<string>();

            // Description
            if (!string.IsNullOrWhiteSpace(dto.DescriptionIncident) && dto.DescriptionIncident != incident.DescriptionIncident)
            {
                modifications.Add($"Description (ancien: '{incident.DescriptionIncident?.Substring(0, Math.Min(20, incident.DescriptionIncident?.Length ?? 0))}...')");
            }

            // Emplacement
            if (!string.IsNullOrWhiteSpace(dto.Emplacement) && dto.Emplacement != incident.Emplacement)
            {
                modifications.Add($"Emplacement (ancien: '{incident.Emplacement}')");
            }

            // Type de problème
            if (dto.TypeProbleme.HasValue && dto.TypeProbleme.Value != incident.TypeProbleme)
            {
                modifications.Add($"Type de problème (ancien: {incident.TypeProbleme})");
            }

            // Sévérité (admin seulement, mais on la détecte quand même)
            if (dto.SeveriteIncident.HasValue && dto.SeveriteIncident.Value != incident.SeveriteIncident)
            {
                modifications.Add($"Sévérité (ancien: {incident.SeveriteIncident})");
            }

            return modifications;
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
                    if (incident.StatutIncident == StatutIncident.EnCours ||
                        incident.StatutIncident == StatutIncident.Ferme)
                    {
                        return ApiResponse<IncidentDTO>.Failure(
                            "Vous ne pouvez pas modifier un incident qui est déjà en cours ou fermé.",
                            resultCode: 70
                        );
                    }

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

                // ⚠️ IMPORTANT: Calculer les modifications AVANT de modifier l'incident
                var modifications = GetModificationsList(dto, incident);
                _logger.LogInformation("Modifications détectées: {Modifications}", string.Join(", ", modifications));

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

                // Mise à jour des autres champs
                if (!string.IsNullOrWhiteSpace(dto.DescriptionIncident))
                    incident.DescriptionIncident = dto.DescriptionIncident;

                if (!string.IsNullOrWhiteSpace(dto.Emplacement))
                    incident.Emplacement = dto.Emplacement;

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
                    var entiteImpactee = incident.EntitesImpactees?.FirstOrDefault();
                    if (entiteImpactee != null)
                    {
                        entiteImpactee.TypeEntiteImpactee = nouveauTypeEntiteImpactee.Value;
                        _logger.LogInformation("Entité impactée mise à jour de {AncienType} à {NouveauType}",
                            entiteImpactee.TypeEntiteImpactee, nouveauTypeEntiteImpactee.Value);
                    }
                    else
                    {
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

                // ======================================================
                // 🔔 NOTIFICATIONS POUR MODIFICATION D'INCIDENT
                // ======================================================

                // Si des modifications ont été faites, envoyer les notifications
                if (modifications.Any())
                {
                    var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                    var modificateur = await _userRepository.GetByIdAsync(userId);
                    string modificateurNom = modificateur != null ? $"{modificateur.Nom} {modificateur.Prenom}" : "Un utilisateur";
                    string modificateurRole = isAdmin ? "administrateur" : "commerçant";

                    _logger.LogInformation("Envoi des notifications aux admins pour les modifications: {Modifications}", string.Join(", ", modifications));

                    foreach (var admin in admins)
                    {
                        // Exclure l'admin si c'est lui qui a fait la modification
                        if (!(isAdmin && admin.Id == userId))
                        {
                            await _notificationService.CreateIncidentNotificationAsync(
                                admin.Id,
                                incident.Id,
                                TypeNotification.IncidentModifie,
                                $"Incident modifié : {incident.CodeIncident}",
                                $"{modificateurNom} a modifié l'incident '{incident.CodeIncident}'. Modifications: {string.Join(", ", modifications)}"
                            );
                        }
                    }

                    _logger.LogInformation("Notifications envoyées aux admins pour la modification de l'incident {IncidentId} par {Role}",
                        incident.Id, modificateurRole);
                }
                else
                {
                    _logger.LogInformation("Aucune modification détectée pour l'incident {IncidentId}", incident.Id);
                }

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
                        // TOUS les tickets sont résolus → incident fermé
                        incident.StatutIncident = StatutIncident.Ferme;
                        incident.DateResolution = DateTime.UtcNow;
                        _logger.LogInformation("Incident {IncidentId} fermé car tous ses tickets sont résolus", incidentId);
                    }
                    else if (aUnTicketEnCours)
                    {
                        // Au moins un ticket en cours → incident en cours
                        incident.StatutIncident = StatutIncident.EnCours;
                        incident.DateResolution = null;
                        _logger.LogInformation("Incident {IncidentId} en cours", incidentId);
                    }
                    else if (aUnTicketAssigne && !aUnTicketEnCours)
                    {
                        // Tickets assignés mais aucun en cours → incident reste en cours
                        incident.StatutIncident = StatutIncident.EnCours;
                        incident.DateResolution = null;
                    }
                }
                else
                {
                    // Plus aucun ticket lié → incident sans statut
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

        // Dans IncidentService.cs - Remplacer la méthode DeleteIncidentAsync

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

                // Règles pour le commerçant
                if (isCommercant && !isAdmin)
                {
                    if (incident.CreatedById != userId)
                    {
                        return ApiResponse<bool>.Failure("Vous ne pouvez supprimer que vos propres incidents.", resultCode: 72);
                    }

                    if (incident.StatutIncident == StatutIncident.EnCours)
                    {
                        return ApiResponse<bool>.Failure(
                            "Impossible de supprimer un incident en cours de traitement.",
                            resultCode: 48
                        );
                    }
                }

                using var transaction = await _incidentRepository.BeginTransactionAsync();

                try
                {
                    // 1. Supprimer les liaisons IncidentTPE
                    if (incident.IncidentTPEs != null && incident.IncidentTPEs.Any())
                    {
                        foreach (var lien in incident.IncidentTPEs.ToList())
                        {
                            await _incidentTPERepository.DeleteAsync(lien);
                        }
                    }

                    // 2. Récupérer les tickets liés AVANT de supprimer les liaisons
                    var ticketsLies = new List<TicketEntity>();
                    if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                    {
                        ticketsLies = incident.IncidentTickets
                            .Select(it => it.Ticket)
                            .Where(t => t != null)
                            .ToList();

                        // Supprimer les liaisons IncidentTicket
                        foreach (var lien in incident.IncidentTickets.ToList())
                        {
                            await _incidentTicketRepository.DeleteAsync(lien);
                        }
                    }

                    // 3. Supprimer les notifications liées à l'incident
                    var notifications = await _notificationRepository.GetByIncidentIdAsync(id);
                    foreach (var notification in notifications)
                    {
                        await _notificationRepository.DeleteAsync(notification);
                    }

                    // 4. Supprimer l'incident
                    await _incidentRepository.DeleteAsync(incident);
                    await _incidentRepository.SaveChangesAsync();

                    // 5. Pour chaque ticket qui n'a plus d'incidents, supprimer SES notifications d'abord
                    foreach (var ticket in ticketsLies)
                    {
                        var incidentsRestants = await _incidentTicketRepository.GetIncidentsByTicketIdAsync(ticket.Id);

                        if (!incidentsRestants.Any())
                        {
                            _logger.LogInformation("Ticket {TicketId} n'a plus d'incidents liés - suppression", ticket.Id);

                            // ✅ NOUVEAU : Supprimer les notifications liées au ticket AVANT de supprimer le ticket
                            var ticketNotifications = await _notificationRepository.GetByTicketIdAsync(ticket.Id);
                            foreach (var notif in ticketNotifications)
                            {
                                await _notificationRepository.DeleteAsync(notif);
                            }

                            // Supprimer les commentaires et leurs pièces jointes
                            if (ticket.Commentaires != null && ticket.Commentaires.Any())
                            {
                                foreach (var commentaire in ticket.Commentaires.ToList())
                                {
                                    if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                                    {
                                        foreach (var piece in commentaire.PiecesJointes.ToList())
                                        {
                                            await _pieceJointeService.SupprimerFichierAsync(piece.Id);
                                        }
                                    }
                                    await _commentaireRepository.DeleteAsync(commentaire);
                                }
                            }

                            await _ticketRepository.DeleteAsync(ticket);
                        }
                    }

                    await _incidentRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    sw.Stop();
                    _logger.LogInformation("DeleteIncident SUCCESS | Id: {Id} | Duration: {Ms} ms",
                        id, sw.ElapsedMilliseconds);

                    return ApiResponse<bool>.Success(true, "Incident supprimé avec succès");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "DeleteIncident ERROR | Id: {Id} | Duration: {Ms} ms", id, sw.ElapsedMilliseconds);
                return ApiResponse<bool>.Failure("Erreur interne du serveur");
            }
        }

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
                    // 1. STATISTIQUES GLOBALES (existantes)
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
                    // 2. STATISTIQUES PAR STATUT (existantes)
                    // ============================================
                    var statsParStatut = new List<IncidentStatutStatDTO>
            {
                new IncidentStatutStatDTO
                {
                    Statut = "Non traité",
                    Count = nonTraite,
                    Color = "#ffc107",
                    Pourcentage = total > 0 ? Math.Round((double)nonTraite / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "En cours",
                    Count = enCours,
                    Color = "#17a2b8",
                    Pourcentage = total > 0 ? Math.Round((double)enCours / total * 100, 1) : 0
                },
                new IncidentStatutStatDTO
                {
                    Statut = "Fermé",
                    Count = ferme,
                    Color = "#28a745",
                    Pourcentage = total > 0 ? Math.Round((double)ferme / total * 100, 1) : 0
                }
            };

                    // ============================================
                    // 3. STATISTIQUES DE RÉSOLUTION GLOBALES (NOUVEAU)
                    // ============================================
                    var incidentsResolus = incidentsList.Where(i => i.StatutIncident == StatutIncident.Ferme && i.DateResolution.HasValue).ToList();
                    var (moyenneHeuresGlobal, moyenneJoursGlobal, nbResolus) = CalculerTempsMoyenResolution(incidentsResolus);

                    var statsResolution = new ResolutionIncidentStatsDTO
                    {
                        TempsMoyenResolutionHeures = moyenneHeuresGlobal,
                        TempsMoyenResolutionJours = moyenneJoursGlobal,
                        IncidentsResolus = nbResolus,
                        IncidentsNonResolus = total - nbResolus,
                        TauxResolution = total > 0 ? Math.Round((double)nbResolus / total * 100, 1) : 0
                    };

                    // ============================================
                    // 4. TEMPS MOYEN PAR SÉVÉRITÉ (NOUVEAU)
                    // ============================================
                    var resolutionParSeverite = new List<ResolutionParSeveriteDTO>();

                    foreach (SeveriteIncident severite in Enum.GetValues(typeof(SeveriteIncident)))
                    {
                        var incidentsSeverite = incidentsList.Where(i => i.SeveriteIncident == severite).ToList();
                        var incidentsResolusSeverite = incidentsSeverite.Where(i => i.StatutIncident == StatutIncident.Ferme && i.DateResolution.HasValue).ToList();
                        var (moyenneHeures, moyenneJours, _) = CalculerTempsMoyenResolution(incidentsResolusSeverite);

                        resolutionParSeverite.Add(new ResolutionParSeveriteDTO
                        {
                            Severite = GetSeveriteLabel(severite),
                            NombreIncidents = incidentsSeverite.Count,
                            NombreResolus = incidentsResolusSeverite.Count,
                            TempsMoyenResolutionHeures = moyenneHeures,
                            TempsMoyenResolutionJours = moyenneJours,
                            TauxResolution = incidentsSeverite.Count > 0
                                ? Math.Round((double)incidentsResolusSeverite.Count / incidentsSeverite.Count * 100, 1)
                                : 0,
                            Color = GetColorForSeverite(severite)
                        });
                    }

                    // ============================================
                    // 5. TEMPS MOYEN PAR TYPE DE PROBLÈME + POURCENTAGE (NOUVEAU)
                    // ============================================
                    var resolutionParTypeProbleme = new List<ResolutionParTypeProblemeDTO>();

                    foreach (TypeProbleme type in Enum.GetValues(typeof(TypeProbleme)))
                    {
                        var incidentsType = incidentsList.Where(i => i.TypeProbleme == type).ToList();
                        var incidentsResolusType = incidentsType.Where(i => i.StatutIncident == StatutIncident.Ferme && i.DateResolution.HasValue).ToList();
                        var (moyenneHeures, moyenneJours, _) = CalculerTempsMoyenResolution(incidentsResolusType);

                        resolutionParTypeProbleme.Add(new ResolutionParTypeProblemeDTO
                        {
                            TypeProbleme = GetTypeProblemeLabel(type),
                            TypeProblemeEnum = type,
                            NombreIncidents = incidentsType.Count,
                            NombreResolus = incidentsResolusType.Count,
                            TempsMoyenResolutionHeures = moyenneHeures,
                            TempsMoyenResolutionJours = moyenneJours,
                            TauxResolution = incidentsType.Count > 0
                                ? Math.Round((double)incidentsResolusType.Count / incidentsType.Count * 100, 1)
                                : 0,
                            PourcentageTotal = total > 0
                                ? Math.Round((double)incidentsType.Count / total * 100, 1)
                                : 0,
                            Color = GetColorForTypeProbleme(type)
                        });
                    }

                    // Trier par nombre d'incidents (décroissant)
                    resolutionParTypeProbleme = resolutionParTypeProbleme
                        .OrderByDescending(t => t.NombreIncidents)
                        .ToList();

                    // ============================================
                    // 6. STATISTIQUES PAR JOUR (existantes)
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
                    // 7. STATISTIQUES PAR SEMAINE (ANNÉE COMPLÈTE)
                    // ============================================
                    var statsParSemaine = new List<IncidentJournalierDTO>();

                    // Récupérer l'année actuelle
                    var currentYear = DateTime.Today.Year;

                    // Déterminer la première semaine de l'année (semaine 1)
                    var firstDayOfYear = new DateTime(currentYear, 1, 1);

                    // Trouver le premier lundi de l'année
                    var startOfFirstWeek = firstDayOfYear;
                    while (startOfFirstWeek.DayOfWeek != DayOfWeek.Monday)
                    {
                        startOfFirstWeek = startOfFirstWeek.AddDays(1);  // ✅ CORRECTION: startOfFirstWeek au lieu de startOfYear
                    }

                    // Ajuster si le premier lundi est après le 7 janvier (semaine 1 de l'année ISO)
                    if (startOfFirstWeek > firstDayOfYear.AddDays(7))
                    {
                        startOfFirstWeek = startOfFirstWeek.AddDays(-7);
                    }

                    // Déterminer la dernière semaine de l'année
                    var lastDayOfYear = new DateTime(currentYear, 12, 31);
                    var endOfLastWeek = lastDayOfYear;
                    while (endOfLastWeek.DayOfWeek != DayOfWeek.Sunday)
                    {
                        endOfLastWeek = endOfLastWeek.AddDays(1);
                    }

                    // Calculer le nombre de semaines dans l'année
                    var weeksCount = (int)Math.Ceiling((endOfLastWeek - startOfFirstWeek).TotalDays / 7);

                    // Générer les statistiques pour chaque semaine de l'année
                    for (int weekNumber = 1; weekNumber <= weeksCount; weekNumber++)
                    {
                        // Calculer le début et la fin de la semaine
                        var debutSemaine = startOfFirstWeek.AddDays((weekNumber - 1) * 7);
                        var finSemaine = debutSemaine.AddDays(6);

                        // Vérifier si la semaine est dans l'année courante
                        if (debutSemaine.Year > currentYear) break;

                        // Filtrer les incidents de cette semaine
                        var incidentsSemaine = incidentsList
                            .Where(i => i.DateDetection.Date >= debutSemaine && i.DateDetection.Date <= finSemaine)
                            .ToList();

                        statsParSemaine.Add(new IncidentJournalierDTO
                        {
                            Date = debutSemaine,
                            Crees = incidentsSemaine.Count,
                            NonTraite = incidentsSemaine.Count(i => i.StatutIncident == null || i.StatutIncident == StatutIncident.NonTraite),
                            EnCours = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.EnCours),
                            Ferme = incidentsSemaine.Count(i => i.StatutIncident == StatutIncident.Ferme)
                        });
                    }

                    // Ordonner par date
                    statsParSemaine = statsParSemaine.OrderBy(s => s.Date).ToList();
                    // ============================================
                    // 8. STATISTIQUES PAR MOIS (existantes)
                    // ============================================
                    var statsParMois = new List<IncidentJournalierDTO>();

                    for (int i = 5; i >= 0; i--)
                    {
                        var dateMois = today.AddMonths(-i);
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

                    // ============================================
                    // 9. DASHBOARD COMPLET
                    // ============================================
                    var dashboard = new IncidentDashboardDTO
                    {
                        Overview = overview,
                        StatsParStatut = statsParStatut,
                        StatsParJour = statsParJour,
                        StatsParSemaine = statsParSemaine,
                        StatsParMois = statsParMois,
                        StatsResolution = statsResolution,                      // ✅ NOUVEAU
                        ResolutionParSeverite = resolutionParSeverite,          // ✅ NOUVEAU
                        ResolutionParTypeProbleme = resolutionParTypeProbleme   // ✅ NOUVEAU
                    };

                    _logger.LogInformation("Dashboard incidents généré - Total: {Total}, Résolus: {Resolus}, Temps moyen: {Moyenne}h",
                        total, nbResolus, moyenneHeuresGlobal);

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

                // ======================================================
                // 🔔 NOTIFICATION POUR RÉSOLUTION D'INCIDENT
                // ======================================================

                var technicien = await _userRepository.GetByIdAsync(userId);
                string technicienNom = technicien != null ? $"{technicien.Nom} {technicien.Prenom}" : "Le technicien";

                // 1. Notification au COMMERCANT créateur de l'incident
                if (incident.CreatedById.HasValue)
                {
                    var createurIncident = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                    if (createurIncident != null)
                    {
                        await _notificationService.CreateIncidentNotificationAsync(
                            createurIncident.Id,
                            incident.Id,
                            TypeNotification.IncidentResolu,
                            $"Incident résolu : {incident.CodeIncident}",
                            $"{technicienNom} a résolu votre incident '{incident.CodeIncident}'."
                        );
                        _logger.LogInformation("Notification envoyée au commerçant {CommercantId}", createurIncident.Id);
                    }
                }

                // 2. Notification aux ADMINS
                var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateIncidentNotificationAsync(
                        admin.Id,
                        incident.Id,
                        TypeNotification.IncidentResolu,
                        $"Incident résolu : {incident.CodeIncident}",
                        $"{technicienNom} a résolu l'incident '{incident.CodeIncident}'."
                    );
                }
                _logger.LogInformation("Notifications envoyées aux admins pour la résolution de l'incident {IncidentId}", incident.Id); 
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
                var ticket = await _ticketRepository.GetTicketWithDetailsAsync(ticketId);
                if (ticket != null && ticket.StatutTicket != StatutTicket.Resolu)
                {
                    var ancienStatut = ticket.StatutTicket;
                    ticket.StatutTicket = StatutTicket.Resolu;
                    ticket.DateCloture = DateTime.UtcNow;
                    await _ticketRepository.SaveChangesAsync();

                    _logger.LogInformation("Ticket {TicketId} automatiquement clôturé car tous ses incidents sont résolus", ticketId);

                    // ======================================================
                    // 🔔 NOTIFICATION : Ticket résolu automatiquement
                    // ======================================================

                    // Récupérer le créateur du ticket et le technicien
                    var createur = await _userRepository.GetByIdAsync(ticket.CreateurId);
                    var technicien = ticket.AssigneeId.HasValue
                        ? await _userRepository.GetByIdAsync(ticket.AssigneeId.Value)
                        : null;
                    string technicienNom = technicien != null ? $"{technicien.Nom} {technicien.Prenom}" : "Le technicien";

                    // 1. Notification au CREATEUR du ticket
                    if (createur != null)
                    {
                        await _notificationService.CreateTicketNotificationAsync(
                            createur.Id,
                            ticket.Id,
                            TypeNotification.TicketCloture,
                            $"Ticket résolu : {ticket.ReferenceTicket}",
                            $"Tous les incidents liés à votre ticket '{ticket.TitreTicket}' ont été résolus. Le ticket est maintenant fermé."
                        );
                        _logger.LogInformation("Notification envoyée au créateur du ticket {CreateurId} pour clôture automatique", createur.Id);
                    }

                    // 2. Notification aux ADMINS
                    var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                    foreach (var admin in admins)
                    {
                        await _notificationService.CreateTicketNotificationAsync(
                            admin.Id,
                            ticket.Id,
                            TypeNotification.TicketCloture,
                            $"Ticket résolu : {ticket.ReferenceTicket}",
                            $"Le ticket '{ticket.TitreTicket}' a été automatiquement résolu car tous ses incidents sont résolus. Créé par {createur?.Nom} {createur?.Prenom}."
                        );
                    }
                    _logger.LogInformation("Notifications envoyées aux admins pour la clôture automatique du ticket {TicketId}", ticketId);
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

                    // ✅ CORRECTION : Autoriser la suppression si l'incident est "Non traité" (valeur 0)
                    // Ne bloquer que si l'incident est "En cours" ou "Fermé"
                    if (incident.StatutIncident == StatutIncident.EnCours ||
                        incident.StatutIncident == StatutIncident.Ferme)
                    {
                        _logger.LogWarning("Tentative de suppression liaison TPE pour incident avec statut {Statut} | IncidentId: {IncidentId}",
                            incident.StatutIncident, incidentId);

                        return ApiResponse<bool>.Failure(
                            "Impossible de supprimer la liaison TPE : l'incident est en cours ou fermé.",
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

                    // Après la suppression réussie
                    var utilisateur = await _userRepository.GetByIdAsync(userId);
                    string utilisateurNom = utilisateur != null ? $"{utilisateur.Nom} {utilisateur.Prenom}" : "Un utilisateur";
                    string tpeNom = tpe.NumSerieComplet ?? tpe.NumSerie ?? "TPE";

                    // ======================================================
                    // 🔔 NOTIFICATIONS POUR DÉLIAISON DE TPE
                    // ======================================================

                    // 1. Notification aux ADMINS
                    var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                    foreach (var admin in admins)
                    {
                        await _notificationService.CreateIncidentNotificationAsync(
                            admin.Id,
                            incidentId,
                            TypeNotification.IncidentModifie,
                            $"TPE retiré de l'incident {incident.CodeIncident}",
                            $"{utilisateurNom} a retiré le TPE '{tpeNom}' de l'incident '{incident.CodeIncident}'."
                        );
                    }

                    // 2. Notification au COMMERCANT créateur de l'incident (s'il n'est pas l'actionneur)
                    if (incident.CreatedById.HasValue && incident.CreatedById.Value != userId)
                    {
                        var createurIncident = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                        if (createurIncident != null)
                        {
                            await _notificationService.CreateIncidentNotificationAsync(
                                createurIncident.Id,
                                incidentId,
                                TypeNotification.IncidentModifie,
                                $"TPE retiré de votre incident {incident.CodeIncident}",
                                $"{utilisateurNom} a retiré le TPE '{tpeNom}' de votre incident '{incident.CodeIncident}'."
                            );
                        }
                    }

                    _logger.LogInformation("Notifications envoyées pour la déliaison du TPE {TPEId} de l'incident {IncidentId}", tpeId, incidentId);

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

                        // ✅ Autoriser la modification si le statut est "Non traité" (valeur 0)
                        if (incident.StatutIncident.HasValue && incident.StatutIncident != StatutIncident.NonTraite)
                            return ApiResponse<List<IncidentTPEDTO>>.Failure(
                                "Vous ne pouvez pas ajouter de TPE à un incident qui a déjà un statut (en cours ou fermé).", resultCode: 76);
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

                    // ======================================================
                    // 🔔 NOTIFICATION POUR LIAISON DE TPE
                    // ======================================================

                    var utilisateur = await _userRepository.GetByIdAsync(userId);
                    string utilisateurNom = utilisateur != null ? $"{utilisateur.Nom} {utilisateur.Prenom}" : "Un utilisateur";
                    string tpeNoms = string.Join(", ", tpesLies.Select(t => t.NumSerie));

                    // 1. Notification aux ADMINS
                    var admins = await _userRepository.GetUsersByRoleAsync("Admin");
                    foreach (var admin in admins)
                    {
                        await _notificationService.CreateIncidentNotificationAsync(
                            admin.Id,
                            incidentId,
                            TypeNotification.IncidentModifie,
                            $"TPEs liés à l'incident {incident.CodeIncident}",
                            $"{utilisateurNom} a lié le(s) TPE(s) [{tpeNoms}] à l'incident '{incident.CodeIncident}'."
                        );
                    }

                    // 2. Notification au COMMERCANT créateur (s'il n'est pas l'actionneur)
                    if (incident.CreatedById.HasValue && incident.CreatedById.Value != userId)
                    {
                        var createurIncident = await _userRepository.GetByIdAsync(incident.CreatedById.Value);
                        if (createurIncident != null)
                        {
                            await _notificationService.CreateIncidentNotificationAsync(
                                createurIncident.Id,
                                incidentId,
                                TypeNotification.IncidentModifie,
                                $"TPEs liés à votre incident {incident.CodeIncident}",
                                $"{utilisateurNom} a lié le(s) TPE(s) [{tpeNoms}] à votre incident."
                            );
                        }
                    }

                    _logger.LogInformation("Notifications envoyées pour la liaison de TPEs à l'incident {IncidentId}", incidentId); 

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

        public async Task<ApiResponse<PagedResult<IncidentDTO>>> GetMyIncidentsPagedAsync(IncidentSearchRequest request, Guid userId)
        {
            return await MeasureAsync(nameof(GetMyIncidentsPagedAsync), request, async () =>
            {
                try
                {
                    var archivedIncidentIds = await _incidentArchiveRepository
                        .GetArchivedIncidentIdsByUserAsync(userId);

                    // ✅ CORRECTION : Commencer avec IQueryable, puis appliquer Include
                    var baseQuery = _incidentRepository.QueryWithDetails();

                    // ✅ Appliquer les Include séparément
                    var query = baseQuery
                        .Include(i => i.IncidentTickets)
                            .ThenInclude(it => it.Ticket)
                        .Include(i => i.EntitesImpactees)
                        .AsQueryable();  // Important pour garder le type IQueryable

                    query = query.Where(i => i.CreatedById == userId);

                    if (archivedIncidentIds.Any())
                    {
                        query = query.Where(i => !archivedIncidentIds.Contains(i.Id));
                    }

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

                    query = ApplySearchFilters(query, request, matchedUserIds);
                    var totalCount = await query.CountAsync();
                    query = ApplySorting(query, request.SortBy, request.SortDescending);

                    var pagedIncidents = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    // Mapper vers DTO
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
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des incidents");
                    return ApiResponse<PagedResult<IncidentDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }
        #endregion

        #region Dashboard Statistics Methods

        /// <summary>
        /// Calcule le temps moyen de résolution pour une liste d'incidents
        /// </summary>
        private (double moyenneHeures, double moyenneJours, int resolus) CalculerTempsMoyenResolution(List<IncidentEntity> incidents)
        {
            var tempsResolution = new List<double>();
            int resolus = 0;

            foreach (var incident in incidents)
            {
                // Un incident est résolu s'il a une date de résolution ET statut Fermé
                if (incident.DateResolution.HasValue && incident.StatutIncident == StatutIncident.Ferme)
                {
                    var temps = (incident.DateResolution.Value - incident.DateDetection).TotalHours;
                    tempsResolution.Add(temps);
                    resolus++;
                }
            }

            double moyenneHeures = tempsResolution.Any() ? Math.Round(tempsResolution.Average(), 1) : 0;
            double moyenneJours = Math.Round(moyenneHeures / 24, 1);

            return (moyenneHeures, moyenneJours, resolus);
        }

        /// <summary>
        /// Récupère le libellé de la sévérité
        /// </summary>
        private string GetSeveriteLabel(SeveriteIncident severite)
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

        /// <summary>
        /// Récupère le libellé du type de problème
        /// </summary>
        private string GetTypeProblemeLabel(TypeProbleme type)
        {
            return type switch
            {
                TypeProbleme.PaiementRefuse => "Paiement refusé",
                TypeProbleme.TerminalHorsLigne => "Terminal hors ligne",
                TypeProbleme.Lenteur => "Lenteur",
                TypeProbleme.BugAffichage => "Bug affichage",
                TypeProbleme.ConnexionReseau => "Connexion réseau",
                TypeProbleme.ErreurFluxTransactionnel => "Erreur flux transactionnel",
                TypeProbleme.ProblemeLogicielTPE => "Problème logiciel TPE",
                TypeProbleme.Autre => "Autre",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Récupère une couleur pour un type de problème (pour graphique)
        /// </summary>
        private string GetColorForTypeProbleme(TypeProbleme type)
        {
            return type switch
            {
                TypeProbleme.PaiementRefuse => "#dc3545",      // Rouge
                TypeProbleme.TerminalHorsLigne => "#fd7e14",   // Orange
                TypeProbleme.Lenteur => "#ffc107",             // Jaune
                TypeProbleme.BugAffichage => "#20c997",        // Turquoise
                TypeProbleme.ConnexionReseau => "#17a2b8",     // Bleu clair
                TypeProbleme.ErreurFluxTransactionnel => "#6f42c1", // Violet
                TypeProbleme.ProblemeLogicielTPE => "#e83e8c", // Rose
                TypeProbleme.Autre => "#6c757d",               // Gris
                _ => "#6c757d"
            };
        }

        /// <summary>
        /// Récupère une couleur pour la sévérité
        /// </summary>
        private string GetColorForSeverite(SeveriteIncident severite)
        {
            return severite switch
            {
                SeveriteIncident.NonDefinie => "#6c757d",  // Gris
                SeveriteIncident.Faible => "#28a745",      // Vert
                SeveriteIncident.Moyenne => "#ffc107",     // Jaune
                SeveriteIncident.Forte => "#dc3545",       // Rouge
                _ => "#6c757d"
            };
        }

        #endregion
    }
}
