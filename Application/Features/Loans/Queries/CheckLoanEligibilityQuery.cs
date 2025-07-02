using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Queries;

public record CheckLoanEligibilityQuery(int MemberId) : IRequest<Result<LoanEligibilityDto>>;

public class CheckLoanEligibilityQueryHandler : IRequestHandler<CheckLoanEligibilityQuery, Result<LoanEligibilityDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private const int MaxAllowedLoans = 5;

    public CheckLoanEligibilityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoanEligibilityDto>> Handle(CheckLoanEligibilityQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            
            // Get member with user details
            var member = await memberRepository.GetAsync(
                m => m.Id == request.MemberId,
                m => m.User
            );
            
            if (member == null)
                return Result.Failure<LoanEligibilityDto>($"Member with ID {request.MemberId} not found.");
            
            var eligibilityDto = new LoanEligibilityDto
            {
                MemberId = request.MemberId,
                IsEligible = true, // Start with assumption of eligibility
                Reasons = new List<string>()
            };
            
            // Check membership status
            if (member.MembershipStatus != MembershipStatus.Active)
            {
                eligibilityDto.IsEligible = false;
                eligibilityDto.Reasons.Add($"Membership status is {member.MembershipStatus}, must be Active.");
            }
            
            // Check for unpaid fines
            if (member.OutstandingFines > 0)
            {
                eligibilityDto.IsEligible = false;
                eligibilityDto.Reasons.Add($"Member has outstanding fines of ${member.OutstandingFines:F2}.");
            }
            
            // Check number of active loans
            var activeLoans = await loanRepository.CountAsync(l => 
                l.MemberId == request.MemberId && 
                (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue)
            );
            
            eligibilityDto.AvailableLoanSlots = Math.Max(0, MaxAllowedLoans - activeLoans);
            
            if (activeLoans >= MaxAllowedLoans)
            {
                eligibilityDto.IsEligible = false;
                eligibilityDto.Reasons.Add($"Member already has {activeLoans} active loans (maximum is {MaxAllowedLoans}).");
            }
            
            // Check for overdue loans
            var overdueLoans = await loanRepository.CountAsync(l => 
                l.MemberId == request.MemberId && 
                l.Status == LoanStatus.Overdue
            );
            
            if (overdueLoans > 0)
            {
                eligibilityDto.Reasons.Add($"Warning: Member has {overdueLoans} overdue loan(s).");
            }
            
            return Result.Success(eligibilityDto);
        }
        catch (Exception ex)
        {
            return Result.Failure<LoanEligibilityDto>($"Failed to check loan eligibility: {ex.Message}");
        }
    }
}