using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class PieceJointe
    {
        public Guid Id { get; set; }
        public string NomFichier { get; set; }
      
        public DateTime DateAjout { get; set; }

        // Foreign Keys
        public Guid? CommentaireId { get; set; }
        
        public Guid UploadedById { get; set; }

        // Navigation Properties
        public virtual CommentaireTicket Commentaire { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; }

        // Foreign Keys - Ajouter IncidentId
        public Guid? IncidentId { get; set; } 
        
        // Navigation Properties - Ajouter Incident
        public virtual Incident Incident { get; set; } 

    }
}
