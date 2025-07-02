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
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var fineRepository = _unitOfWork.Repository<Fine>();
            
            // Verify member exists
            var member = await memberRepository.GetAsync(
                m => m.Id == request.MemberId,
                m => m.User
            );
            
            if (member == null)
                return Result.Failure<OutstandingFineDto>($"Member with ID {request.MemberId} not found.");
            
            // Get pending fines for the member
            var pendingFines = await fineRepository.ListAsync(
                predicate: f => f.MemberId == request.MemberId && f.Status == FineStatus.Pending,
                orderBy: q => q.OrderByDescending(f => f.FineDate),
                includes: new Expression<Func<Fine, object>>[] 
                { 
                    f => f.Loan.BookCopy.Book
                }
            );
            
            // Transform fines to report items
            var fineItems = new List<FineReportItemDto>();
            
            foreach (var fine in pendingFines)
            {
                var reportItem = new FineReportItemDto
                {
                    FineId = fine.Id,
                    MemberId = fine.MemberId,
                    MemberName = member.User?.FullName ?? "Unknown",
                    MemberEmail = member.User?.Email ?? "Unknown",
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
            
            // Create the outstanding fines result
            var outstandingFines = new OutstandingFineDto
            {
                MemberId = member.Id,
                MemberName = member.User?.FullName ?? "Unknown",
                OutstandingAmount = member.OutstandingFines,
                PendingFinesCount = pendingFines.Count,
                PendingFines = fineItems,
                CalculatedAt = DateTime.UtcNow
            };
            
            return Result.Success(outstandingFines);
        }
        catch (Exception ex)
        {
            return Result.Failure<OutstandingFineDto>($"Error retrieving outstanding fines: {ex.Message}");
        }
    }
}