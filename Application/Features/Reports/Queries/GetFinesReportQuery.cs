using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to retrieve a comprehensive report of all fines in the system
/// </summary>
public record GetFinesReportQuery(FineStatus[]? IncludeStatus = null) : IRequest<Result<FineReportDto>>;

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
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        // Get all fines, filtered by status if specified
        var fines = await fineRepository.ListAsync(
            predicate: request.IncludeStatus != null && request.IncludeStatus.Length > 0 
                ? f => request.IncludeStatus.Contains(f.Status) 
                : null,
            includes: [
                f => f.Member, 
                f => f.Member.User, 
                f => f.Loan!,
                f => f.Loan!.BookCopy,
                f => f.Loan!.BookCopy.Book
            ]
        );
        
        if (fines == null)
        {
            return Result.Failure<FineReportDto>("Failed to retrieve fines");
        }
        
        var report = new FineReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            Fines = new List<FineReportItemDto>()
        };
        
        foreach (var fine in fines)
        {
            var fineItem = new FineReportItemDto
            {
                FineId = fine.Id,
                MemberId = fine.MemberId,
                MemberName = fine.Member.User.FullName,
                MemberEmail = fine.Member.User.Email,
                LoanId = fine.LoanId,
                BookTitle = fine.Loan?.BookCopy?.Book?.Title,
                Type = fine.Type,
                Amount = fine.Amount,
                FineDate = fine.FineDate,
                Status = fine.Status,
                Description = fine.Description
            };
            
            report.Fines.Add(fineItem);
        }
        
        // Sort by fine date (most recent first)
        report.Fines = report.Fines.OrderByDescending(f => f.FineDate).ToList();
        
        return Result.Success(report);
    }
}