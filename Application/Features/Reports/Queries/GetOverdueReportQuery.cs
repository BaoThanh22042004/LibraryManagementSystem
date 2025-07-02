using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to retrieve all overdue loans for report generation
/// </summary>
public record GetOverdueReportQuery : IRequest<Result<OverdueReportDto>>;

/// <summary>
/// Handler for retrieving overdue loan report data
/// </summary>
public class GetOverdueReportQueryHandler : IRequestHandler<GetOverdueReportQuery, Result<OverdueReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetOverdueReportQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<OverdueReportDto>> Handle(GetOverdueReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var loanRepository = _unitOfWork.Repository<Loan>();
            var now = DateTime.UtcNow;
            
            // Get all loans that are currently active/overdue and past their due date
            var overdueLoans = await loanRepository.ListAsync(
                predicate: l => (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue) && l.DueDate < now,
                orderBy: q => q.OrderBy(l => l.DueDate), // Oldest overdue first
                includes: [
                    l => l.Member, 
                    l => l.Member.User,
                    l => l.BookCopy,
                    l => l.BookCopy.Book
                ]
            );
            
            if (overdueLoans == null || !overdueLoans.Any())
            {
                return Result.Success(new OverdueReportDto
                {
                    OverdueLoans = [],
                    GeneratedAt = now
                });
            }
            
            // Transform the data into the report format using AutoMapper
            var overdueItems = _mapper.Map<List<OverdueLoanDto>>(overdueLoans);
            
            var report = new OverdueReportDto
            {
                OverdueLoans = overdueItems,
                GeneratedAt = now
            };
            
            return Result.Success(report);
        }
        catch (Exception ex)
        {
            return Result.Failure<OverdueReportDto>($"Error retrieving overdue report: {ex.Message}");
        }
    }
}