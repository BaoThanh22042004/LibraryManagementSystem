using Application.DTOs;
using Application.Features.Reports.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using MediatR;

namespace Application.Services;

/// <summary>
/// Service for generating various reports in the library system
/// </summary>
public class ReportService : IReportService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReportService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a comprehensive report of all loans that are past their due date
    /// </summary>
    /// <returns>A report containing all overdue loans with member and book details</returns>
    public async Task<OverdueReportDto> GetOverdueReportAsync()
    {
        var result = await _mediator.Send(new GetOverdueReportQuery());
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
}