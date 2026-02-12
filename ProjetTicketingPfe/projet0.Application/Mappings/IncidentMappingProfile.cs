using projet0.Application.Commun.DTOs.Incident;
using projet0.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace projet0.Application.Mappings
{
    public class IncidentMappingProfile : Profile
    {
        public IncidentMappingProfile()
        {
            // Incident mappings
            CreateMap<Incident, IncidentDTO>()
                .ForMember(dest => dest.SeveriteIncidentLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.StatutIncidentLibelle, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
                .ForMember(dest => dest.NombreTickets, opt => opt.Ignore())
                .ForMember(dest => dest.NombreEntitesImpactees, opt => opt.Ignore());

            CreateMap<Incident, IncidentDetailDTO>()
                .IncludeBase<Incident, IncidentDTO>()
                .ForMember(dest => dest.Tickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore());

            CreateMap<CreateIncidentDTO, Incident>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CodeIncident, opt => opt.Ignore())
                .ForMember(dest => dest.StatutIncident, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentTickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());

            CreateMap<UpdateIncidentDTO, Incident>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CodeIncident, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.IncidentTickets, opt => opt.Ignore())
                .ForMember(dest => dest.EntitesImpactees, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore());

            // EntiteImpactee mappings
            CreateMap<EntiteImpactee, EntiteImpacteeDTO>();
        }
    }
}