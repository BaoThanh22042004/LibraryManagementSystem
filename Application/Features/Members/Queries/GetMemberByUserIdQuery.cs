using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Members.Queries;

public record GetMemberByUserIdQuery(int UserId) : IRequest<MemberDto?>;

public class GetMemberByUserIdQueryHandler : IRequestHandler<GetMemberByUserIdQuery, MemberDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMemberByUserIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MemberDto?> Handle(GetMemberByUserIdQuery request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        
        var member = await memberRepository.GetAsync(
            m => m.UserId == request.UserId,
            m => m.User
        );

        if (member == null)
            return null;

        return _mapper.Map<MemberDto>(member);
    }
}