using Application.Common;
using Application.DTOs;
using Application.Features.Members.Commands;
using Application.Features.Members.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Services;

public class MemberService : IMemberService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MemberService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<MemberDto>> GetPaginatedMembersAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetMembersQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<MemberDetailsDto?> GetMemberByIdAsync(int id)
    {
        return await _mediator.Send(new GetMemberByIdQuery(id));
    }

    public async Task<MemberDto?> GetMemberByMembershipNumberAsync(string membershipNumber)
    {
        return await _mediator.Send(new GetMemberByMembershipNumberQuery(membershipNumber));
    }

    public async Task<MemberDto?> GetMemberByUserIdAsync(int userId)
    {
        return await _mediator.Send(new GetMemberByUserIdQuery(userId));
    }

    public async Task<int> CreateMemberAsync(CreateMemberDto memberDto)
    {
        var result = await _mediator.Send(new CreateMemberCommand(memberDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateMemberAsync(int id, UpdateMemberDto memberDto)
    {
        var result = await _mediator.Send(new UpdateMemberCommand(id, memberDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteMemberAsync(int id)
    {
        var result = await _mediator.Send(new DeleteMemberCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        return await memberRepository.ExistsAsync(m => m.Id == id);
    }

    public async Task<bool> MembershipNumberExistsAsync(string membershipNumber)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        return await memberRepository.ExistsAsync(m => m.MembershipNumber == membershipNumber);
    }

    public async Task<decimal> GetOutstandingFinesAsync(int memberId)
    {
        return await _mediator.Send(new GetOutstandingFinesQuery(memberId));
    }

    public async Task<bool> UpdateMembershipStatusAsync(int memberId, MembershipStatus status)
    {
        var result = await _mediator.Send(new UpdateMembershipStatusCommand(memberId, status));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return true;
    }

    public async Task<Result<int>> SignUpMemberAsync(MemberSignUpDto signUpDto)
    {
        return await _mediator.Send(new SignUpMemberCommand(signUpDto));
    }
}