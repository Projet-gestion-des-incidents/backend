using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class TPEDto
    {
        public Guid Id { get; set; }
        public string NumSerie { get; set; }
        public string Modele { get; set; }
        public Guid CommercantId { get; set; }
        public string CommercantNom { get; set; }
        
    }

    public class CreateTPEDto
    {
        public string NumSerie { get; set; }
        public string Modele { get; set; }
        public Guid CommercantId { get; set; }
    }

    public class UpdateTPEDto
    {
        public Guid Id { get; set; }
        public string NumSerie { get; set; }
        public string Modele { get; set; }
    }
}
