using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationReadDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt));
        CreateMap<Notification, NotificationListDto>()
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));
        CreateMap<NotificationCreateDto, Notification>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => Domain.Enums.NotificationStatus.Pending))
            .ForMember(dest => dest.SentAt, opt => opt.Ignore());
        CreateMap<NotificationBatchCreateDto, Notification>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => Domain.Enums.NotificationStatus.Pending))
            .ForMember(dest => dest.SentAt, opt => opt.Ignore());
    }
}
