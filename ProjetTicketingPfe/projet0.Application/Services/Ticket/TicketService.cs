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
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Diagnostics;
using System.Linq.Expressions;
using TicketEntity = projet0.Domain.Entities.Ticket;

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

        public TicketService(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<TicketService> logger,
            IWebHostEnvironment environment,   
            IPieceJointeService pieceJointeService,
            ICommentaireService commentaireService,  
            IMapper mapper)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
            _environment = environment;     
            _pieceJointeService = pieceJointeService;
            _commentaireService = commentaireService;  
            _mapper = mapper;
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

        private async Task<TicketDTO> MapToDto(TicketEntity ticket)
        {
            try
            {
                _logger.LogDebug("Début MapToDto pour ticket {Id} - {Reference}", ticket.Id, ticket.ReferenceTicket);

                var dto = _mapper.Map<TicketDTO>(ticket);

                // Libellés
                dto.StatutTicketLibelle = GetStatutLibelle(ticket.StatutTicket);
                dto.PrioriteTicketLibelle = GetPrioriteLibelle(ticket.PrioriteTicket);

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

                // VERSION ROBUSTE - Compter les relations avec vérifications null
                if (ticket.Commentaires != null)
                {
                    dto.NombreCommentaires = ticket.Commentaires.Count;

                    // Compter les pièces jointes de manière sécurisée
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
                _logger.LogError(ex, "❌ Erreur dans MapToDto pour ticket {Id}", ticket?.Id);
                throw;
            }
        }
        private string GetStatutLibelle(StatutTicket statut)
        {
            return statut switch
            {
                StatutTicket.Nouveau => "Nouveau",
                StatutTicket.Assigne => "Assigné",
                StatutTicket.EnCours => "En cours",
                StatutTicket.EnAttente => "En attente",
                StatutTicket.Resolu => "Résolu",
                StatutTicket.Cloture => "Clôturé",
                StatutTicket.Reouvert => "Réouvert",
                _ => statut.ToString()
            };
        }

        private string GetPrioriteLibelle(PrioriteTicket priorite)
        {
            return priorite switch
            {
                PrioriteTicket.Basse => "Basse",
                PrioriteTicket.Normale => "Normale",
                PrioriteTicket.Haute => "Haute",
                _ => priorite.ToString()
            };
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

            // Filtre par priorité
            if (request.Priorite.HasValue)
            {
                predicates.Add(t => t.PrioriteTicket == request.Priorite.Value);
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
                ("priorite", false) => query.OrderBy(t => t.PrioriteTicket),
                ("priorite", true) => query.OrderByDescending(t => t.PrioriteTicket),
                _ => query.OrderByDescending(t => t.DateCreation)
            };
        }

        public async Task<ApiResponse<TicketDTO>> CreateTicketAsync(CreateTicketDTO dto, Guid createurId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("CreateTicket START | Titre: {Titre}, Priorité: {Priorite}, Statut: {Statut}",
                dto.TitreTicket, dto.PrioriteTicket, dto.StatutTicket);

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(dto.TitreTicket))
                    return ApiResponse<TicketDTO>.Failure("Le titre est requis");

                // Générer la référence unique
                var reference = await _ticketRepository.GenerateReferenceTicketAsync();

                // Créer le ticket
                var ticket = new TicketEntity
                {
                    Id = Guid.NewGuid(),
                    ReferenceTicket = reference,
                    TitreTicket = dto.TitreTicket,
                    DescriptionTicket = dto.DescriptionTicket ?? string.Empty,
                    StatutTicket = dto.StatutTicket,
                    PrioriteTicket = dto.PrioriteTicket,
                    DateCreation = DateTime.UtcNow,
                    CreateurId = createurId,
                    AssigneeId = null, // AssigneeId a été retiré du DTO
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
                    AncienStatut = dto.StatutTicket,
                    NouveauStatut = dto.StatutTicket,
                    DateChangement = DateTime.UtcNow,
                    ModifieParId = createurId
                });

                // Sauvegarder le ticket d'abord
                await _ticketRepository.AddAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                // Mapper le résultat
                var result = await MapToDto(ticket);

                sw.Stop();
                _logger.LogInformation("CreateTicket SUCCESS | Ref: {Reference} | Duration: {Ms} ms",
                    reference, sw.ElapsedMilliseconds);

                return ApiResponse<TicketDTO>.Success(result, $"Ticket {reference} créé avec succès. Utilisez POST /api/commentaires?ticketId={ticket.Id} pour ajouter des commentaires.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CreateTicket ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
                return ApiResponse<TicketDTO>.Failure("Erreur interne du serveur");
            }
        }

        // NOUVELLE MÉTHODE: GetTicketDetailAsync
        public async Task<ApiResponse<TicketDetailDTO>> GetTicketDetailAsync(Guid id)
        {
            _logger.LogInformation("=== TicketService.GetTicketDetailAsync ===");
            _logger.LogInformation("ID reçu: {Id}", id);

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
        public async Task<ApiResponse<bool>> DeleteTicketAsync(Guid id)
        {
            return await MeasureAsync(nameof(DeleteTicketAsync), new { id }, async () =>
            {
                try
                {
                    var ticket = await _ticketRepository.GetByIdAsync(id);

                    if (ticket == null)
                        return ApiResponse<bool>.Failure($"Ticket avec ID {id} non trouvé");

                    // Vérifier si le ticket peut être supprimé (optionnel)
                    if (ticket.StatutTicket == StatutTicket.Cloture)
                    {
                        return ApiResponse<bool>.Failure("Impossible de supprimer un ticket clôturé");
                    }

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
        private TypePieceJointe DeterminerTypePieceJointe(string nomFichier)
        {
            var extension = Path.GetExtension(nomFichier).ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => TypePieceJointe.Image,
                ".pdf" or ".doc" or ".docx" or ".txt" => TypePieceJointe.Document,
                ".xls" or ".xlsx" or ".csv" => TypePieceJointe.Tableur,
                ".zip" or ".rar" or ".7z" => TypePieceJointe.Archive,
                _ => TypePieceJointe.Autre
            };
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
                dto.PrioriteTicketLibelle = GetPrioriteLibelle(ticket.PrioriteTicket);

                // 3. Nom du créateur
                if (ticket.Createur != null)
                {
                    dto.CreateurNom = $"{ticket.Createur.Nom} {ticket.Createur.Prenom}";
                    _logger.LogDebug("Créateur trouvé: {CreateurNom}", dto.CreateurNom);
                }
                else if (ticket.CreateurId != Guid.Empty)
                {
                    _logger.LogDebug("Recherche du créateur par ID: {CreateurId}", ticket.CreateurId);
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
                        _logger.LogDebug("Recherche de l'assigné par ID: {AssigneeId}", ticket.AssigneeId);
                        var user = await _userRepository.GetByIdAsync(ticket.AssigneeId.Value);
                        dto.AssigneeNom = user != null ? $"{user.Nom} {user.Prenom}" : "Utilisateur inconnu";
                    }
                }

                // 5. Mapper les commentaires
                if (ticket.Commentaires != null && ticket.Commentaires.Any())
                {
                    _logger.LogDebug("Mapping de {Count} commentaires", ticket.Commentaires.Count);

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
                            Taille = p.Taille,
                            ContentType = p.ContentType,
                            TypePieceJointe = p.TypePieceJointe,
                            DateAjout = p.DateAjout
                        }).ToList() ?? new()
                    }).ToList();
                }
                else
                {
                    dto.Commentaires = new List<CommentaireDTO>();
                    _logger.LogDebug("Aucun commentaire trouvé");
                }

                // 6. Compter les relations
                dto.NombreCommentaires = dto.Commentaires.Count;
                dto.NombrePiecesJointes = dto.Commentaires
                    .SelectMany(c => c.PiecesJointes)
                    .Count();

                _logger.LogDebug("MapToDetailDto terminé pour ticket {Id} - {Commentaires} commentaires, {Pieces} pièces jointes",
                    ticket.Id, dto.NombreCommentaires, dto.NombrePiecesJointes);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur dans MapToDetailDto pour ticket {Id}", ticket?.Id);
                throw;
            }
        }

        public async Task<ApiResponse<UpdateTicketResponseDTO>> UpdateTicketAsync(Guid id, UpdateTicketDTO dto, Guid userId)
        {
            return await MeasureAsync(nameof(UpdateTicketAsync), new { id, dto }, async () =>
            {
                try
                {
                    _logger.LogInformation("Début UpdateTicketAsync pour ticket {Id}", id);

                    // 1. Récupérer le ticket avec tous ses commentaires
                    var ticket = await _ticketRepository.GetTicketWithDetailsAsync(id);
                    if (ticket == null)
                        return ApiResponse<UpdateTicketResponseDTO>.Failure($"Ticket avec ID {id} non trouvé");

                    var commentairesModifies = new List<Guid>();
                    var piecesJointesSupprimees = new Dictionary<Guid, List<Guid>>();
                    var piecesJointesAjoutees = new Dictionary<Guid, List<Guid>>();

                    // 2. Mettre à jour les champs simples du ticket
                    if (!string.IsNullOrWhiteSpace(dto.TitreTicket))
                        ticket.TitreTicket = dto.TitreTicket;

                    if (dto.DescriptionTicket != null)
                        ticket.DescriptionTicket = dto.DescriptionTicket;

                    if (dto.PrioriteTicket.HasValue)
                        ticket.PrioriteTicket = dto.PrioriteTicket.Value;

                    if (dto.StatutTicket.HasValue)
                    {
                        var ancienStatut = ticket.StatutTicket;
                        ticket.StatutTicket = dto.StatutTicket.Value;

                        if (ancienStatut != dto.StatutTicket.Value)
                        {
                            ticket.Historiques ??= new List<HistoriqueTicket>();
                            ticket.Historiques.Add(new HistoriqueTicket
                            {
                                Id = Guid.NewGuid(),
                                TicketId = ticket.Id,
                                AncienStatut = ancienStatut,
                                NouveauStatut = dto.StatutTicket.Value,
                                DateChangement = DateTime.UtcNow,
                                ModifieParId = userId
                            });
                        }
                    }

                    if (dto.AssigneeId.HasValue)
                    {
                        var assignee = await _userRepository.GetByIdAsync(dto.AssigneeId.Value);
                        if (assignee == null)
                            return ApiResponse<UpdateTicketResponseDTO>.Failure("L'utilisateur assigné n'existe pas");

                        ticket.AssigneeId = dto.AssigneeId;
                    }

                    ticket.UpdatedAt = DateTime.UtcNow;

                    // 3. Gérer les commentaires existants
                    if (dto.Commentaires != null && dto.Commentaires.Any())
                    {
                        _logger.LogInformation("Traitement de {Count} commentaire(s)", dto.Commentaires.Count);

                        foreach (var commentaireDto in dto.Commentaires)
                        {
                            _logger.LogInformation("DTO reçu - ID: {Id}, Message: {Message}, EffacerMessage: {Effacer}, EstInterne: {EstInterne}",
                                commentaireDto.Id,
                                commentaireDto.Message ?? "null",
                                commentaireDto.NouveauxFichiers,
                                commentaireDto.EstInterne);

                            if (commentaireDto.NouveauxFichiers != null)
                                _logger.LogInformation("Nombre de nouveaux fichiers: {Count}", commentaireDto.NouveauxFichiers.Count);

                            if (commentaireDto.PiecesJointesASupprimer != null)
                                _logger.LogInformation("Pièces jointes à supprimer: {Ids}", string.Join(", ", commentaireDto.PiecesJointesASupprimer));

                            var commentaireExistant = ticket.Commentaires?
                                .FirstOrDefault(c => c.Id == commentaireDto.Id);

                            if (commentaireExistant != null)
                            {
                                _logger.LogInformation("Commentaire trouvé, message actuel: '{Message}'", commentaireExistant.Message);

                                // ✅ CORRECTION: Ajouter tous les paramètres
                                await MettreAJourCommentaire(
                                    commentaireExistant,           // commentaire
                                    commentaireDto,                 // dto
                                    userId,                         // userId
                                    piecesJointesSupprimees,        // piecesJointesSupprimees
                                    piecesJointesAjoutees           // piecesJointesAjoutees
                                );

                                commentairesModifies.Add(commentaireExistant.Id);
                                _logger.LogInformation("Commentaire {CommentaireId} mis à jour", commentaireExistant.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Commentaire avec ID {CommentaireId} NON TROUVÉ dans le ticket", commentaireDto.Id);
                            }
                        }
                    }

                    // 4. SAUVEGARDER UNE SEULE FOIS (et avant de recharger)
                    await _ticketRepository.SaveChangesAsync();

                    // 5. MAINTENANT on peut recharger pour la réponse
                    var responseDto = await MapToDetailDto(ticket);

                    var updateResponse = new UpdateTicketResponseDTO
                    {
                        Id = responseDto.Id,
                        ReferenceTicket = responseDto.ReferenceTicket,
                        TitreTicket = responseDto.TitreTicket,
                        DescriptionTicket = responseDto.DescriptionTicket,
                        StatutTicket = responseDto.StatutTicket,
                        StatutTicketLibelle = responseDto.StatutTicketLibelle,
                        PrioriteTicket = responseDto.PrioriteTicket,
                        PrioriteTicketLibelle = responseDto.PrioriteTicketLibelle,
                        DateCreation = responseDto.DateCreation,
                        DateCloture = responseDto.DateCloture,
                        CreateurId = responseDto.CreateurId,
                        CreateurNom = responseDto.CreateurNom,
                        AssigneeId = responseDto.AssigneeId,
                        AssigneeNom = responseDto.AssigneeNom,
                        NombreCommentaires = responseDto.NombreCommentaires,
                        NombrePiecesJointes = responseDto.NombrePiecesJointes,
                        Commentaires = responseDto.Commentaires,
                        CommentairesModifies = commentairesModifies,
                        PiecesJointesSupprimees = piecesJointesSupprimees,
                        PiecesJointesAjoutees = piecesJointesAjoutees
                    };

                    return ApiResponse<UpdateTicketResponseDTO>.Success(updateResponse, "Ticket mis à jour avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la mise à jour du ticket {Id}", id);
                    return ApiResponse<UpdateTicketResponseDTO>.Failure($"Erreur interne: {ex.Message}");
                }
            });
        }

        private async Task MettreAJourCommentaire(
            CommentaireTicket commentaire,
            UpdateCommentaireDTO dto,
            Guid userId,
            Dictionary<Guid, List<Guid>> piecesJointesSupprimees,
            Dictionary<Guid, List<Guid>> piecesJointesAjoutees)
        {
            _logger.LogInformation("=== DÉBUT MISE À JOUR COMMENTAIRE {CommentaireId} ===", commentaire.Id);

            // Gérer le message
            if (dto.EffacerMessage)
            {
                commentaire.Message = string.Empty;
                _logger.LogInformation("Message effacé pour commentaire {CommentaireId}", commentaire.Id);
            }
            else if (dto.Message != null)
            {
                commentaire.Message = dto.Message;
                _logger.LogInformation("Message mis à jour pour commentaire {CommentaireId}: '{Message}'",
                    commentaire.Id, dto.Message);
            }

            // Mettre à jour EstInterne
            commentaire.EstInterne = dto.EstInterne;
            _logger.LogInformation("EstInterne mis à jour: {EstInterne}", dto.EstInterne);

            // Supprimer les pièces jointes
            if (dto.PiecesJointesASupprimer != null && dto.PiecesJointesASupprimer.Any())
            {
                _logger.LogInformation("Suppression de {Count} pièce(s) jointe(s)", dto.PiecesJointesASupprimer.Count);

                var piecesSupprimees = new List<Guid>();

                foreach (var pieceId in dto.PiecesJointesASupprimer)
                {
                    _logger.LogInformation("Tentative de suppression de la pièce jointe {PieceId}", pieceId);

                    var piece = commentaire.PiecesJointes?.FirstOrDefault(p => p.Id == pieceId);
                    if (piece != null)
                    {
                        var success = await _pieceJointeService.SupprimerFichierAsync(pieceId);
                        if (success)
                        {
                            piecesSupprimees.Add(pieceId);
                            _logger.LogInformation("Pièce jointe {PieceId} supprimée avec succès", pieceId);
                        }
                        else
                        {
                            _logger.LogWarning("Échec de suppression de la pièce jointe {PieceId}", pieceId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Pièce jointe {PieceId} non trouvée dans le commentaire", pieceId);
                    }
                }

                if (piecesSupprimees.Any())
                {
                    piecesJointesSupprimees[commentaire.Id] = piecesSupprimees;
                }
            }

            // Ajouter de nouveaux fichiers
            if (dto.NouveauxFichiers != null && dto.NouveauxFichiers.Any())
            {
                _logger.LogInformation("Ajout de {Count} nouveau(x) fichier(s)", dto.NouveauxFichiers.Count);

                var piecesAjoutees = new List<Guid>();

                foreach (var fichier in dto.NouveauxFichiers)
                {
                    _logger.LogInformation("Traitement du fichier: {FileName}", fichier.FileName);

                    var base64Data = await ConvertirFichierEnBase64(fichier);

                    var pieceDto = new CreatePieceJointeDTO
                    {
                        NomFichier = fichier.FileName,
                        Taille = fichier.Length,
                        ContentType = fichier.ContentType,
                        TypePieceJointe = DeterminerTypePieceJointe(fichier.FileName),
                        ContenuBase64 = base64Data
                    };

                    var pieceJointe = await _pieceJointeService.SauvegarderFichierAsync(
                        pieceDto, commentaire.Id, userId);

                    piecesAjoutees.Add(pieceJointe.Id);
                    _logger.LogInformation("Fichier sauvegardé avec ID: {PieceId}", pieceJointe.Id);
                }

                if (piecesAjoutees.Any())
                {
                    piecesJointesAjoutees[commentaire.Id] = piecesAjoutees;
                }
            }

            _logger.LogInformation("=== FIN MISE À JOUR COMMENTAIRE {CommentaireId} ===", commentaire.Id);
        }

        // Helper pour convertir IFormFile en Base64
        private async Task<string> ConvertirFichierEnBase64(IFormFile fichier)
        {
            using var memoryStream = new MemoryStream();
            await fichier.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            return Convert.ToBase64String(bytes);
        }
        #endregion
    } 
}
