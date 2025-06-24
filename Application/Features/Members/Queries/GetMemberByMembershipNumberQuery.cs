using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Members.Queries;

public record GetMemberByMembershipNumberQuery(string MembershipNumber) : IRequest<MemberDto?>;

public class GetMemberByMembershipNumberQueryHandler : IRequestHandler<GetMemberByMembershipNumberQuery, MemberDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMemberByMembershipNumberQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MemberDto?> Handle(GetMemberByMembershipNumberQuery request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        
        var member = await memberRepository.GetAsync(
            m => m.MembershipNumber == request.MembershipNumber,
            m => m.User
        );

        if (member == null)
            return null;

        return _mapper.Map<MemberDto>(member);
    }
}