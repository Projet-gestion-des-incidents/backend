using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class IncidentTPEDTO
    {
        public Guid TPEId { get; set; }
        public string NumSerie { get; set; }
        public string NumSerieComplet { get; set; }
        public ModeleTPE Modele { get; set; }
        public string ModeleNom => Modele.ToString();
        public DateTime DateAssociation { get; set; }
    }
}
