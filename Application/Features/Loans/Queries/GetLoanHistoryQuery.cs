using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Queries;

public record GetLoanHistoryQuery(int MemberId, int? RecentLoansCount = 5) : IRequest<Result<LoanHistoryDto>>;

public class GetLoanHistoryQueryHandler : IRequestHandler<GetLoanHistoryQuery, Result<LoanHistoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLoanHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<LoanHistoryDto>> Handle(GetLoanHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var fineRepository = _unitOfWork.Repository<Fine>();
            
            // Get member with user details
            var member = await memberRepository.GetAsync(
                m => m.Id == request.MemberId,
                m => m.User
            );
            
            if (member == null)
                return Result.Failure<LoanHistoryDto>($"Member with ID {request.MemberId} not found.");
            
            // Create loan history DTO
            var loanHistory = new LoanHistoryDto
            {
                MemberId = request.MemberId,
                MemberName = member.User.FullName,
                OutstandingFines = member.OutstandingFines
            };
            
            // Get total loans count
            loanHistory.TotalLoans = await loanRepository.CountAsync(l => l.MemberId == request.MemberId);
            
            // Get active loans count
            loanHistory.ActiveLoans = await loanRepository.CountAsync(l => 
                l.MemberId == request.MemberId && 
                l.Status == LoanStatus.Active
            );
            
            // Get overdue loans count
            loanHistory.OverdueLoans = await loanRepository.CountAsync(l => 
                l.MemberId == request.MemberId && 
                l.Status == LoanStatus.Overdue
            );
            
            // Get completed loans count
            loanHistory.CompletedLoans = await loanRepository.CountAsync(l => 
                l.MemberId == request.MemberId && 
                l.Status == LoanStatus.Returned
            );
            
            // Get total fines paid
            var paidFines = await fineRepository.ListAsync(
                f => f.MemberId == request.MemberId && f.Status == FineStatus.Paid
            );
            
            loanHistory.TotalFinesPaid = paidFines.Sum(f => f.Amount);
            
            // Get recent loans
            int recentCount = request.RecentLoansCount ?? 5;
            var recentLoans = await loanRepository.ListAsync(
                l => l.MemberId == request.MemberId,
                q => q.OrderByDescending(l => l.LoanDate),
                true,
                l => l.BookCopy,
                l => l.BookCopy.Book
            );
            
            // Only take the requested number of recent loans
            var recentLoansLimited = recentLoans.Take(recentCount).ToList();
            
            // Map loans to DTOs
            loanHistory.RecentLoans = _mapper.Map<List<LoanDto>>(recentLoansLimited);
            
            return Result.Success(loanHistory);
        }
        catch (Exception ex)
        {
            return Result.Failure<LoanHistoryDto>($"Failed to get loan history: {ex.Message}");
        }
    }
}