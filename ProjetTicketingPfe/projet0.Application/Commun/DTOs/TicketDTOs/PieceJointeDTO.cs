using System;
using System.Collections.Generic;
using System.Text;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class PieceJointeDTO
    {
        public Guid Id { get; set; }
        public string NomFichier { get; set; }
        public long Taille { get; set; }
        public string ContentType { get; set; }
        public DateTime DateAjout { get; set; }
        public string Url { get; set; } // Pour télécharger le fichier
    }
}