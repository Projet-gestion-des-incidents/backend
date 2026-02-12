using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class IncidentDetailDTO: IncidentDTO
    {
        public List<IncidentTicketDTO> Tickets { get; set; }
    public List<EntiteImpacteeDTO> EntitesImpactees { get; set; }
}
}
