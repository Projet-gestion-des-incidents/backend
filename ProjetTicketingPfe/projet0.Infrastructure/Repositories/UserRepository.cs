using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projet0.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly IWebHostEnvironment _environment;  // ✅ AJOUTER



        public UserRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<UserRepository> logger,
            IWebHostEnvironment environment)
            : base(context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context; // Ajouter cette ligne
            _logger = logger; // Ajouter cette ligne
            _environment = environment;  // ✅ AJOUTER

        }

        public Task<ApplicationUser> GetByEmailAsync(string email) => _userManager.FindByEmailAsync(email);

        public Task<ApplicationUser> GetByUserNameAsync(string userName) => _userManager.FindByNameAsync(userName);
        public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Créer sans mot de passe (si jamais nécessaire)
                return await _userManager.CreateAsync(user);
            }
            else
            {
                // Créer avec mot de passe
                return await _userManager.CreateAsync(user, password);
            }
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string roleName)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users;
        }

        public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync()
        {
            return await _dbSet.OrderBy(u => u.Nom).ThenBy(u => u.Prenom).ToListAsync();
        }
        public async Task<PagedResult<UserWithRoleDto>> GetAllUsersWithRolesAsync(PagedRequest request)
        {
            // 1. Créer la query de base
            var query = _userManager.Users.AsQueryable();

            // 2. Recherche globale si SearchTerm est fourni
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Nom.ToLower().Contains(term) ||
                    u.Prenom.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.UserName.ToLower().Contains(term));
            }

            // 3. Appliquer le tri
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            // 4. Compter le total (avant pagination)
            var totalCount = await query.CountAsync();

            // 5. Appliquer la pagination
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var skip = (page - 1) * pageSize;
            if (skip >= totalCount && totalCount > 0)
            {
                page = (int)Math.Ceiling(totalCount / (double)pageSize);
                skip = (page - 1) * pageSize;
            }

            var paginatedUsers = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // 6. Convertir en DTO avec rôles
            var usersWithRoles = new List<UserWithRoleDto>();
            foreach (var user in paginatedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "USER";

                usersWithRoles.Add(new UserWithRoleDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Image = user.Image,
                    Role = roleName,
                    Statut = user.Statut,
                    BirthDate = user.BirthDate,
                    Adresse = user.Adresse  // ✅ AJOUTER CETTE LIGNE

                });
            }

            // 7. Retourner le résultat paginé
            return PagedResult<UserWithRoleDto>.Create(
                items: usersWithRoles,
                totalCount: totalCount,
                page: page,
                pageSize: pageSize
            );
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Email == email);
            if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName, Guid? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.UserName == userName);
            if (excludeUserId.HasValue) query = query.Where(u => u.Id != excludeUserId.Value);
            return !await query.AnyAsync();
        }

        public async Task<IList<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            return user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        }

        public async Task<bool> AddUserToRoleAsync(Guid userId, string roleName)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }
        public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<bool> RemoveUserFromRoleAsync(Guid userId, string roleName)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        public Task<IdentityResult> RestoreAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> SoftDeleteAsync(ApplicationUser user)
        {
            throw new NotImplementedException();
        }
        
        public async Task<(IEnumerable<UserWithRoleDto> Users, int TotalCount)> SearchUsersAsync(
            UserSearchRequest request)
        {
            // 1. Créer la query de base
            var query = _userManager.Users.AsQueryable();

            // 2. Recherche globale
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Nom.ToLower().Contains(term) ||
                    u.Prenom.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.UserName.ToLower().Contains(term) ||
            (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
            (u.Adresse != null && u.Adresse.ToLower().Contains(term)));
            }

            // 3. Filtres additionnels
            if (request.Statut.HasValue)
                query = query.Where(u => u.Statut == request.Statut.Value);

            if (!string.IsNullOrWhiteSpace(request.UserName))
                query = query.Where(u => u.UserName.Contains(request.UserName));

            if (!string.IsNullOrWhiteSpace(request.Email))
                query = query.Where(u => u.Email.Contains(request.Email));

            if (!string.IsNullOrWhiteSpace(request.Nom))
                query = query.Where(u => u.Nom.Contains(request.Nom));

            if (!string.IsNullOrWhiteSpace(request.Prenom))
                query = query.Where(u => u.Prenom.Contains(request.Prenom));

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(request.PhoneNumber));

            // 4. MODIFICATION : Filtrer par année de naissance (NOUVEAU)
            if (request.BirthYear.HasValue)
            {
                query = query.Where(u => u.BirthDate.HasValue &&
                                         u.BirthDate.Value.Year == request.BirthYear.Value);
            }

            // CONSERVER : Filtrer par date complète (pour compatibilité)
            if (request.BirthDate.HasValue)
            {
                query = query.Where(u => u.BirthDate.HasValue &&
                                         u.BirthDate.Value.Date == request.BirthDate.Value.Date);
            }

            // 5. Appliquer le tri
            query = ApplySorting(query, request.SortBy, request.SortDescending);

            // 6. Compter le total (avant pagination)
            var totalCount = await query.CountAsync();

            // 7. Appliquer la pagination
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var skip = (page - 1) * pageSize;
            if (skip >= totalCount && totalCount > 0)
            {
                page = (int)Math.Ceiling(totalCount / (double)pageSize);
                skip = (page - 1) * pageSize;
            }

            var paginatedUsers = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // 8. Convertir en DTO avec rôles
            var usersWithRoles = new List<UserWithRoleDto>();
            foreach (var user in paginatedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "USER";

                // Filtrer par rôle si spécifié
                if (string.IsNullOrWhiteSpace(request.Role) ||
                    string.Equals(roleName, request.Role.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    usersWithRoles.Add(new UserWithRoleDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Image = user.Image,
                        Role = roleName,
                        Statut = user.Statut,
                        BirthDate = user.BirthDate,
                        Adresse = user.Adresse  // ✅ AJOUTER CETTE LIGNE

                    });
                }
            }

            // 9. Ajuster le totalCount après filtrage par rôle
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                totalCount = usersWithRoles.Count;
            }

            return (usersWithRoles, totalCount);
        }

        // Méthode helper pour le tri (identique à celle de UserService)
        private IQueryable<ApplicationUser> ApplySorting(
            IQueryable<ApplicationUser> query,
            string sortBy,
            bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(u => u.Nom);

            switch (sortBy.ToLower())
            {
                case "username":
                    return sortDescending
                        ? query.OrderByDescending(u => u.UserName)
                        : query.OrderBy(u => u.UserName);

                case "email":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email);

                case "nom":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Nom)
                        : query.OrderBy(u => u.Nom);

                case "prenom":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Prenom)
                        : query.OrderBy(u => u.Prenom);

                case "birthdate":
                    if (sortDescending)
                        return query.OrderByDescending(u => u.BirthDate.HasValue)
                                   .ThenByDescending(u => u.BirthDate);
                    else
                        return query.OrderBy(u => u.BirthDate.HasValue)
                                   .ThenBy(u => u.BirthDate);

                case "statut":
                    return sortDescending
                        ? query.OrderByDescending(u => u.Statut)
                        : query.OrderBy(u => u.Statut);

                default:
                    return query.OrderBy(u => u.Nom);
            }
        }
        // projet0.Infrastructure/Repositories/UserRepository.cs
        public async Task<IEnumerable<TechnicienDto>> GetTechniciensAsync()
        {
            // Récupérer tous les utilisateurs avec le rôle "Technicien"
            var techniciens = await _userManager.GetUsersInRoleAsync("Technicien");

            return techniciens.Select(t => new TechnicienDto
            {
                Id = t.Id,
                Nom = t.Nom,
                Prenom = t.Prenom,
                Email = t.Email
            }).OrderBy(t => t.Nom).ThenBy(t => t.Prenom);
        }
        public async Task<IdentityResult> DeleteUserWithCascadeAsync(ApplicationUser user)
        {
            // Utiliser une transaction pour assurer l'intégrité des données
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Vérifier le rôle de l'utilisateur
                var roles = await _userManager.GetRolesAsync(user);
                var isCommercant = roles.Contains("Commercant");
                var isTechnicien = roles.Contains("Technicien");

                // ============================================
                // 1. POUR UN COMMERÇANT
                // ============================================
                if (isCommercant)
                {
                    // 1.1 Récupérer tous les TPEs du commerçant (détacher)
                    var tpes = await _context.TPEs
                        .Where(t => t.CommercantId == user.Id)
                        .ToListAsync();

                    if (tpes.Any())
                    {
                        foreach (var tpe in tpes)
                        {
                            // Supprimer les liaisons Incident-TPE
                            var incidentTPEs = await _context.IncidentTPEs
                                .Where(it => it.TPEId == tpe.Id)
                                .ToListAsync();
                            if (incidentTPEs.Any())
                            {
                                _context.IncidentTPEs.RemoveRange(incidentTPEs);
                            }
                            tpe.CommercantId = null;
                        }
                        _context.TPEs.UpdateRange(tpes);
                        _logger.LogInformation("{Count} TPE(s) détachés du commerçant {UserId}", tpes.Count, user.Id);
                    }

                    // 1.2 Récupérer tous les incidents ET les tickets liés
                    var incidentsCommercant = await _context.Incidents
                        .Include(i => i.IncidentTickets)
                            .ThenInclude(it => it.Ticket)
                        .Where(i => i.CreatedById == user.Id)
                        .ToListAsync();

                    // Récupérer TOUS les tickets liés aux incidents (avant suppression)
                    var ticketsLiesAuxIncidents = new List<Ticket>();

                    if (incidentsCommercant.Any())
                    {
                        foreach (var incident in incidentsCommercant)
                        {
                            // Récupérer les tickets liés à cet incident
                            var ticketsLies = incident.IncidentTickets?
                                .Select(it => it.Ticket)
                                .Where(t => t != null)
                                .ToList() ?? new List<Ticket>();

                            ticketsLiesAuxIncidents.AddRange(ticketsLies);

                            _logger.LogInformation("Incident {IncidentId} lié à {Count} ticket(s)", incident.Id, ticketsLies.Count);

                            // Supprimer les liaisons Incident-TPE
                            var incidentTPEs = await _context.IncidentTPEs
                                .Where(it => it.IncidentId == incident.Id)
                                .ToListAsync();
                            if (incidentTPEs.Any())
                            {
                                _context.IncidentTPEs.RemoveRange(incidentTPEs);
                                _logger.LogInformation("Suppression de {Count} liaisons Incident-TPE pour incident {IncidentId}",
                                    incidentTPEs.Count, incident.Id);
                            }

                            // Supprimer les liaisons Incident-Ticket
                            if (incident.IncidentTickets != null && incident.IncidentTickets.Any())
                            {
                                _context.IncidentTickets.RemoveRange(incident.IncidentTickets);
                                _logger.LogInformation("Suppression de {Count} liaisons Incident-Ticket pour incident {IncidentId}",
                                    incident.IncidentTickets.Count, incident.Id);
                            }

                            // Supprimer les pièces jointes de l'incident (fichiers physiques)
                            var piecesJointesIncident = await _context.PiecesJointes
                                .Where(p => p.IncidentId == incident.Id)
                                .ToListAsync();
                            if (piecesJointesIncident.Any())
                            {
                                foreach (var piece in piecesJointesIncident)
                                {
                                    var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "incidents", piece.NomFichier);
                                    if (File.Exists(filePath))
                                    {
                                        try
                                        {
                                            File.Delete(filePath);
                                            _logger.LogInformation("Fichier physique supprimé: {FilePath}", filePath);
                                        }
                                        catch { }
                                    }
                                }
                                _context.PiecesJointes.RemoveRange(piecesJointesIncident);
                            }
                        }

                        // Supprimer les incidents
                        _context.Incidents.RemoveRange(incidentsCommercant);
                        _logger.LogInformation("Suppression de {Count} incident(s) du commerçant {UserId}",
                            incidentsCommercant.Count, user.Id);

                        // ✅ CRUCIAL : Sauvegarder les changements en base AVANT de vérifier les tickets
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("SaveChanges effectué - Incidents et liaisons supprimés en base");

                        // ✅ MAINTENANT, vérifier et supprimer les tickets qui n'ont plus d'incidents
                        var ticketsUniques = ticketsLiesAuxIncidents.Distinct().ToList();
                        _logger.LogInformation("Tickets uniques à vérifier: {Count}", ticketsUniques.Count);

                        foreach (var ticket in ticketsUniques)
                        {
                            _logger.LogInformation("Vérification du ticket {TicketId}", ticket.Id);

                            // Recharger le ticket pour vérifier s'il a encore des incidents
                            var ticketAvecIncidents = await _context.Tickets
                                .Include(t => t.IncidentTickets)
                                .FirstOrDefaultAsync(t => t.Id == ticket.Id);

                            if (ticketAvecIncidents == null)
                            {
                                _logger.LogWarning("Ticket {TicketId} introuvable lors du rechargement", ticket.Id);
                                continue;
                            }

                            var incidentsRestants = ticketAvecIncidents.IncidentTickets?
                                .Select(it => it.Incident)
                                .Where(i => i != null)
                                .ToList() ?? new List<Incident>();

                            _logger.LogInformation("Ticket {TicketId} a {Count} incident(s) restant(s) EN BASE",
                                ticket.Id, incidentsRestants.Count);

                            if (!incidentsRestants.Any())
                            {
                                _logger.LogWarning("✅ Ticket {TicketId} n'a plus d'incidents, suppression en cours...", ticket.Id);

                                // Supprimer les commentaires du ticket et leurs pièces jointes
                                var commentairesTicket = await _context.CommentairesTicket
                                    .Include(c => c.PiecesJointes)
                                    .Where(c => c.TicketId == ticket.Id)
                                    .ToListAsync();

                                _logger.LogInformation("Ticket {TicketId} a {Count} commentaire(s) à supprimer",
                                    ticket.Id, commentairesTicket.Count);

                                foreach (var commentaire in commentairesTicket)
                                {
                                    if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                                    {
                                        foreach (var piece in commentaire.PiecesJointes)
                                        {
                                            var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "commentaires", piece.NomFichier);
                                            if (File.Exists(filePath))
                                            {
                                                try
                                                {
                                                    File.Delete(filePath);
                                                    _logger.LogInformation("Fichier commentaire supprimé: {FilePath}", filePath);
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                                _context.CommentairesTicket.RemoveRange(commentairesTicket);

                                // Supprimer le ticket
                                _context.Tickets.Remove(ticketAvecIncidents);
                                _logger.LogInformation("✅ Ticket {TicketId} supprimé définitivement", ticket.Id);
                            }
                            else
                            {
                                _logger.LogInformation("❌ Ticket {TicketId} conserve {Count} incident(s), non supprimé (attendu: 0)",
                                    ticket.Id, incidentsRestants.Count);
                            }
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("SaveChanges effectué après suppression des tickets orphelins");
                    }
                }
                // ============================================
                // 2. POUR UN TECHNICIEN
                // ============================================
                if (isTechnicien)
                {
                    // 2.1 Gérer les tickets assignés au technicien
                    var ticketsAssignes = await _context.Tickets
                        .Include(t => t.IncidentTickets)
                            .ThenInclude(it => it.Incident)
                        .Where(t => t.AssigneeId == user.Id)
                        .ToListAsync();

                    if (ticketsAssignes.Any())
                    {
                        foreach (var ticket in ticketsAssignes)
                        {
                            var ancienStatut = ticket.StatutTicket;

                            ticket.AssigneeId = null;

                            if (ticket.StatutTicket == StatutTicket.Resolu)
                            {
                                _logger.LogInformation("Ticket {TicketId} (Résolu) - Désassigné, statut conservé", ticket.Id);
                            }
                            else
                            {
                                ticket.StatutTicket = null;
                                _logger.LogInformation("Ticket {TicketId} ({AncienStatut}) - Désassigné, statut → null",
                                    ticket.Id, ancienStatut);
                            }

                            // ✅ Mettre à jour les incidents liés à ce ticket
                            if (ticket.IncidentTickets != null && ticket.IncidentTickets.Any())
                            {
                                foreach (var lien in ticket.IncidentTickets)
                                {
                                    if (lien.Incident != null)
                                    {
                                        // Vérifier si l'incident a encore des tickets en cours
                                        var autresTicketsEnCours = await _context.IncidentTickets
                                            .Where(it => it.IncidentId == lien.IncidentId && it.TicketId != ticket.Id)
                                            .Select(it => it.Ticket)
                                            .AnyAsync(t => t.StatutTicket == StatutTicket.EnCours);

                                        if (!autresTicketsEnCours)
                                        {
                                            // Plus aucun ticket en cours lié à cet incident
                                            var incident = lien.Incident;
                                            var ancienStatutIncident = incident.StatutIncident;

                                            incident.StatutIncident = null;
                                            incident.DateResolution = null;

                                            _logger.LogInformation("Incident {IncidentId} n'a plus de tickets en cours, statut → null",
                                                incident.Id);
                                        }
                                    }
                                }
                            }

                            // Ajouter un historique
                            ticket.Historiques ??= new List<HistoriqueTicket>();
                            ticket.Historiques.Add(new HistoriqueTicket
                            {
                                Id = Guid.NewGuid(),
                                TicketId = ticket.Id,
                                AncienStatut = ancienStatut,
                                DateChangement = DateTime.UtcNow,
                                ModifieParId = user.Id
                            });
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("{Count} ticket(s) désassigné(s) du technicien {UserId}",
                            ticketsAssignes.Count, user.Id);
                    }

                    // 2.2 Tickets créés par le technicien (inchangé)
                    var ticketsCrees = await _context.Tickets
                        .Include(t => t.Commentaires)
                            .ThenInclude(c => c.PiecesJointes)
                        .Where(t => t.CreateurId == user.Id)
                        .ToListAsync();

                    foreach (var ticket in ticketsCrees)
                    {
                        if (ticket.Commentaires != null && ticket.Commentaires.Any())
                        {
                            foreach (var commentaire in ticket.Commentaires)
                            {
                                if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                                {
                                    foreach (var piece in commentaire.PiecesJointes)
                                    {
                                        var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "commentaires", piece.NomFichier);
                                        if (File.Exists(filePath))
                                        {
                                            try { File.Delete(filePath); } catch { }
                                        }
                                    }
                                }
                            }
                        }

                        if (ticket.Commentaires?.Any() == true)
                        {
                            _context.CommentairesTicket.RemoveRange(ticket.Commentaires);
                        }

                        var incidentsTicket = await _context.IncidentTickets
                            .Where(i => i.TicketId == ticket.Id)
                            .ToListAsync();
                        if (incidentsTicket.Any())
                        {
                            _context.IncidentTickets.RemoveRange(incidentsTicket);
                        }
                    }

                    if (ticketsCrees.Any())
                    {
                        _context.Tickets.RemoveRange(ticketsCrees);
                    }
                }

                // ============================================
                // 3. POUR TOUS LES UTILISATEURS (quel que soit le rôle)
                // ============================================

                // 3.1 Gérer les tickets assignés (mettre à null l'assignation)
                // ✅ Remplacer "ticketsAssignes" par "ticketsAssignesGeneraux"
                var ticketsAssignesGeneraux = await _context.Tickets
                    .Where(t => t.AssigneeId == user.Id)
                    .ToListAsync();

                if (ticketsAssignesGeneraux.Any())
                {
                    foreach (var ticket in ticketsAssignesGeneraux)
                    {
                        ticket.AssigneeId = null;
                    }
                    _logger.LogInformation("{Count} ticket(s) désassigné(s) de l'utilisateur {UserId}",
                        ticketsAssignesGeneraux.Count, user.Id);
                }
                // 3.2 Supprimer les commentaires directs (et leurs pièces jointes)
                var commentairesDirects = await _context.CommentairesTicket
                    .Include(c => c.PiecesJointes)
                    .Where(c => c.AuteurId == user.Id)
                    .ToListAsync();

                if (commentairesDirects.Any())
                {
                    foreach (var commentaire in commentairesDirects)
                    {
                        // Supprimer les fichiers physiques des pièces jointes
                        if (commentaire.PiecesJointes != null && commentaire.PiecesJointes.Any())
                        {
                            foreach (var piece in commentaire.PiecesJointes)
                            {
                                var filePath = Path.Combine(_environment.ContentRootPath, "uploads", "commentaires", piece.NomFichier);
                                if (File.Exists(filePath))
                                {
                                    try
                                    {
                                        File.Delete(filePath);
                                        _logger.LogInformation("Fichier physique supprimé: {FilePath}", filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Erreur lors de la suppression du fichier {FilePath}", filePath);
                                    }
                                }
                            }
                        }
                    }

                    _context.CommentairesTicket.RemoveRange(commentairesDirects);
                }

                // 3.3 Supprimer les notifications
                var notifications = await _context.Notifications
                    .Where(n => n.DestinataireId == user.Id)
                    .ToListAsync();
                if (notifications.Any())
                {
                    _context.Notifications.RemoveRange(notifications);
                }

                // 3.4 Supprimer les historiques
                var historiques = await _context.HistoriquesTicket
                    .Where(h => h.ModifieParId == user.Id)
                    .ToListAsync();
                if (historiques.Any())
                {
                    _context.HistoriquesTicket.RemoveRange(historiques);
                }

                // 3.5 Supprimer les incidents créés par l'utilisateur (LieParId)
                var incidentsUser = await _context.IncidentTickets
                    .Where(i => i.LieParId == user.Id)
                    .ToListAsync();
                if (incidentsUser.Any())
                {
                    _context.IncidentTickets.RemoveRange(incidentsUser);
                }

                // 3.6 Sauvegarder toutes les modifications
                await _context.SaveChangesAsync();

                // 3.7 Supprimer l'utilisateur
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return result;
                }

                await transaction.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la suppression en cascade de l'utilisateur {UserId}", user.Id);
                throw;
            }
        }
    } 
}
