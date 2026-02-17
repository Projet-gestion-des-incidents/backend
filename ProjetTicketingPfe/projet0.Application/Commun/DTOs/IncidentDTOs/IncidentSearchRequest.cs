using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentSearchRequest
    {
        public string? SearchTerm { get; set; } // <-- le "?" permet de rendre le champ optionnel
        public SeveriteIncident? SeveriteIncident { get; set; }
        public StatutIncident? StatutIncident { get; set; }

        public int? YearDetection { get; set; }
        public int? YearResolution { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "DateDetection";
        public bool SortDescending { get; set; } = true;
    }
}
