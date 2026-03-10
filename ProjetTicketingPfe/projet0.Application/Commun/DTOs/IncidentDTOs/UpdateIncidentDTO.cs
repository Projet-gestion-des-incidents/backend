using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class UpdateIncidentDTO
    {        
        public string? DescriptionIncident { get; set; }
        public string? Emplacement { get; set; }
        public TypeProbleme? TypeProbleme { get; set; }
        public SeveriteIncident? SeveriteIncident { get; set; }

    }
}
