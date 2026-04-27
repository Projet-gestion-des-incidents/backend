using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public TypeNotification TypeNotification { get; set; }
        public string TypeNotificationName => TypeNotification.ToString();
        public string Titre { get; set; }
        public string Message { get; set; }
        public DateTime DateEnvoi { get; set; }
        public bool EstLu { get; set; }
        public DateTime? DateLecture { get; set; }
        public Guid DestinataireId { get; set; }
        public string DestinataireNom { get; set; }
        public Guid? TicketId { get; set; }
        public string TicketTitre { get; set; }
        public Guid? IncidentId { get; set; }
        public string IncidentTitre { get; set; }
        public Guid? CommentaireId { get; set; }
    }

    public class CreateNotificationDto
    {
        public Guid DestinataireId { get; set; }
        public TypeNotification TypeNotification { get; set; }
        public string Titre { get; set; }
        public string Message { get; set; }
        public Guid? TicketId { get; set; }
        public Guid? IncidentId { get; set; }
        public Guid? CommentaireId { get; set; }
    }

    public class MarkNotificationsReadDto
    {
        public List<Guid> NotificationIds { get; set; }
    }
}
