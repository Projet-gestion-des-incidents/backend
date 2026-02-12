using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class CreateEntiteImpacteeDTO
    {
        public TypeEntiteImpactee TypeEntiteImpactee { get; set; }
        public string Nom { get; set; }
    }
}
