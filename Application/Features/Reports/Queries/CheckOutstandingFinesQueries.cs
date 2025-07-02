using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reports.Queries;

/// <summary>
/// Query to check if a member has outstanding fines
/// </summary>
public record HasOutstandingFinesQuery(int MemberId) : IRequest<Result<bool>>;

/// <summary>
/// Handler for checking if a member has outstanding fines
/// </summary>
public class HasOutstandingFinesQueryHandler : IRequestHandler<HasOutstandingFinesQuery, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public HasOutstandingFinesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(HasOutstandingFinesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            
            // Verify member exists
            var member = await memberRepository.GetAsync(m => m.Id == request.MemberId);
            
            if (member == null)
                return Result.Failure<bool>($"Member with ID {request.MemberId} not found.");
            
            // Check if the member has outstanding fines
            bool hasOutstandingFines = member.OutstandingFines > 0;
            
            return Result.Success(hasOutstandingFines);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Error checking outstanding fines: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to get the total amount of outstanding fines for a member
/// </summary>
public record GetTotalOutstandingFineAmountQuery(int MemberId) : IRequest<Result<decimal>>;

/// <summary>
/// Handler for getting the total amount of outstanding fines for a member
/// </summary>
public class GetTotalOutstandingFineAmountQueryHandler : IRequestHandler<GetTotalOutstandingFineAmountQuery, Result<decimal>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTotalOutstandingFineAmountQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<decimal>> Handle(GetTotalOutstandingFineAmountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            
            // Verify member exists
            var member = await memberRepository.GetAsync(m => m.Id == request.MemberId);
            
            if (member == null)
                return Result.Failure<decimal>($"Member with ID {request.MemberId} not found.");
            
            // Return the outstanding fines amount
            return Result.Success(member.OutstandingFines);
        }
        catch (Exception ex)
        {
            return Result.Failure<decimal>($"Error getting outstanding fine amount: {ex.Message}");
        }
    }
}