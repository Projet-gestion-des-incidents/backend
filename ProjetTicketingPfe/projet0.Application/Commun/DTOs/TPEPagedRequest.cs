using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class TPEPagedRequest
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Tri
        public string? SortBy { get; set; } = "NumSerieComplet";
        public bool SortDescending { get; set; } = false;

        // Filtres
        public ModeleTPE? Modele { get; set; }
        public string? SearchTerm { get; set; }  // Recherche par numéro de série ou nom du commerçant
        public Guid? CommercantId { get; set; }  // Filtrer par commerçant spécifique

        public bool? NonAssigne { get; set; }

        // ✅ NOUVEAUX FILTRES (simples)
        public DateTime? CreatedAt { get; set; }     // Date de création exacte
        public DateTime? UpdatedAt { get; set; }     // Date de modification exacte
        public Guid? CreatedById { get; set; }       // Filtre par créateur
        public Guid? UpdatedById { get; set; }       // Filtre par modificateur
    }
}
