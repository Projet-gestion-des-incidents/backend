using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class CreateCommentaireDTO
    {
        [StringLength(2000, ErrorMessage = "Le message ne peut pas dépasser 2000 caractères")]
        public string? Message { get; set; }

        public bool EstInterne { get; set; } = false;

    }
}