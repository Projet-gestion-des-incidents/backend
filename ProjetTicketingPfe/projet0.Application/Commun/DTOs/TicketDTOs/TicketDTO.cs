using System;
using System.Collections.Generic;
using System.Text;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class TicketDTO
    {
        public Guid Id { get; set; }
        public string ReferenceTicket { get; set; }
        public string TitreTicket { get; set; }
        public string DescriptionTicket { get; set; }
        public StatutTicket StatutTicket { get; set; }
        public string StatutTicketLibelle { get; set; }
        public PrioriteTicket PrioriteTicket { get; set; }
        public string PrioriteTicketLibelle { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateCloture { get; set; }
        public Guid CreateurId { get; set; }
        public string CreateurNom { get; set; }
        public Guid? AssigneeId { get; set; }
        public string AssigneeNom { get; set; }
        public int NombreCommentaires { get; set; }
        public int NombrePiecesJointes { get; set; }
    }
}