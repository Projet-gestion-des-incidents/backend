using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentDTO
    {
        public Guid Id { get; set; }
        public string CodeIncident { get; set; }
        public string TitreIncident { get; set; }
        public string DescriptionIncident { get; set; }
        public SeveriteIncident SeveriteIncident { get; set; }
        public string SeveriteIncidentLibelle { get; set; }
        public StatutIncident StatutIncident { get; set; }
        public string StatutIncidentLibelle { get; set; }
        public DateTime DateDetection { get; set; }
        public DateTime? DateResolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedById { get; set; }
        public string CreatedByName { get; set; }
        public int NombreTickets { get; set; }
        public int NombreEntitesImpactees { get; set; }
    }
}
