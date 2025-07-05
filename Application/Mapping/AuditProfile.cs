using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for audit-related mapping.
/// </summary>
public class AuditProfile : Profile
{
    public AuditProfile()
    {
        // CreateAuditLogRequest to AuditLog
        CreateMap<CreateAuditLogRequest, AuditLog>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
        
        // AuditLog to AuditLogResponse
        CreateMap<AuditLog, AuditLogResponse>()
            .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null));
    }
}
