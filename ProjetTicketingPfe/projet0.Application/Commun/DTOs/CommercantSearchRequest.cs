using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class CommercantSearchRequest
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Tri
        public string? SortBy { get; set; } = "Nom";
        public bool SortDescending { get; set; } = false;

        // Recherche et filtres
        public string? SearchTerm { get; set; }
        public string? NomMagasin { get; set; }  // Nom du magasin (UserName)
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Adresse { get; set; }
        public UserStatut? Statut { get; set; }
    }
}
