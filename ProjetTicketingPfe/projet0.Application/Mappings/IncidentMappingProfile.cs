using AutoMapper;
using projet0.Application.Commun.DTOs.Incident;
using projet0.Domain.Entities;

namespace projet0.Application.Mappings
{
    public class IncidentMappingProfile : Profile
    {
        public IncidentMappingProfile()
        {
            CreateMap<Incident, IncidentDTO>()
                .ForMember(dest => dest.SeveriteIncidentLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.StatutIncidentLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore());

            CreateMap<Incident, IncidentDetailDTO>()
                .IncludeBase<Incident, IncidentDTO>()
                .ForMember(dest => dest.Tickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore())
                .ForMember(dest => dest.TPEs, opt => opt.Ignore())        
                .ForMember(dest => dest.PiecesJointes, opt => opt.Ignore());

            CreateMap<CreateIncidentDTO, Incident>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CodeIncident, opt => opt.Ignore())
                .ForMember(dest => dest.StatutIncident, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentTickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());

            CreateMap<UpdateIncidentDTO, Incident>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CodeIncident, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentTickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());

            CreateMap<EntiteImpactee, EntiteImpacteeDTO>();
        }
    }
}