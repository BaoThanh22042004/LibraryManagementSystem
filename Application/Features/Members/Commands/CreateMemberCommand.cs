using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Members.Commands;

public record CreateMemberCommand(CreateMemberDto MemberDto) : IRequest<Result<int>>;

public class CreateMemberCommandHandler : IRequestHandler<CreateMemberCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateMemberCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        var userRepository = _unitOfWork.Repository<User>();
        
        // Check if membership number already exists
        var membershipNumberExists = await memberRepository.ExistsAsync(
            m => m.MembershipNumber == request.MemberDto.MembershipNumber
        );
        
        if (membershipNumberExists)
        {
            return Result.Failure<int>($"Member with membership number '{request.MemberDto.MembershipNumber}' already exists.");
        }
        
        // Check if user exists
        var user = await userRepository.GetAsync(u => u.Id == request.MemberDto.UserId);
        
        if (user == null)
        {
            return Result.Failure<int>($"User with ID {request.MemberDto.UserId} not found.");
        }
        
        // Check if user is already a member
        var userIsMember = await memberRepository.ExistsAsync(m => m.UserId == request.MemberDto.UserId);
        
        if (userIsMember)
        {
            return Result.Failure<int>($"User with ID {request.MemberDto.UserId} is already a member.");
        }
        
        // Create new member
        var member = _mapper.Map<Member>(request.MemberDto);
        member.MembershipStartDate = DateTime.UtcNow;
        member.MembershipStatus = MembershipStatus.Active;
        member.OutstandingFines = 0;
        
        await memberRepository.AddAsync(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(member.Id);
    }
}