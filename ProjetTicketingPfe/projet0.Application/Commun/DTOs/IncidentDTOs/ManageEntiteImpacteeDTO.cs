using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    // Pour AJOUTER une entité impactée à un incident
    public class AddEntiteImpacteeDTO
    {
        public Guid IncidentId { get; set; }
        public TypeEntiteImpactee TypeEntiteImpactee { get; set; }
    }

    // Pour SUPPRIMER une entité impactée
    public class RemoveEntiteImpacteeDTO
    {
        public Guid EntiteImpacteeId { get; set; }
        public Guid IncidentId { get; set; }

    }
    
}
