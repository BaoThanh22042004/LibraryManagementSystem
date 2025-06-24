using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Members.Queries;

public record GetMembersQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<MemberDto>>;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, PagedResult<MemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMembersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var memberRepository = _unitOfWork.Repository<Member>();
        
        var memberQuery = await memberRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? m => m.MembershipNumber.Contains(request.SearchTerm) || 
                       m.User.FullName.Contains(request.SearchTerm) ||
                       m.User.Email.Contains(request.SearchTerm)
                : null,
            orderBy: q => q.OrderBy(m => m.User.FullName),
            includes: new Expression<Func<Member, object>>[] { m => m.User }
        );

        return new PagedResult<MemberDto>(
            _mapper.Map<List<MemberDto>>(memberQuery.Items),
            memberQuery.TotalCount,
            memberQuery.PageNumber,
            memberQuery.PageSize
        );
    }
}