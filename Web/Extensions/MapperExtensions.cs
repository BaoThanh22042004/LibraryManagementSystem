using Application.DTOs;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace Web.Extensions;

public static class MapperExtensions
{
    private static readonly IMapper _mapper;

    static MapperExtensions()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new Application.Mapping.AuditProfile()), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    public static AuditLogExportDto SafeMapToExportDto(this AuditLogResponse response)
    {
        // Map only the allowed fields
        return _mapper.Map<AuditLogExportDto>(response);
    }
}