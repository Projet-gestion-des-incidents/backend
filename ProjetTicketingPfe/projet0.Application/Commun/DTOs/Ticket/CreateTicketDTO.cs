// Fichier: projet0.Application/Commun/DTOs/Ticket/CreateTicketDTO.cs
using System.ComponentModel.DataAnnotations;
using projet0.Domain.Enums;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class CreateTicketDTO
    {
        [Required(ErrorMessage = "Le titre est requis")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Le titre doit contenir entre 3 et 200 caractères")]
        public string TitreTicket { get; set; }

        [StringLength(2000, ErrorMessage = "La description ne peut pas dépasser 2000 caractères")]
        public string DescriptionTicket { get; set; }

        [Required(ErrorMessage = "La priorité est requise")]
        public PrioriteTicket PrioriteTicket { get; set; }

        [Required(ErrorMessage = "Le statut est requis")]
        public StatutTicket StatutTicket { get; set; }

        // 👇 SUPPRIMEZ assigneeId pour l'instant - il sera null
        // public Guid? AssigneeId { get; set; }  ← À COMMENTER/SUPPRIMER
    }
}