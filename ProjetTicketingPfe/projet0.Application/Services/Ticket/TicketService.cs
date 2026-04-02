using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Commun.Ressources.Pagination;
using projet0.Application.Extensions;
using projet0.Application.Interfaces;
using projet0.Application.Services.Incident;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Diagnostics;
using System.Linq.Expressions;
using TicketEntity = projet0.Domain.Entities.Ticket;
using IncidentEntity = projet0.Domain.Entities.Incident;
using Microsoft.EntityFrameworkCore; 
using System.Linq;
using projet0.Application.Commun.DTOs.TicketDTOs;

namespace projet0.Application.Services.Ticket
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TicketService> _logger;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly IPieceJointeService _pieceJointeService;
        private readonly ICommentaireService _commentaireService;
        private readonly IIncidentRepository _incidentRepository;
        private readonly IIncidentTicketRepository _incidentTicketRepository;  
        private readonly IIncidentService _incidentService;

        public TicketService(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<TicketService> logger,
            IWebHostEnvironment environment,
            IPieceJointeService pieceJointeService,
            ICommentaireService commentaireService,
            IMapper mapper, 
            IIncidentRepository incidentRepository,  
            IIncidentTicketRepository incidentTicketRepository,
            IIncidentService incidentService)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
            _environment = environment;
            _pieceJointeService = pieceJointeService;
            _commentaireService = commentaireService;
            _mapper = mapper;
            _incidentRepository = incidentRepository; 
            _incidentTicketRepository = incidentTicketRepository;
            _incidentService = incidentService; 
        }

        #region Private Methods
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

        public async Task<TicketDTO> MapToDto(TicketEntity ticket)
        {
            try
            {
                _logger.LogDebug("Début MapToDto pour ticket {Id} - {Reference}", ticket.Id, ticket.ReferenceTicket);

                var dto = _mapper.Map<TicketDTO>(ticket);

                // Libellés
                dto.StatutTicketLibelle = GetStatutLibelle(ticket.StatutTicket);
                
                // Nom du créateur
                if (ticket.Createur != null)
                {
                    dto.CreateurNom = $"{ticket.Createur.Nom} {ticket.Createur.Prenom}";
                }
                else if (ticket.CreateurId != Guid.Empty)
                {
                    var user = await _userRepository.GetByIdAsync(ticket.CreateurId);
                    dto.CreateurNom = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
                }

                // Nom de l'assigné (optionnel)
                if (ticket.AssigneeId.HasValue)
                {
                    if (ticket.Assignee != null)
                    {
                        dto.AssigneeNom = $"{ticket.Assignee.Nom} {ticket.Assignee.Prenom}";
                    }
                    else
                    {
                        var user = await _userRepository.GetByIdAsync(ticket.AssigneeId.Value);
                        dto.AssigneeNom = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
                    }
                }

                if (ticket.Commentaires != null)
                {
                    dto.NombreCommentaires = ticket.Commentaires.Count;
                  
                    int totalPieces = 0;
                    foreach (var commentaire in ticket.Commentaires)
                    {
                        if (commentaire.PiecesJointes != null)
                        {
                            totalPieces += commentaire.PiecesJointes.Count;
                        }
                    }
                    dto.NombrePiecesJointes = totalPieces;
                }
                else
                {
                    dto.NombreCommentaires = 0;
                    dto.NombrePiecesJointes = 0;
                }

                _logger.LogDebug("MapToDto terminé pour ticket {Id} - Commentaires: {NbCommentaires}, Pièces: {NbPieces}",
                    ticket.Id, dto.NombreCommentaires, dto.NombrePiecesJointes);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans MapToDto pour ticket {Id}", ticket?.Id);
                throw;
            }
        }
 
        #endregion

        #region CRUD Operations
        private Expression<Func<TicketEntity, bool>>? BuildFilter(TicketPagedRequest request)
        {
            if (request == null)
                return null;

            // Commencer avec une collection de conditions
            var predicates = new List<Expression<Func<TicketEntity, bool>>>();

            // Filtre par statut
            if (request.Statut.HasValue)
            {
                predicates.Add(t => t.StatutTicket == request.Statut.Value);
            }

            // OPTION 1: Date exacte (si vous voulez les tickets d'un jour précis)
            if (request.DateDebut.HasValue && !request.DateFin.HasValue)
            {
                var date = request.DateDebut.Value.Date;
                var dateSuivante = date.AddDays(1);
                _logger.LogInformation("Filtre date exacte: tickets avec DateCreation entre {Date} et {DateSuivante}",
                    date, dateSuivante);
                predicates.Add(t => t.DateCreation >= date && t.DateCreation < dateSuivante);
            }
            // OPTION 2: Plage de dates (si les deux dates sont fournies)
            else if (request.DateDebut.HasValue && request.DateFin.HasValue)
            {
                var dateDebut = request.DateDebut.Value.Date;
                var dateFin = request.DateFin.Value.Date.AddDays(1);
                _logger.LogInformation("Filtre plage de dates: tickets entre {DateDebut} et {DateFin}",
                    dateDebut, dateFin);
                predicates.Add(t => t.DateCreation >= dateDebut && t.DateCreation < dateFin);
            }
            // OPTION 3: DateDebut seule (>=)
            else if (request.DateDebut.HasValue)
            {
                var dateDebut = request.DateDebut.Value.Date;
                _logger.LogInformation("Filtre DateDebut seule: tickets avec DateCreation >= {DateDebut}", dateDebut);
                predicates.Add(t => t.DateCreation >= dateDebut);
            }
            // OPTION 4: DateFin seule (<=)
            else if (request.DateFin.HasValue)
            {
                var dateFin = request.DateFin.Value.Date.AddDays(1);
                _logger.LogInformation("Filtre DateFin seule: tickets avec DateCreation < {DateFin}", dateFin);
                predicates.Add(t => t.DateCreation < dateFin);
            }

            // RECHERCHE AVANCÉE - Sur le nom du créateur, la référence et le titre
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower().Trim();

                predicates.Add(t =>
                    // Recherche dans la référence du ticket
                    t.ReferenceTicket.ToLower().Contains(term) ||

                    // Recherche dans le titre du ticket
                    t.TitreTicket.ToLower().Contains(term) ||

                    // Recherche dans le nom du créateur (prénom + nom)
                (t.Createur != null && (
                    (t.Createur.Nom.ToLower().Contains(term)) ||
                    (t.Createur.Prenom.ToLower().Contains(term)) ||
                    (t.Createur.Nom.ToLower() + " " + t.Createur.Prenom.ToLower()).Contains(term) ||
                    (t.Createur.Prenom.ToLower() + " " + t.Createur.Nom.ToLower()).Contains(term)
                ))
                );
            }

            // Combiner tous les prédicats avec AND
            if (!predicates.Any())
                return null;

            var combined = predicates.Aggregate((current, next) => current.AndAlso(next));
            return combined;
        }

        public async Task<ApiResponse<PagedResult<TicketDTO>>> GetTicketsPagedAsync(TicketPagedRequest request)
        {
            return await MeasureAsync(nameof(GetTicketsPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogInformation("Début GetTicketsPagedAsync - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
                        request.Page, request.PageSize, request.SearchTerm);

                    // 1. Construire le filtre
                    var filter = BuildFilter(request);

                    // 2. Obtenir la requête de base
                    var query = _ticketRepository.GetFilteredQuery(filter);

                    // 3. Appliquer le tri
                    if (!string.IsNullOrWhiteSpace(request.SortBy))
                    {
                        query = ApplySorting(query, request.SortBy, request.SortDescending);
                    }
                    else
                    {
                        // Tri par défaut
                        query = query.OrderByDescending(t => t.DateCreation);
                    }

                    // 4. Compter le total (AVANT pagination)
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total tickets trouvés: {TotalCount}", totalCount);

                    // 5. Appliquer la pagination
                    var items = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    _logger.LogInformation("{Count} tickets récupérés pour la page {Page}", items.Count, request.Page);

                    // 6. Mapper vers DTO
                    var dtos = new List<TicketDTO>();
                    foreach (var ticket in items)
                    {
                        dtos.Add(await MapToDto(ticket));
                    }

                    // 7. Créer le résultat paginé
                    var pagedResult = PagedResult<TicketDTO>.Create(
                        dtos,
                        totalCount,
                        request.Page,
                        request.PageSize
                    );

                    return ApiResponse<PagedResult<TicketDTO>>.Success(pagedResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des tickets");
                    return ApiResponse<PagedResult<TicketDTO>>.Failure("Erreur interne du serveur: " + ex.Message);
                }
            });
        }

        // Méthode de tri améliorée

        private IQueryable<TicketEntity> ApplySorting(IQueryable<TicketEntity> query, string sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(t => t.DateCreation);

            sortBy = sortBy.ToLower();

            return (sortBy, descending) switch
            {
                ("reference", false) => query.OrderBy(t => t.ReferenceTicket),
                ("reference", true) => query.OrderByDescending(t => t.ReferenceTicket),
                ("titre", false) => query.OrderBy(t => t.TitreTicket),
                ("titre", true) => query.OrderByDescending(t => t.TitreTicket),
                ("date", false) => query.OrderBy(t => t.DateCreation),
                ("date", true) => query.OrderByDescending(t => t.DateCreation),
                ("statut", false) => query.OrderBy(t => t.StatutTicket),
                ("statut", true) => query.OrderByDescending(t => t.StatutTicket),
                ("id", false) => query.OrderBy(t => t.Id),
                ("id", true) => query.OrderByDescending(t => t.Id),
                // Valeur par défaut si aucun cas ne correspond
                _ => descending
                    ? query.OrderByDescending(t => t.DateCreation)
                    : query.OrderByDescending(t => t.DateCreation) // Garde le tri par défaut
            };
        }

        public async Task<ApiResponse<TicketDTO>> CreateTicketAsync(CreateTicketDTO dto, Guid createurId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("CreateTicket START | Titre: {Titre}", dto.TitreTicket);

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(dto.TitreTicket))
                    return ApiResponse<TicketDTO>.Failure("Le titre est requis");

                // Générer la référence unique
                var reference = await _ticketRepository.GenerateReferenceTicketAsync();

                // Déterminer le statut initial
                var statutInitial = dto.AssigneeId.HasValue
                    ? StatutTicket.Assigne
                    : (StatutTicket?)null;

                // Créer le ticket
                var ticket = new TicketEntity
                {
                    Id = Guid.NewGuid(),
                    ReferenceTicket = reference,
                    TitreTicket = dto.TitreTicket,
                    DescriptionTicket = dto.DescriptionTicket ?? string.Empty,
                    DateLimite = dto.DateLimite,
                    StatutTicket = statutInitial,
                    DateCreation = DateTime.UtcNow,
                    CreateurId = createurId,
                    AssigneeId = dto.AssigneeId,
                    CreatedAt = DateTime.UtcNow,
                    Historiques = new List<HistoriqueTicket>(),
                    Commentaires = new List<CommentaireTicket>(),
                    Notifications = new List<Notification>()
                };

                // Ajouter un historique de création
                ticket.Historiques.Add(new HistoriqueTicket
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    AncienStatut = statutInitial,  // Pour la création, ancien = nouveau
                    DateChangement = DateTime.UtcNow,
                    ModifieParId = createurId
                });

                // Sauvegarder
                await _ticketRepository.AddAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                var result = await MapToDto(ticket);

                sw.Stop();
                _logger.LogInformation("CreateTicket SUCCESS | Ref: {Reference} | Statut: {Statut} | Duration: {Ms} ms",
                    reference, statutInitial, sw.ElapsedMilliseconds);

                return ApiResponse<TicketDTO>.Success(result, $"Ticket {reference} créé avec succès.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CreateTicket ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
                return ApiResponse<TicketDTO>.Failure("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<TicketDetailDTO>> GetTicketDetailAsync(Guid id, Guid userId)
        {
            _logger.LogInformation("=== TicketService.GetTicketDetailAsync ===");
            _logger.LogInformation("ID reçu: {Id}, UserId: {UserId}", id, userId);

            return await MeasureAsync(nameof(GetTicketDetailAsync), new { id }, async () =>
            {
                try
                {
                    _logger.LogInformation("Appel du repository GetTicketWithDetailsAsync pour ID: {Id}", id);
                    var ticket = await _ticketRepository.GetTicketWithDetailsAsync(id);

                    if (ticket == null)
                    {
                        _logger.LogWarning("Ticket avec ID {Id} non trouvé en base", id);
                        return ApiResponse<TicketDetailDTO>.Failure($"Ticket avec ID {id} non trouvé");
                    }

                    _logger.LogInformation("Ticket trouvé: {Reference}, Commentaires: {NbCommentaires}",
                        ticket.ReferenceTicket, ticket.Commentaires?.Count ?? 0);

                    _logger.LogInformation("Début du mapping vers TicketDetailDTO");
                    var dto = await MapToDetailDto(ticket);
                    _logger.LogInformation("Mapping terminé avec succès");

                    return ApiResponse<TicketDetailDTO>.Success(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de la récupération des détails du ticket {Id}", id);
                    return ApiResponse<TicketDetailDTO>.Failure($"Erreur interne du serveur: {ex.Message}");
                }
            });
        }

        public async Task<ApiResponse<TicketDTO>> GetTicketByIdAsync(Guid id)
        {
            _logger.LogInformation("=== TicketService.GetTicketByIdAsync ===");
            _logger.LogInformation("ID reçu: {Id}", id);
            _logger.LogInformation("ID en majuscules: {IdUpper}", id.ToString().ToUpper());
            _logger.LogInformation("ID en minuscules: {IdLower}", id.ToString().ToLower());

            return await MeasureAsync(nameof(GetTicketByIdAsync), new { id }, async () =>
            {
                try
                {
                    _logger.LogInformation("Appel du repository avec ID: {Id}", id);
                    var ticket = await _ticketRepository.GetByIdAsync(id);

                    if (ticket == null)
                    {
                        _logger.LogWarning("Ticket avec ID {Id} non trouvé en base", id);
                        return ApiResponse<TicketDTO>.Failure($"Ticket avec ID {id} non trouvé");
                    }

                    _logger.LogInformation("Ticket trouvé: {Reference}", ticket.ReferenceTicket);
                    var dto = await MapToDto(ticket);
                    return ApiResponse<TicketDTO>.Success(dto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération du ticket {Id}", id);
                    return ApiResponse<TicketDTO>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<bool>> DeleteTicketAsync(Guid id, Guid userId)
        {
            return await MeasureAsync(nameof(DeleteTicketAsync), new { id }, async () =>
            {
                try
                {
                    var ticket = await _ticketRepository.GetTicketWithDetailsAsync(id);

                    if (ticket == null)
                        return ApiResponse<bool>.Failure($"Ticket avec ID {id} non trouvé");

                    // Vérifier le rôle de l'utilisateur
                    var userRoles = await _userRepository.GetUserRolesAsync(userId);
                    var isAdmin = userRoles.Contains("Admin");

                    // Si ce n'est pas un admin, appliquer les restrictions
                    if (!isAdmin)
                    {
                        // RÈGLE : Ne peut pas supprimer un ticket en cours ou résolu
                        if (ticket.StatutTicket == StatutTicket.EnCours ||
                            ticket.StatutTicket == StatutTicket.Resolu)
                        {
                            return ApiResponse<bool>.Failure(
                                "Impossible de supprimer un ticket en cours ou résolu",
                                resultCode: 50
                            );
                        }

                        // RÈGLE : Vérifier que tous les incidents liés sont supprimables
                        if (ticket.IncidentTickets != null && ticket.IncidentTickets.Any())
                        {
                            var incidentsNonSupprimables = ticket.IncidentTickets
                                .Select(it => it.Incident)
                                .Where(i => i.StatutIncident.HasValue)
                                .ToList();

                            if (incidentsNonSupprimables.Any())
                            {
                                var ids = string.Join(", ", incidentsNonSupprimables.Select(i => i.CodeIncident));
                                return ApiResponse<bool>.Failure(
                                    $"Impossible de supprimer le ticket car des incidents liés ont déjà un statut: {ids}",
                                    resultCode: 51
                                );
                            }
                        }
                    }
                    else
                    {
                        // ADMIN : Peut tout supprimer, sans aucune restriction !
                        _logger.LogInformation("Admin supprime le ticket {Id} - Nettoyage des incidents liés", id);

                        // Récupérer tous les incidents liés AVANT de supprimer le ticket
                        var incidentsLies = new List<IncidentEntity>();
                        if (ticket.IncidentTickets != null)
                        {
                            incidentsLies = ticket.IncidentTickets
                                .Select(it => it.Incident)
                                .Where(i => i != null)
                                .ToList();
                        }

                        // Supprimer d'abord toutes les liaisons (IncidentTicket)
                        if (ticket.IncidentTickets != null)
                        {
                            foreach (var lien in ticket.IncidentTickets.ToList())
                            {
                                await _incidentTicketRepository.DeleteAsync(lien);
                            }
                        }

                        // Pour chaque incident lié, remettre son statut à null
                        foreach (var incident in incidentsLies)
                        {
                            incident.StatutIncident = null;
                            incident.DateResolution = null;
                            _logger.LogInformation("Incident {IncidentId} remis à null (statut et date résolution)", incident.Id);
                        }

                        // Sauvegarder les modifications des incidents
                        if (incidentsLies.Any())
                        {
                            await _incidentRepository.SaveChangesAsync();
                        }
                    }

                    // Supprimer le ticket (les liaisons ont déjà été supprimées)
                    await _ticketRepository.DeleteAsync(ticket);
                    await _ticketRepository.SaveChangesAsync();

                    _logger.LogInformation("Ticket supprimé avec succès | ID: {TicketId}, Ref: {Reference}",
                        id, ticket.ReferenceTicket);

                    return ApiResponse<bool>.Success(true, $"Ticket {ticket.ReferenceTicket} supprimé avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression du ticket {Id}", id);
                    return ApiResponse<bool>.Failure("Erreur interne du serveur");
                }
            });
        }

        private async Task<TicketDetailDTO> MapToDetailDto(TicketEntity ticket)
        {
            try
            {
                _logger.LogDebug("Début MapToDetailDto pour ticket {Id} - {Reference}", ticket.Id, ticket.ReferenceTicket);

                // 1. Mapper les propriétés de base avec AutoMapper
                var dto = _mapper.Map<TicketDetailDTO>(ticket);
                _logger.LogDebug("Mapping AutoMapper réussi");

                // 2. Ajouter les libellés
                dto.StatutTicketLibelle = GetStatutLibelle(ticket.StatutTicket);

                // 3. Nom du créateur
                if (ticket.Createur != null)
                {
                    dto.CreateurNom = $"{ticket.Createur.Nom} {ticket.Createur.Prenom}";
                }
                else if (ticket.CreateurId != Guid.Empty)
                {
                    var user = await _userRepository.GetByIdAsync(ticket.CreateurId);
                    dto.CreateurNom = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
                }

                // 4. Nom de l'assigné
                if (ticket.AssigneeId.HasValue)
                {
                    if (ticket.Assignee != null)
                    {
                        dto.AssigneeNom = $"{ticket.Assignee.Nom} {ticket.Assignee.Prenom}";
                    }
                    else
                    {
                        var user = await _userRepository.GetByIdAsync(ticket.AssigneeId.Value);
                        dto.AssigneeNom = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
                    }
                }

                // 5. Mapper les commentaires
                if (ticket.Commentaires != null && ticket.Commentaires.Any())
                {
                    dto.Commentaires = ticket.Commentaires.Select(c => new CommentaireDTO
                    {
                        Id = c.Id,
                        Message = c.Message,
                        DateCreation = c.DateCreation,
                        EstInterne = c.EstInterne,
                        AuteurId = c.AuteurId,
                        AuteurNom = c.Auteur != null ? $"{c.Auteur.Nom} {c.Auteur.Prenom}" : "Inconnu",
                        PiecesJointes = c.PiecesJointes?.Select(p => new PieceJointeDTO
                        {
                            Id = p.Id,
                            NomFichier = p.NomFichier,
                            DateAjout = p.DateAjout
                        }).ToList() ?? new()
                    }).ToList();
                }
                else
                {
                    dto.Commentaires = new List<CommentaireDTO>();
                }

                // 6. Compter les relations
                dto.NombreCommentaires = dto.Commentaires.Count;
                dto.NombrePiecesJointes = dto.Commentaires
                    .SelectMany(c => c.PiecesJointes)
                    .Count();

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur dans MapToDetailDto pour ticket {Id}", ticket?.Id);
                throw;
            }
        }
        
        public async Task<ApiResponse<bool>> LierTicketAIncident(Guid ticketId, Guid incidentId, Guid userId)
        {
            return await MeasureAsync(nameof(LierTicketAIncident), new { ticketId, incidentId }, async () =>
            {
                try
                {
                    // Vérifier que le ticket existe
                    var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                    if (ticket == null)
                        return ApiResponse<bool>.Failure($"Ticket avec ID {ticketId} non trouvé");

                    // Vérifier que l'incident existe
                    var incident = await _incidentRepository.GetByIdAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<bool>.Failure($"Incident avec ID {incidentId} non trouvé");

                    // Vérifier si la liaison existe déjà
                    var existingLink = await _incidentTicketRepository
                        .GetByTicketAndIncidentAsync(ticketId, incidentId);

                    if (existingLink != null)
                        return ApiResponse<bool>.Failure("Ce ticket est déjà lié à cet incident");

                    // Créer la liaison
                    var lien = new IncidentTicket
                    {
                        IncidentId = incidentId,
                        TicketId = ticketId,
                        DateLiaison = DateTime.UtcNow,
                        LieParId = userId
                    };

                    await _incidentTicketRepository.AddAsync(lien);
                    await _incidentTicketRepository.SaveChangesAsync();

                    // Mettre à jour le statut de l'incident (via le service d'incident)                    
                    await _incidentService.MettreAJourStatutIncident(incidentId);

                    _logger.LogInformation("Ticket {TicketId} lié à l'incident {IncidentId}", ticketId, incidentId);

                    return ApiResponse<bool>.Success(true, "Ticket lié à l'incident avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la liaison ticket-incident");
                    return ApiResponse<bool>.Failure("Erreur interne du serveur");
                }
            });
        }

        public async Task<ApiResponse<List<TicketDTO>>> GetTicketsByIncidentIdAsync(Guid incidentId)
        {
            return await MeasureAsync(nameof(GetTicketsByIncidentIdAsync), new { incidentId }, async () =>
            {
                try
                {
                    var tickets = await _ticketRepository.GetTicketsByIncidentIdAsync(incidentId);
                    var dtos = new List<TicketDTO>();

                    foreach (var ticket in tickets)
                    {
                        dtos.Add(await MapToDto(ticket));
                    }

                    return ApiResponse<List<TicketDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération des tickets de l'incident {IncidentId}", incidentId);
                    return ApiResponse<List<TicketDTO>>.Failure("Erreur interne du serveur");
                }
            });
        }
        public async Task<ApiResponse<LiaisonResultDTO>> LierIncidentsAuTicket(
    Guid ticketId,
    List<Guid> incidentIds,
    Guid userId)
        {
            return await MeasureAsync(nameof(LierIncidentsAuTicket), new { ticketId, incidentIds }, async () =>
            {
                try
                {
                    var ticket = await _ticketRepository.GetByIdAsync(ticketId);
                    if (ticket == null)
                        return ApiResponse<LiaisonResultDTO>.Failure($"Ticket {ticketId} non trouvé");

                    var liensAjoutes = 0;
                    var liensDejaExistants = 0;
                    var incidentsNonTrouves = 0;
                    var details = new List<string>();

                    foreach (var incidentId in incidentIds)
                    {
                        // Vérifier si l'incident existe
                        var incident = await _incidentRepository.GetByIdAsync(incidentId);
                        if (incident == null)
                        {
                            incidentsNonTrouves++;
                            details.Add($"Incident {incidentId} non trouvé");
                            continue;
                        }

                        // Vérifier si le lien existe déjà
                        var existe = await _incidentTicketRepository.ExistsAsync(ticketId, incidentId);
                        if (!existe)
                        {
                            var lien = new IncidentTicket
                            {
                                IncidentId = incidentId,
                                TicketId = ticketId,
                                DateLiaison = DateTime.UtcNow,
                                LieParId = userId
                            };

                            await _incidentTicketRepository.AddAsync(lien);
                            liensAjoutes++;
                            details.Add($"Incident {incidentId} lié avec succès");

                            // Mettre à jour le statut de l'incident
                            await _incidentService.MettreAJourStatutIncident(incidentId);
                        }
                        else
                        {
                            liensDejaExistants++;
                            details.Add($"Incident {incidentId} déjà lié à ce ticket");
                        }
                    }

                    await _incidentTicketRepository.SaveChangesAsync();

                    var result = new LiaisonResultDTO
                    {
                        LiensAjoutes = liensAjoutes,
                        LiensDejaExistants = liensDejaExistants,
                        IncidentsNonTrouves = incidentsNonTrouves,
                        TotalDemande = incidentIds.Count,
                        Details = details
                    };

                    string message;
                    if (liensAjoutes > 0 && liensDejaExistants > 0)
                        message = $"{liensAjoutes} incident(s) lié(s), {liensDejaExistants} déjà existant(s)";
                    else if (liensAjoutes > 0)
                        message = $"{liensAjoutes} incident(s) lié(s) avec succès";
                    else if (liensDejaExistants > 0)
                        message = $"Tous les incidents ({liensDejaExistants}) étaient déjà liés à ce ticket";
                    else
                        message = "Aucun incident n'a pu être lié";

                    return ApiResponse<LiaisonResultDTO>.Success(result, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du lien incidents-ticket");
                    return ApiResponse<LiaisonResultDTO>.Failure("Erreur interne");
                }
            });
        }

        public async Task<ApiResponse<UpdateTicketResponseDTO>> UpdateTicketAsync(Guid id, UpdateTicketDTO dto, Guid userId)
        {
            return await MeasureAsync(nameof(UpdateTicketAsync), new { id, dto }, async () =>
            {
                // Déclarer ces variables en dehors du try pour pouvoir y accéder dans le catch
                TicketEntity ticket = null;
                var isAdmin = false;
                var isTechnicien = false;
                var sauvegardeReussie = false;
                var modifications = new List<string>();
                var erreurs = new List<string>();
                var ancienStatut = (StatutTicket?)null;
                var ancienAssigneeId = (Guid?)null;

                try
                {
                    ticket = await _ticketRepository.GetTicketWithDetailsAsync(id);
                    if (ticket == null)
                        return ApiResponse<UpdateTicketResponseDTO>.Failure($"Ticket avec ID {id} non trouvé");

                    // Vérifier les permissions
                    var userRoles = await _userRepository.GetUserRolesAsync(userId);
                    isAdmin = userRoles.Contains("Admin");
                    isTechnicien = userRoles.Contains("Technicien");

                    ancienStatut = ticket.StatutTicket;
                    ancienAssigneeId = ticket.AssigneeId;

                    _logger.LogWarning("=== DÉBUT UPDATE TICKET {Id} ===", id);
                    _logger.LogWarning("Rôle: {Role}", isAdmin ? "Admin" : (isTechnicien ? "Technicien" : "Autre"));
                    _logger.LogWarning("État initial - Statut: {Statut}, AssigneeId: {AssigneeId}",
                        ticket.StatutTicket?.ToString() ?? "null", ticket.AssigneeId?.ToString() ?? "null");

                    // ========================================================
                    // RÈGLE COMMUNE : Si le ticket est résolu, personne ne peut le modifier
                    // ========================================================
                    if (ticket.StatutTicket == StatutTicket.Resolu)
                    {
                        return ApiResponse<UpdateTicketResponseDTO>.Failure(
                            "Impossible de modifier un ticket résolu.",
                            resultCode: 80
                        );
                    }

                    // ========================================================
                    // VÉRIFICATION PERMISSIONS TECHNICIEN (tentatives interdites)
                    // ========================================================
                    if (isTechnicien && !isAdmin)
                    {
                        var tentatives = new List<string>();
                        if (!string.IsNullOrWhiteSpace(dto.TitreTicket)) tentatives.Add("titre");
                        if (dto.DescriptionTicket != null) tentatives.Add("description");
                        if (dto.DateLimite.HasValue) tentatives.Add("date limite");

                        if (tentatives.Any())
                        {
                            erreurs.Add($"Vous n'avez pas le droit de modifier: {string.Join(", ", tentatives)}");
                        }
                    }

                    // ========================================================
                    // 1. CHAMPS MODIFIABLES PAR ADMIN UNIQUEMENT
                    // ========================================================
                    if (isAdmin)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.TitreTicket) && dto.TitreTicket != ticket.TitreTicket)
                        {
                            _logger.LogWarning("Admin modifie Titre: '{Ancien}' -> '{Nouveau}'",
                                ticket.TitreTicket, dto.TitreTicket);
                            ticket.TitreTicket = dto.TitreTicket;
                            modifications.Add("Titre");
                        }

                        if (dto.DescriptionTicket != null && dto.DescriptionTicket != ticket.DescriptionTicket)
                        {
                            _logger.LogWarning("Admin modifie Description: '{Ancien}' -> '{Nouveau}'",
                                ticket.DescriptionTicket, dto.DescriptionTicket);
                            ticket.DescriptionTicket = dto.DescriptionTicket;
                            modifications.Add("Description");
                        }

                        if (dto.DateLimite.HasValue && dto.DateLimite != ticket.DateLimite)
                        {
                            _logger.LogWarning("Admin modifie DateLimite: '{Ancien}' -> '{Nouveau}'",
                                ticket.DateLimite, dto.DateLimite);
                            ticket.DateLimite = dto.DateLimite;
                            modifications.Add("Date limite");
                        }
                    }

                    // ========================================================
                    // 2. GESTION DE L'ASSIGNATION (NOUVELLE VERSION)
                    // ========================================================
                    if (dto.IsAssigneeIdSpecified && dto.AssigneeId != ticket.AssigneeId)
                    {
                        _logger.LogWarning("=== MODIFICATION ASSIGNATION ===");
                        _logger.LogWarning("AssigneeId actuel: {Actuel}", ticket.AssigneeId?.ToString() ?? "null");
                        _logger.LogWarning("AssigneeId demandé: {Demande}", dto.AssigneeId?.ToString() ?? "null");

                        if (dto.AssigneeId.HasValue)
                        {
                            // CAS 1: On assigne à quelqu'un
                            var assignee = await _userRepository.GetByIdAsync(dto.AssigneeId.Value);
                            if (assignee == null)
                            {
                                erreurs.Add("L'utilisateur assigné n'existe pas.");
                            }
                            else
                            {
                                var roles = await _userRepository.GetUserRolesAsync(dto.AssigneeId.Value);
                                if (!roles.Contains("Technicien"))
                                {
                                    erreurs.Add("Vous ne pouvez assigner un ticket qu'à un technicien.");
                                }
                                else
                                {
                                    // Vérifier les droits selon le rôle
                                    bool peutAssigner = false;

                                    if (isAdmin)
                                    {
                                        peutAssigner = true;
                                    }
                                    else if (isTechnicien)
                                    {
                                        // Technicien ne peut réassigner que ses tickets en cours
                                        peutAssigner = (ticket.AssigneeId == userId &&
                                                       ticket.StatutTicket == StatutTicket.EnCours);

                                        if (!peutAssigner)
                                        {
                                            if (ticket.AssigneeId != userId)
                                                erreurs.Add("Vous ne pouvez réassigner que vos propres tickets.");
                                            else if (ticket.StatutTicket != StatutTicket.EnCours)
                                                erreurs.Add("Vous ne pouvez réassigner qu'un ticket en cours.");
                                        }
                                    }

                                    if (peutAssigner)
                                    {
                                        ticket.AssigneeId = dto.AssigneeId;
                                        modifications.Add($"Assignation -> {assignee.Nom} {assignee.Prenom}");

                                        // Le statut redevient Assigné si le ticket n'est pas résolu
                                        if (ticket.StatutTicket != StatutTicket.Resolu)
                                        {
                                            ticket.StatutTicket = StatutTicket.Assigne;
                                            modifications.Add("Statut -> Assigné");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // CAS 2: On veut explicitement supprimer l'assignation (mettre null)
                            if (isAdmin)
                            {
                                ticket.AssigneeId = null;
                                modifications.Add("Assignation (supprimée)");

                                // Optionnel: que faire du statut quand on désassigne ?
                                // ticket.StatutTicket = null;
                            }
                            else
                            {
                                erreurs.Add("Seul l'admin peut désassigner un ticket.");
                            }
                        }
                    }

                    // ========================================================
                    // 3. GESTION DU STATUT (Technicien uniquement)
                    // ========================================================
                    if (isTechnicien && dto.StatutTicket.HasValue && dto.StatutTicket.Value != ticket.StatutTicket)
                    {
                        var nouveauStatut = dto.StatutTicket.Value;

                        // Vérification : Le technicien ne peut modifier que ses propres tickets
                        if (ticket.AssigneeId != userId)
                        {
                            erreurs.Add("Vous ne pouvez modifier le statut que des tickets qui vous sont assignés.");
                        }
                        else
                        {
                            bool transitionValide = false;

                            // RÈGLES STRICTES pour le technicien
                            if (ticket.StatutTicket == StatutTicket.Assigne && nouveauStatut == StatutTicket.EnCours)
                            {
                                transitionValide = true;
                                _logger.LogWarning("Technicien transition: Assigné -> EnCours");

                                // Mettre à jour les incidents liés
                                if (ticket.IncidentTickets != null && ticket.IncidentTickets.Any())
                                {
                                    foreach (var lien in ticket.IncidentTickets)
                                    {
                                        if (lien.Incident != null && lien.Incident.StatutIncident != StatutIncident.Ferme)
                                        {
                                            lien.Incident.StatutIncident = StatutIncident.EnCours;
                                            _logger.LogInformation("Incident {IncidentId} passé en cours", lien.IncidentId);
                                        }
                                    }
                                    modifications.Add("Incidents mis à jour (EnCours)");
                                }
                            }
                            else if (ticket.StatutTicket == StatutTicket.EnCours && nouveauStatut == StatutTicket.Resolu)
                            {
                                transitionValide = true;
                                _logger.LogWarning("Technicien transition: EnCours -> Résolu");

                                // Date de clôture
                                ticket.DateCloture = DateTime.UtcNow;
                                modifications.Add("Date clôture enregistrée");
                            }
                            else
                            {
                                var statutActuel = ticket.StatutTicket?.ToString() ?? "null";
                                erreurs.Add($"Transition interdite: '{statutActuel}' -> '{nouveauStatut}'");
                            }

                            if (transitionValide)
                            {
                                ticket.StatutTicket = nouveauStatut;
                                modifications.Add($"Statut -> {GetStatutLibelle(nouveauStatut)}");
                            }
                        }
                    }

                    // ========================================================
                    // 4. AJOUTER À L'HISTORIQUE SI DES CHANGEMENTS ONT EU LIEU
                    // ========================================================
                    if (modifications.Any() || ancienStatut != ticket.StatutTicket || ancienAssigneeId != ticket.AssigneeId)
                    {
                        ticket.Historiques ??= new List<HistoriqueTicket>();
                        ticket.Historiques.Add(new HistoriqueTicket
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            AncienStatut = ancienStatut,
                            DateChangement = DateTime.UtcNow,
                            ModifieParId = userId
                        });

                        _logger.LogInformation("Ticket {Id} modifié : {Modifications}", id, string.Join(", ", modifications));
                    }

                    ticket.UpdatedAt = DateTime.UtcNow;

                    // ========================================================
                    // 5. LOG DE DÉBOGAGE AVANT SAUVEGARDE
                    // ========================================================
                    _logger.LogWarning("=== AVANT SAUVEGARDE ===");
                    _logger.LogWarning("Modifications détectées: {Count}", modifications.Count);
                    _logger.LogWarning("Liste des modifs: {Modifs}", string.Join(", ", modifications));
                    _logger.LogWarning("AssigneeId: {AssigneeId}", ticket.AssigneeId?.ToString() ?? "null");
                    _logger.LogWarning("Statut: {Statut}", ticket.StatutTicket?.ToString() ?? "null");

                    // ========================================================
                    // 6. SAUVEGARDER SI DES MODIFICATIONS
                    // ========================================================
                    if (modifications.Any())
                    {
                        _logger.LogInformation("Tentative de sauvegarde des modifications: {Modifications}",
                            string.Join(", ", modifications));

                        _ticketRepository.SetModified(ticket);

                        try
                        {
                            var saved = await _ticketRepository.SaveChangesAsync();
                            if (saved > 0)
                            {
                                _logger.LogInformation("{Saved} modification(s) sauvegardée(s)", saved);
                                sauvegardeReussie = true;
                            }
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            _logger.LogWarning("Conflit de concurrence pour le ticket {Id}. Rechargement...", id);

                            _ticketRepository.Detach(ticket);
                            ticket = await _ticketRepository.GetByIdAsync(id);

                            if (ticket == null)
                                return ApiResponse<UpdateTicketResponseDTO>.Failure("Le ticket a été supprimé.");

                            // Réappliquer les changements
                            if (isTechnicien && dto.StatutTicket.HasValue)
                            {
                                ticket.StatutTicket = dto.StatutTicket.Value;
                            }
                            if (isAdmin)
                            {
                                if (!string.IsNullOrWhiteSpace(dto.TitreTicket))
                                    ticket.TitreTicket = dto.TitreTicket;
                                if (dto.DescriptionTicket != null)
                                    ticket.DescriptionTicket = dto.DescriptionTicket;
                                if (dto.DateLimite.HasValue)
                                    ticket.DateLimite = dto.DateLimite;
                            }

                            // Gestion de l'assignation APRÈS rechargement
                            if (dto.IsAssigneeIdSpecified && dto.AssigneeId.HasValue)
                            {
                                ticket.AssigneeId = dto.AssigneeId;

                                if (!ticket.StatutTicket.HasValue)
                                {
                                    ticket.StatutTicket = StatutTicket.Assigne;
                                    _logger.LogInformation("Statut automatique appliqué après rechargement: Assigné");
                                }
                            }

                            ticket.UpdatedAt = DateTime.UtcNow;
                            _ticketRepository.SetModified(ticket);

                            try
                            {
                                var saved = await _ticketRepository.SaveChangesAsync();
                                if (saved > 0)
                                {
                                    _logger.LogInformation("Ticket {Id} mis à jour après conflit.", id);
                                    sauvegardeReussie = true;
                                }
                            }
                            catch (DbUpdateConcurrencyException ex2)
                            {
                                _logger.LogError(ex2, "Échec définitif pour le ticket {Id}", id);
                                return ApiResponse<UpdateTicketResponseDTO>.Failure(
                                    "Le ticket a été modifié à plusieurs reprises. Veuillez réessayer.");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("AUCUNE modification détectée !");
                    }

                    // ========================================================
                    // 7. METTRE À JOUR LES INCIDENTS LIÉS SI NÉCESSAIRE
                    // ========================================================
                    if (ticket.StatutTicket == StatutTicket.Resolu && ticket.IncidentTickets != null)
                    {
                        foreach (var lien in ticket.IncidentTickets)
                        {
                            await _incidentService.MettreAJourStatutIncident(lien.IncidentId);
                        }
                    }

                    // ========================================================
                    // 8. PRÉPARER LA RÉPONSE
                    // ========================================================
                    var responseDto = await MapToDetailDto(ticket);
                    var updateResponse = new UpdateTicketResponseDTO
                    {
                        Id = responseDto.Id,
                        ReferenceTicket = responseDto.ReferenceTicket,
                        TitreTicket = responseDto.TitreTicket,
                        DescriptionTicket = responseDto.DescriptionTicket,
                        StatutTicket = responseDto.StatutTicket,
                        StatutTicketLibelle = responseDto.StatutTicketLibelle,
                        DateCreation = responseDto.DateCreation,
                        DateLimite = responseDto.DateLimite,
                        DateCloture = responseDto.DateCloture,
                        CreateurId = responseDto.CreateurId,
                        CreateurNom = responseDto.CreateurNom,
                        AssigneeId = responseDto.AssigneeId,
                        AssigneeNom = responseDto.AssigneeNom,
                        NombreCommentaires = responseDto.NombreCommentaires,
                        NombrePiecesJointes = responseDto.NombrePiecesJointes,
                        Commentaires = responseDto.Commentaires
                    };

                    // ========================================================
                    // 9. CONSTRUIRE LE MESSAGE
                    // ========================================================
                    string message;
                    if (!sauvegardeReussie && modifications.Any())
                        message = "Aucune modification n'a pu être sauvegardée. Veuillez réessayer.";
                    else if (modifications.Any() && !erreurs.Any())
                        message = $"Ticket mis à jour avec succès. Modifications: {string.Join(", ", modifications)}.";
                    else if (modifications.Any() && erreurs.Any())
                        message = $"Mise à jour partielle. Modifications: {string.Join(", ", modifications)}. Problèmes: {string.Join("; ", erreurs)}.";
                    else if (!modifications.Any() && erreurs.Any())
                        message = $"Aucune modification appliquée. Problèmes: {string.Join("; ", erreurs)}.";
                    else
                        message = "Aucune modification n'a été demandée ou autorisée.";

                    _logger.LogWarning("=== FIN UPDATE TICKET {Id} ===", id);
                    _logger.LogWarning("Message final: {Message}", message);

                    return ApiResponse<UpdateTicketResponseDTO>.Success(updateResponse, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la mise à jour du ticket {Id}", id);
                    return ApiResponse<UpdateTicketResponseDTO>.Failure($"Erreur interne: {ex.Message}");
                }
            });
        }
        private string GetStatutLibelle(StatutTicket? statut)
        {
            if (!statut.HasValue)
                return "Non assigné";
            return statut switch
            {
                StatutTicket.Assigne => "Assigné",
                StatutTicket.EnCours => "En cours",
                StatutTicket.Resolu => "Résolu",
                _ => statut.ToString()
            };
        }

        private UpdateTicketResponseDTO MapToUpdateResponse(TicketDetailDTO detailDto)
        {
            return new UpdateTicketResponseDTO
            {
                Id = detailDto.Id,
                ReferenceTicket = detailDto.ReferenceTicket,
                TitreTicket = detailDto.TitreTicket,
                DescriptionTicket = detailDto.DescriptionTicket,
                StatutTicket = detailDto.StatutTicket,
                StatutTicketLibelle = detailDto.StatutTicketLibelle,
                DateCreation = detailDto.DateCreation,
                DateLimite = detailDto.DateLimite,
                DateCloture = detailDto.DateCloture,
                CreateurId = detailDto.CreateurId,
                CreateurNom = detailDto.CreateurNom,
                AssigneeId = detailDto.AssigneeId,
                AssigneeNom = detailDto.AssigneeNom,
                NombreCommentaires = detailDto.NombreCommentaires,
                NombrePiecesJointes = detailDto.NombrePiecesJointes,
                Commentaires = detailDto.Commentaires
            };
        }

        public async Task<ApiResponse<UpdateTicketResponseDTO>> TechnicianUpdateTicketAsync(
    Guid id,
    TechnicianUpdateTicketDTO dto,
    Guid technicienId)
        {
            return await MeasureAsync(nameof(TechnicianUpdateTicketAsync), new { id, dto }, async () =>
            {
                TicketEntity ticket = null;
                var modifications = new List<string>();
                var erreurs = new List<string>();

                try
                {
                    ticket = await _ticketRepository.GetTicketWithDetailsAsync(id);
                    if (ticket == null)
                        return ApiResponse<UpdateTicketResponseDTO>.Failure($"Ticket {id} non trouvé");

                    var ancienStatut = ticket.StatutTicket;
                    var ancienAssigneeId = ticket.AssigneeId;

                    // RÈGLE 1: Le technicien ne peut modifier que ses tickets assignés
                    if (ticket.AssigneeId != technicienId)
                    {
                        return ApiResponse<UpdateTicketResponseDTO>.Failure(
                            "Vous ne pouvez modifier que les tickets qui vous sont assignés.");
                    }

                    // RÈGLE 2: Gestion du changement d'assignation
                    if (dto.AssigneeId.HasValue && dto.AssigneeId != ticket.AssigneeId)
                    {
                        if (ticket.StatutTicket != StatutTicket.EnCours)
                        {
                            erreurs.Add("Vous ne pouvez réassigner un ticket que lorsqu'il est en cours.");
                        }
                        else
                        {
                            var newAssignee = await _userRepository.GetByIdAsync(dto.AssigneeId.Value);
                            if (newAssignee == null)
                            {
                                erreurs.Add("L'utilisateur assigné n'existe pas.");
                            }
                            else
                            {
                                var roles = await _userRepository.GetUserRolesAsync(dto.AssigneeId.Value);
                                if (!roles.Contains("Technicien"))
                                {
                                    erreurs.Add("Vous ne pouvez assigner un ticket qu'à un technicien.");
                                }
                                else
                                {
                                    ticket.AssigneeId = dto.AssigneeId;
                                    ticket.StatutTicket = StatutTicket.Assigne;
                                    modifications.Add($"Réassigné à {newAssignee.Nom} {newAssignee.Prenom}");
                                    modifications.Add("Statut -> Assigné");
                                }
                            }
                        }
                    }

                    // RÈGLE 3: Gestion du changement de statut
                    if (dto.StatutTicket.HasValue && dto.StatutTicket.Value != ticket.StatutTicket)
                    {
                        var nouveauStatut = dto.StatutTicket.Value;
                        var statutActuel = ticket.StatutTicket;

                        var transitionsAutorisees = new Dictionary<StatutTicket, List<StatutTicket>>
                {
                    { StatutTicket.Assigne, new List<StatutTicket> { StatutTicket.EnCours } },
                    { StatutTicket.EnCours, new List<StatutTicket> { StatutTicket.Resolu } }
                };

                        bool transitionValide = statutActuel.HasValue &&
                            transitionsAutorisees.ContainsKey(statutActuel.Value) &&
                            transitionsAutorisees[statutActuel.Value].Contains(nouveauStatut);

                        if (transitionValide)
                        {
                            ticket.StatutTicket = nouveauStatut;
                            modifications.Add($"Statut -> {GetStatutLibelle(nouveauStatut)}");

                            if (nouveauStatut == StatutTicket.Resolu)
                            {
                                ticket.DateCloture = DateTime.UtcNow;
                                modifications.Add("Date clôture enregistrée");

                                if (ticket.IncidentTickets != null)
                                {
                                    foreach (var lien in ticket.IncidentTickets)
                                    {
                                        await _incidentService.MettreAJourStatutIncident(lien.IncidentId);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var statutActuelLib = statutActuel?.ToString() ?? "null";
                            erreurs.Add($"Transition interdite: {statutActuelLib} -> {nouveauStatut}");
                        }
                    }

                    // RÈGLE 4: Si des erreurs, retourner
                    if (erreurs.Any())
                    {
                        return ApiResponse<UpdateTicketResponseDTO>.Failure(
                            $"Modifications refusées: {string.Join("; ", erreurs)}");
                    }

                    // RÈGLE 5: Sauvegarder avec gestion de concurrence
                    if (modifications.Any())
                    {
                        ticket.Historiques ??= new List<HistoriqueTicket>();
                        ticket.Historiques.Add(new HistoriqueTicket
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            AncienStatut = ancienStatut,
                            DateChangement = DateTime.UtcNow,
                            ModifieParId = technicienId,
                        });

                        ticket.UpdatedAt = DateTime.UtcNow;
                        _ticketRepository.SetModified(ticket);

                        try
                        {
                            var saved = await _ticketRepository.SaveChangesAsync();

                            if (saved == 0)
                            {
                                _logger.LogWarning("Conflit de concurrence pour le ticket {Id}", id);
                                var entry = _ticketRepository.GetDbContext().Entry(ticket);
                                await entry.ReloadAsync();

                                return ApiResponse<UpdateTicketResponseDTO>.Failure(
                                    "Le ticket a été modifié par un autre utilisateur. Veuillez rafraîchir.");
                            }
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            _logger.LogWarning(ex, "Conflit de concurrence pour le ticket {Id}", id);
                            var entry = _ticketRepository.GetDbContext().Entry(ticket);
                            await entry.ReloadAsync();

                            return ApiResponse<UpdateTicketResponseDTO>.Failure(
                                "Le ticket a été modifié par un autre utilisateur. Veuillez rafraîchir.");
                        }
                    }

                    // 6. Préparer la réponse
                    var detailDto = await MapToDetailDto(ticket);
                    var responseDto = new UpdateTicketResponseDTO
                    {
                        Id = detailDto.Id,
                        ReferenceTicket = detailDto.ReferenceTicket,
                        TitreTicket = detailDto.TitreTicket,
                        DescriptionTicket = detailDto.DescriptionTicket,
                        StatutTicket = detailDto.StatutTicket,
                        StatutTicketLibelle = detailDto.StatutTicketLibelle,
                        DateCreation = detailDto.DateCreation,
                        DateLimite = detailDto.DateLimite,
                        DateCloture = detailDto.DateCloture,
                        CreateurId = detailDto.CreateurId,
                        CreateurNom = detailDto.CreateurNom,
                        AssigneeId = detailDto.AssigneeId,
                        AssigneeNom = detailDto.AssigneeNom,
                        NombreCommentaires = detailDto.NombreCommentaires,
                        NombrePiecesJointes = detailDto.NombrePiecesJointes,
                        Commentaires = detailDto.Commentaires
                    };

                    var message = modifications.Any()
                        ? $"Ticket mis à jour. Modifications: {string.Join(", ", modifications)}"
                        : "Aucune modification détectée.";

                    return ApiResponse<UpdateTicketResponseDTO>.Success(responseDto, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur TechnicianUpdateTicket pour {Id}", id);
                    return ApiResponse<UpdateTicketResponseDTO>.Failure($"Erreur interne: {ex.Message}");
                }
            });
        }

        private async Task ReloadTicketAsync(TicketEntity ticket)
        {
            if (ticket == null) return;

            var entry = _ticketRepository.GetDbContext().Entry(ticket);
            await entry.ReloadAsync();
        }
        public async Task<ApiResponse<bool>> DelierIncidentDuTicket(Guid ticketId, Guid incidentId, Guid userId)
        {
            return await MeasureAsync(nameof(DelierIncidentDuTicket), new { ticketId, incidentId }, async () =>
            {
                try
                {
                    // Vérifier que le ticket existe avec ses détails
                    var ticket = await _ticketRepository.GetTicketWithDetailsAsync(ticketId);
                    if (ticket == null)
                        return ApiResponse<bool>.Failure($"Ticket {ticketId} non trouvé");

                    // Vérifier les droits de l'utilisateur
                    var userRoles = await _userRepository.GetUserRolesAsync(userId);
                    var isAdmin = userRoles.Contains("Admin");

                    // RÈGLE 1 : Seul l'admin peut délier un incident d'un ticket
                    if (!isAdmin)
                    {
                        _logger.LogWarning("Tentative de suppression liaison par un non-admin | UserId: {UserId}, TicketId: {TicketId}",
                            userId, ticketId);

                        return ApiResponse<bool>.Failure(
                            "Seul un administrateur peut supprimer une liaison entre un ticket et un incident.",
                            resultCode: 93
                        );
                    }

                    // RÈGLE 2 : L'admin ne peut délier que si le ticket n'est PAS "EnCours" ou "Resolu"
                    if (ticket.StatutTicket == StatutTicket.EnCours || ticket.StatutTicket == StatutTicket.Resolu)
                    {
                        _logger.LogWarning("Tentative de suppression liaison par admin pour ticket avec statut {Statut} | TicketId: {TicketId}",
                            ticket.StatutTicket, ticketId);

                        return ApiResponse<bool>.Failure(
                            "Impossible de supprimer la liaison : le ticket est en cours ou résolu.",
                            resultCode: 90
                        );
                    }

                    // Vérifier que l'incident existe
                    var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
                    if (incident == null)
                        return ApiResponse<bool>.Failure($"Incident {incidentId} non trouvé");

                    // Vérifier que la liaison existe
                    var existe = await _incidentTicketRepository.ExistsAsync(ticketId, incidentId);
                    if (!existe)
                        return ApiResponse<bool>.Failure("Cette liaison n'existe pas");

                    // Supprimer la liaison
                    var supprime = await _incidentTicketRepository.DeleteLiaisonAsync(ticketId, incidentId);
                    if (!supprime)
                        return ApiResponse<bool>.Failure("Erreur lors de la suppression");

                    // Mettre à jour le statut de l'incident après déliaison
                    await MettreAJourStatutIncidentApresDeliaison(incidentId);

                    await _incidentRepository.SaveChangesAsync();

                    _logger.LogInformation("Liaison supprimée entre ticket {TicketId} (Statut: {Statut}) et incident {IncidentId} par Admin {UserId}",
                        ticketId, ticket.StatutTicket, incidentId, userId);

                    return ApiResponse<bool>.Success(true, "Liaison supprimée avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression de la liaison");
                    return ApiResponse<bool>.Failure("Erreur interne du serveur");
                }
            });
        }

        // Méthode helper pour mettre à jour le statut de l'incident
        private async Task MettreAJourStatutIncidentApresDeliaison(Guid incidentId)
        {
            var incident = await _incidentRepository.GetIncidentWithDetailsAsync(incidentId);
            if (incident == null) return;

            // Récupérer tous les tickets encore liés à cet incident
            var ticketsRestants = await _incidentTicketRepository.GetTicketsByIncidentIdAsync(incidentId);

            if (!ticketsRestants.Any())
            {
                // Plus aucun ticket lié à cet incident
                incident.StatutIncident = null;
                incident.DateResolution = null;
                _logger.LogInformation("Incident {IncidentId} : plus de tickets liés, statut remis à null", incidentId);
            }
            else
            {
                // Vérifie si des tickets sont encore en cours
                var aUnTicketEnCours = ticketsRestants.Any(t => t.StatutTicket == StatutTicket.EnCours);

                if (!aUnTicketEnCours)
                {
                    // Plus de tickets en cours, l'incident n'est plus "EnCours"
                    if (incident.StatutIncident == StatutIncident.EnCours)
                    {
                        incident.StatutIncident = null;
                        incident.DateResolution = null;
                        _logger.LogInformation("Incident {IncidentId} : plus de tickets en cours, statut remis à null", incidentId);
                    }
                }
                else
                {
                    // L'incident reste en cours car d'autres tickets sont encore en cours
                    _logger.LogInformation("Incident {IncidentId} : reste en cours car d'autres tickets sont encore en cours", incidentId);
                }
            }
        }

        public async Task<ApiResponse<PagedResult<TicketDTO>>> GetMesTicketsPagedAsync(TicketPagedRequest request, Guid technicienId)
        {
            return await MeasureAsync(nameof(GetMesTicketsPagedAsync), request, async () =>
            {
                try
                {
                    _logger.LogInformation("Début GetMesTicketsPagedAsync - TechnicienId: {TechnicienId}, Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
                        technicienId, request.Page, request.PageSize, request.SearchTerm);

                    // 1. Construire le filtre de base
                    var baseFilter = BuildFilter(request);

                    // 2. Ajouter le filtre par technicien assigné
                    Expression<Func<TicketEntity, bool>> technicienFilter = t => t.AssigneeId == technicienId;

                    // 3. Combiner les filtres
                    Expression<Func<TicketEntity, bool>> combinedFilter;
                    if (baseFilter != null)
                    {
                        combinedFilter = baseFilter.AndAlso(technicienFilter);
                    }
                    else
                    {
                        combinedFilter = technicienFilter;
                    }

                    // 4. Obtenir la requête avec le filtre combiné
                    var query = _ticketRepository.GetFilteredQuery(combinedFilter);

                    // 5. Appliquer le tri
                    if (!string.IsNullOrWhiteSpace(request.SortBy))
                    {
                        query = ApplySorting(query, request.SortBy, request.SortDescending);
                    }
                    else
                    {
                        query = query.OrderByDescending(t => t.DateCreation);
                    }

                    // 6. Compter le total
                    var totalCount = await query.CountAsync();

                    // 7. Pagination
                    var items = await query
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .ToListAsync();

                    // 8. Mapper vers DTO
                    var dtos = new List<TicketDTO>();
                    foreach (var ticket in items)
                    {
                        dtos.Add(await MapToDto(ticket));
                    }

                    // 9. Créer le résultat paginé
                    var pagedResult = PagedResult<TicketDTO>.Create(
                        dtos,
                        totalCount,
                        request.Page,
                        request.PageSize
                    );

                    return ApiResponse<PagedResult<TicketDTO>>.Success(pagedResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des tickets du technicien");
                    return ApiResponse<PagedResult<TicketDTO>>.Failure("Erreur interne du serveur: " + ex.Message);
                }
            });
        }
        #endregion
    }
}
