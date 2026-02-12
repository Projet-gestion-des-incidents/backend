using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.Incident
{
    public class EntiteImpacteeDTO
    {
        public Guid Id { get; set; }
        public TypeEntiteImpactee TypeEntiteImpactee { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
    }
}
