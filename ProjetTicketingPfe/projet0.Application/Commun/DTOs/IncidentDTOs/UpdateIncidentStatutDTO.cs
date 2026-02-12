using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class UpdateIncidentStatutDTO
    {
        public StatutIncident StatutIncident { get; set; }
        public DateTime? DateResolution { get; set; }
    }
}
