using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Members.Commands;

public record UpdateMemberCommand(int Id, UpdateMemberDto MemberDto) : IRequest<Result>;

public class UpdateMemberCommandHandler : IRequestHandler<UpdateMemberCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        
        // Get current member
        var member = await memberRepository.GetAsync(m => m.Id == request.Id);
        
        if (member == null)
        {
            return Result.Failure($"Member with ID {request.Id} not found.");
        }
        
        // Update member properties
        member.MembershipStatus = request.MemberDto.MembershipStatus;
        
        if (request.MemberDto.MembershipStartDate.HasValue)
        {
            member.MembershipStartDate = request.MemberDto.MembershipStartDate.Value;
        }
        
        memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}