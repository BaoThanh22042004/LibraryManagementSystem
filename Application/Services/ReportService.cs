using Application.DTOs;
using Application.Features.Reports.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Enums;
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
    
    /// <summary>
    /// Gets a comprehensive report of all fines in the system
    /// </summary>
    /// <param name="includeStatus">Optional filter to include specific fine statuses. If null, all statuses are included.</param>
    /// <returns>A report containing all fines with member and loan details</returns>
    public async Task<FineReportDto> GetFinesReportAsync(FineStatus[]? includeStatus = null)
    {
        var result = await _mediator.Send(new GetFinesReportQuery(includeStatus));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    /// <summary>
    /// Gets a summary of outstanding fines for a specific member
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>A summary of the member's outstanding fines</returns>
    public async Task<OutstandingFineDto> GetOutstandingFinesAsync(int memberId)
    {
        var result = await _mediator.Send(new GetOutstandingFinesQuery(memberId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    /// <summary>
    /// Checks if a member has any outstanding fines
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>True if the member has outstanding fines, false otherwise</returns>
    public async Task<bool> HasOutstandingFinesAsync(int memberId)
    {
        var result = await _mediator.Send(new HasOutstandingFinesQuery(memberId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    /// <summary>
    /// Gets the total amount of outstanding fines for a member
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>Total amount of outstanding fines</returns>
    public async Task<decimal> GetTotalOutstandingFineAmountAsync(int memberId)
    {
        var result = await _mediator.Send(new GetTotalOutstandingFineAmountQuery(memberId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    /// <summary>
    /// Gets the current dashboard statistics
    /// </summary>
    /// <returns>A summary of the current dashboard statistics</returns>
    public async Task<DashboardDto> GetDashboardStatisticsAsync()
    {
        var result = await _mediator.Send(new GetDashboardStatisticsQuery());
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    /// <summary>
    /// Gets the dashboard statistics for a specific date range
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>A summary of the dashboard statistics for the specified date range</returns>
    public async Task<DashboardDto> GetDashboardStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _mediator.Send(new GetDashboardStatisticsQuery(startDate, endDate));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
}