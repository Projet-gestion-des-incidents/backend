using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class UpdateIncidentDTO
    {
        public string TitreIncident { get; set; }
        public string DescriptionIncident { get; set; }
        public SeveriteIncident SeveriteIncident { get; set; }
        public StatutIncident StatutIncident { get; set; }
        public List<UpdateEntiteImpacteeDTO> EntitesImpactees { get; set; }

        // public DateTime? DateResolution { get; set; }
    }
}
