using AutoMapper;
using projet0.Application.Commun.DTOs;
using projet0.Domain.Entities;

namespace projet0.Application.Mappings
{
    public class NotificationMappingProfile : Profile
    {
        public NotificationMappingProfile()
        {
            CreateMap<Notification, NotificationDto>()
                .ForMember(dest => dest.DestinataireNom,
                    opt => opt.MapFrom(src => src.Destinataire != null ?
                        $"{src.Destinataire.Prenom} {src.Destinataire.Nom}" : null))
                .ForMember(dest => dest.TicketTitre,
                    opt => opt.MapFrom(src => src.Ticket != null ? src.Ticket.TitreTicket : null))
                .ForMember(dest => dest.IncidentTitre,
                    opt => opt.MapFrom(src => src.Incident != null ? src.Incident.CodeIncident : null));

            CreateMap<CreateNotificationDto, Notification>();
        }
    }
}
