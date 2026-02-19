// Fichier: projet0.Application/Services/Ticket/TicketService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Diagnostics;
using TicketEntity = projet0.Domain.Entities.Ticket;  // Alias pour éviter les conflits

namespace projet0.Application.Services.Ticket
{
   

    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TicketService> _logger;
        private readonly IMapper _mapper;

        public TicketService(
            ITicketRepository ticketRepository,
            IUserRepository userRepository,
            ILogger<TicketService> logger,
            IMapper mapper)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _logger = logger;
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

            // Compter les relations
            dto.NombreCommentaires = ticket.Commentaires?.Count ?? 0;
            dto.NombrePiecesJointes = ticket.Commentaires?
                .SelectMany(c => c.PiecesJointes)
                .Count() ?? 0;

            return dto;
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

        public async Task<ApiResponse<List<TicketDTO>>> GetAllTicketsAsync()
        {
            return await MeasureAsync(nameof(GetAllTicketsAsync), null, async () =>
            {
                try
                {
                    var tickets = await _ticketRepository.GetAllAsync();
                    var dtos = new List<TicketDTO>();

                    foreach (var ticket in tickets)
                    {
                        dtos.Add(await MapToDto(ticket));
                    }

                    return ApiResponse<List<TicketDTO>>.Success(dtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération de tous les tickets");
                    return ApiResponse<List<TicketDTO>>.Failure("Erreur interne du serveur");
                }
            });
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

                // Créer le ticket (tous les champs automatiques sont générés ici)
                var ticket = new TicketEntity
                {
                    Id = Guid.NewGuid(),                          // ✅ Auto-généré
                    ReferenceTicket = reference,                   // ✅ Auto-généré (ex: TCK-2026-001)
                    TitreTicket = dto.TitreTicket,                // ✅ Fourni par l'utilisateur
                    DescriptionTicket = dto.DescriptionTicket ?? string.Empty, // ✅ Fourni par l'utilisateur
                    StatutTicket = dto.StatutTicket,              // ✅ Fourni par l'utilisateur
                    PrioriteTicket = dto.PrioriteTicket,          // ✅ Fourni par l'utilisateur
                    DateCreation = DateTime.UtcNow,               // ✅ Auto-généré
                    CreateurId = createurId,                       // ✅ Auto-généré (utilisateur connecté)
                    AssigneeId = null,                              // ✅ Null pour l'instant
                    CreatedAt = DateTime.UtcNow,                   // ✅ Auto-généré

                    // Initialisation des collections
                    Historiques = new List<HistoriqueTicket>(),
                    Commentaires = new List<CommentaireTicket>(),
                    Notifications = new List<Notification>()
                };

                // Ajouter un historique de création
                ticket.Historiques.Add(new HistoriqueTicket
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    AncienStatut = dto.StatutTicket,  // Même statut pour la création
                    NouveauStatut = dto.StatutTicket,
                    DateChangement = DateTime.UtcNow,
                    ModifieParId = createurId
                });

                await _ticketRepository.AddAsync(ticket);
                await _ticketRepository.SaveChangesAsync();

                var result = await MapToDto(ticket);

                sw.Stop();
                _logger.LogInformation("CreateTicket SUCCESS | Ref: {Reference} | Duration: {Ms} ms",
                    reference, sw.ElapsedMilliseconds);

                return ApiResponse<TicketDTO>.Success(result, $"Ticket {reference} créé avec succès");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CreateTicket ERROR | Duration: {Ms} ms", sw.ElapsedMilliseconds);
                return ApiResponse<TicketDTO>.Failure("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<TicketDTO>> GetTicketByIdAsync(Guid id)
        {
            return await MeasureAsync(nameof(GetTicketByIdAsync), new { id }, async () =>
            {
                try
                {
                    var ticket = await _ticketRepository.GetByIdAsync(id);

                    if (ticket == null)
                        return ApiResponse<TicketDTO>.Failure($"Ticket avec ID {id} non trouvé");

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



        #endregion
    }
}