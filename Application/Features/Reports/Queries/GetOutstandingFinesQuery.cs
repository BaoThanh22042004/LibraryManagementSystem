using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to retrieve outstanding fines for a specific member
/// </summary>
public record GetOutstandingFinesQuery(int MemberId) : IRequest<Result<OutstandingFineDto>>;

/// <summary>
/// Handler for retrieving a member's outstanding fines
/// </summary>
public class GetOutstandingFinesQueryHandler : IRequestHandler<GetOutstandingFinesQuery, Result<OutstandingFineDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetOutstandingFinesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<OutstandingFineDto>> Handle(GetOutstandingFinesQuery request, CancellationToken cancellationToken)
    {
        // Get member information first
        var memberRepository = _unitOfWork.Repository<Member>();
        var member = await memberRepository.GetAsync(
            predicate: m => m.Id == request.MemberId,
            includes: m => m.User
        );
        
        if (member == null)
        {
            return Result.Failure<OutstandingFineDto>($"Member with ID {request.MemberId} not found");
        }
        
        // Get outstanding fines for the member
        var fineRepository = _unitOfWork.Repository<Fine>();
        var outstandingFines = await fineRepository.ListAsync(
            predicate: f => f.MemberId == request.MemberId && f.Status == FineStatus.Pending,
            includes: [
                f => f.Loan,
                f => f.Loan.BookCopy,
                f => f.Loan.BookCopy.Book
            ]
        );
        
        var report = new OutstandingFineDto
        {
            MemberId = member.Id,
            MemberName = member.User.FullName,
            MembershipStatus = member.MembershipStatus,
            CalculatedAt = DateTime.UtcNow,
            PendingFines = new List<FineReportItemDto>()
        };
        
        foreach (var fine in outstandingFines)
        {
            var fineItem = new FineReportItemDto
            {
                FineId = fine.Id,
                MemberId = fine.MemberId,
                MemberName = member.User.FullName,
                MemberEmail = member.User.Email,
                LoanId = fine.LoanId,
                BookTitle = fine.Loan?.BookCopy?.Book?.Title,
                Type = fine.Type,
                Amount = fine.Amount,
                FineDate = fine.FineDate,
                Status = fine.Status,
                Description = fine.Description
            };
            
            report.PendingFines.Add(fineItem);
        }
        
        // Sort by fine date (most recent first)
        report.PendingFines = report.PendingFines.OrderByDescending(f => f.FineDate).ToList();
        
        return Result.Success(report);
    }
}