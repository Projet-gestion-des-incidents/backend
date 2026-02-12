using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public TypeNotification TypeNotification { get; set; }
        public string Titre { get; set; }
        public string Message { get; set; }
        public DateTime DateEnvoi { get; set; }
        public bool EstLu { get; set; }
        public DateTime? DateLecture { get; set; }

        // Foreign Keys
        public Guid DestinataireId { get; set; }
        public Guid? TicketId { get; set; }
        public Guid? IncidentId { get; set; }
        public Guid? CommentaireId { get; set; }

        // Navigation Properties
        public virtual ApplicationUser Destinataire { get; set; }
        public virtual Ticket Ticket { get; set; }
        public virtual Incident Incident { get; set; }
        public virtual CommentaireTicket Commentaire { get; set; }
    }
}
