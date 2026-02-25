using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class CreateCommentaireDTO
    {
        [StringLength(2000, ErrorMessage = "Le message ne peut pas dépasser 2000 caractères")]
        public string? Message { get; set; }
        public bool EstInterne { get; set; } = false;
        public List<IFormFile>? Fichiers { get; set; }

    }
}