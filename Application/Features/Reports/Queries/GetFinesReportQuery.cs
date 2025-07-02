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
/// Query to retrieve fines data for report generation
/// </summary>
public record GetFinesReportQuery(FineStatus[]? IncludeStatus = null) : IRequest<Result<FineReportDto>>;

/// <summary>
/// Handler for retrieving fines report data
/// </summary>
public class GetFinesReportQueryHandler : IRequestHandler<GetFinesReportQuery, Result<FineReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetFinesReportQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<FineReportDto>> Handle(GetFinesReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            
            // Define predicate based on requested status filter
            Expression<Func<Fine, bool>>? predicate = null;
            
            if (request.IncludeStatus != null && request.IncludeStatus.Length > 0)
            {
                predicate = f => request.IncludeStatus.Contains(f.Status);
            }
            
            // Retrieve fines with related data
            var fines = await fineRepository.ListAsync(
                predicate: predicate,
                orderBy: q => q.OrderByDescending(f => f.FineDate),
                includes: new Expression<Func<Fine, object>>[] 
                { 
                    f => f.Member.User,
                    f => f.Loan.BookCopy.Book
                }
            );
            
            if (fines == null || !fines.Any())
            {
                return Result.Success(new FineReportDto
                {
                    Fines = [],
                    GeneratedAt = DateTime.UtcNow
                });
            }
            
            // Transform fines into report items
            var fineItems = new List<FineReportItemDto>();
            
            foreach (var fine in fines)
            {
                var reportItem = new FineReportItemDto
                {
                    FineId = fine.Id,
                    MemberId = fine.MemberId,
                    MemberName = fine.Member?.User?.FullName ?? "Unknown",
                    MemberEmail = fine.Member?.User?.Email ?? "Unknown",
                    Amount = fine.Amount,
                    FineDate = fine.FineDate,
                    Status = fine.Status,
                    Type = fine.Type,
                    Description = fine.Description,
                    LoanId = fine.LoanId
                };
                
                // Include book info if this fine is related to a loan
                if (fine.Loan != null)
                {
                    reportItem.BookTitle = fine.Loan.BookCopy?.Book?.Title;
                }
                
                fineItems.Add(reportItem);
            }
            
            // Create the report
            var report = new FineReportDto
            {
                Fines = fineItems,
                GeneratedAt = DateTime.UtcNow
            };
            
            return Result.Success(report);
        }
        catch (Exception ex)
        {
            return Result.Failure<FineReportDto>($"Error retrieving fines report: {ex.Message}");
        }
    }
}