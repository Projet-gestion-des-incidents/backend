using Microsoft.AspNetCore.Http;
using projet0.Domain.Enums;
using System.ComponentModel.DataAnnotations;

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

        // public Guid? AssigneeId { get; set; }

        // ✅ NOUVEAU: Commentaire initial
        [StringLength(2000, ErrorMessage = "Le commentaire ne peut pas dépasser 2000 caractères")]
        public string? CommentaireInitial { get; set; }

        // ✅ NOUVEAU: Indique si le commentaire est interne
        public bool CommentaireInterne { get; set; } = false;

        // ✅ NOUVEAU: Fichiers joints
        //public List<IFormFile>? Fichiers { get; set; }

        public List<CreatePieceJointeDTO>? Fichiers { get; set; }
    }
}