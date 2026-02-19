using System;
using System.Collections.Generic;
using System.Text;

// Fichier: projet0.Application/Mappings/TicketMappingProfile.cs
using AutoMapper;
using projet0.Application.Commun.DTOs.Ticket;
using projet0.Domain.Entities;

namespace projet0.Application.Mappings
{
    public class TicketMappingProfile : Profile
    {
        public TicketMappingProfile()
        {
            // Ticket -> TicketDTO
            CreateMap<Ticket, TicketDTO>()
                .ForMember(dest => dest.StatutTicketLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.PrioriteTicketLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.CreateurNom, opt => opt.Ignore())
                .ForMember(dest => dest.AssigneeNom, opt => opt.Ignore())
                .ForMember(dest => dest.NombreCommentaires, opt => opt.Ignore())
                .ForMember(dest => dest.NombrePiecesJointes, opt => opt.Ignore());

            // CreateTicketDTO -> Ticket (pour la création)
            CreateMap<CreateTicketDTO, Ticket>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReferenceTicket, opt => opt.Ignore())
                .ForMember(dest => dest.StatutTicket, opt => opt.Ignore())
                .ForMember(dest => dest.DateCreation, opt => opt.Ignore())
                .ForMember(dest => dest.DateCloture, opt => opt.Ignore())
                .ForMember(dest => dest.CreateurId, opt => opt.Ignore())
                .ForMember(dest => dest.Createur, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentTickets, opt => opt.Ignore())
                .ForMember(dest => dest.Historiques, opt => opt.Ignore())
                .ForMember(dest => dest.Commentaires, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        }
    }
}
