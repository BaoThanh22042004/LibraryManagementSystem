using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Users.Queries;

public record GetUsersQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var userRepository = _unitOfWork.Repository<User>();
        
        var userQuery = await userRepository.PagedListAsync(
            pagedRequest,
            predicate: !string.IsNullOrWhiteSpace(request.SearchTerm) 
                ? u => u.FullName.Contains(request.SearchTerm) || 
                       u.Email.Contains(request.SearchTerm)
                : null,
            orderBy: q => q.OrderBy(u => u.FullName)
        );

        return new PagedResult<UserDto>(
            _mapper.Map<List<UserDto>>(userQuery.Items),
            userQuery.TotalCount,
            userQuery.PageNumber,
            userQuery.PageSize
        );
    }
}