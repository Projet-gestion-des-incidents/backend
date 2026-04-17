using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class TPEDto
    {
        public Guid Id { get; set; }
        public string NumSerie { get; set; }
        public string NumSerieComplet { get; set; }
        public ModeleTPE Modele { get; set; }
        
        public Guid? CommercantId { get; set; }
        public string CommercantNom { get; set; }

        // CHAMPS D'AUDIT
        public DateTime CreatedAt { get; set; }
        public string CreatedByNom { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedByNom { get; set; }

    }

    public class CreateTPEDto
    {

        [Required]
        public ModeleTPE Modele { get; set; }

        [Required]
        public Guid CommercantId { get; set; }
    }

    public class UpdateTPEDto
    {
     
        public ModeleTPE Modele { get; set; }
        public Guid? CommercantId { get; set; }

    }
}
