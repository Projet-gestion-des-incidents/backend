using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class TPEDto
    {
        public Guid Id { get; set; }
        public string NumSerie { get; set; }
        public string NumSerieComplet { get; set; }
        public ModeleTPE Modele { get; set; }
        public string ModeleNom => Modele.ToString().Replace('_', ' '); // Pour affichage
        public Guid CommercantId { get; set; }
        public string CommercantNom { get; set; }
        
    }

    public class CreateTPEDto
    {
        public string NumSerie { get; set; }
        public ModeleTPE Modele { get; set; }
        public Guid CommercantId { get; set; }
    }

    public class UpdateTPEDto
    {
        public string NumSerie { get; set; }
        public ModeleTPE Modele { get; set; }
        public Guid CommercantId { get; set; }
    }
}
