using projet0.Application.Common.Models.Pagination;
using projet0.Domain.Entities;
using System;

namespace projet0.Application.Common.Models.Pagination
{
    public class UserSearchRequest : PagedRequest
    {
        // Option 1: Recherche globale (cherche dans tous les champs)
        public string? SearchTerm { get; set; }

        // Option 2: Filtres précis par champ
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public UserStatut? Statut { get; set; }

        public DateTime? BirthDate { get; set; }

        // Tri spécifique
        public string? SortBy { get; set; } = "Nom"; // Par défaut tri par nom
        public bool SortDescending { get; set; } = false;

        public UserSearchRequest()
        {
            PageSize = 20;
        }
    }
}