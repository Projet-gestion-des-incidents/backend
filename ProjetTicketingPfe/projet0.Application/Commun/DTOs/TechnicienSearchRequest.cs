using projet0.Domain.Entities;

namespace projet0.Application.Commun.DTOs
{
    public class TechnicienSearchRequest
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Tri
        public string? SortBy { get; set; } = "Nom";
        public bool SortDescending { get; set; } = false;

        // Recherche et filtres
        public string? SearchTerm { get; set; }
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Email { get; set; }
        public UserStatut? Statut { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? BirthYear { get; set; }
    }
}
