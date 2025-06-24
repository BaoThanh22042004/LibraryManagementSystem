using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Members.Commands;

public record UpdateMembershipStatusCommand(int MemberId, MembershipStatus Status) : IRequest<Result>;

public class UpdateMembershipStatusCommandHandler : IRequestHandler<UpdateMembershipStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMembershipStatusCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMembershipStatusCommand request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        
        // Get current member
        var member = await memberRepository.GetAsync(m => m.Id == request.MemberId);
        
        if (member == null)
        {
            return Result.Failure($"Member with ID {request.MemberId} not found.");
        }
        
        // Update status
        member.MembershipStatus = request.Status;
        
        memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}