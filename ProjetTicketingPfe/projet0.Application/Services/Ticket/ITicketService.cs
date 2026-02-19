using System;
using System.Collections.Generic;
using System.Text;
// Fichier: projet0.Application/Services/Ticket/ITicketService.cs
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Application.Commun.Ressources;

namespace projet0.Application.Services.Ticket
{
    public interface ITicketService
    {
        // Récupérer tous les tickets
        Task<ApiResponse<List<TicketDTO>>> GetAllTicketsAsync();

        // Récupérer un ticket par son ID
        Task<ApiResponse<TicketDTO>> GetTicketByIdAsync(Guid id);

        // Créer un nouveau ticket
        Task<ApiResponse<TicketDTO>> CreateTicketAsync(CreateTicketDTO dto, Guid createurId);
    }
}