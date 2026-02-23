// Fichier: projet0.Application/Commun/DTOs/Ticket/UpdateCommentaireDTO.cs
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class UpdateCommentaireDTO
    {
        [StringLength(2000, ErrorMessage = "Le message ne peut pas dépasser 2000 caractères")]
        public string? Message { get; set; }

       
        public bool EffacerMessage { get; set; } = false;

        public bool EstInterne { get; set; }

        // Liste des IDs des pièces jointes à supprimer
        public List<Guid>? PiecesJointesASupprimer { get; set; }

        // Nouveaux fichiers à ajouter
        public List<IFormFile>? NouveauxFichiers { get; set; }
    }
}