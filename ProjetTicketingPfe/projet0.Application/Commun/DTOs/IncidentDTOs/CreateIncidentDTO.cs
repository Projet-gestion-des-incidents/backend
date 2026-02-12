using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class CreateIncidentDTO
    {
        public string TitreIncident { get; set; }
        public string DescriptionIncident { get; set; }
        public SeveriteIncident SeveriteIncident { get; set; }
        public DateTime DateDetection { get; set; }
        public List<Guid> EntitesImpacteesIds { get; set; }
    }
}
