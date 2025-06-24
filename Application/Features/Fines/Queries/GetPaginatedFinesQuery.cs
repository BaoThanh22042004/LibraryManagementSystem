using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

public record GetPaginatedFinesQuery(PagedRequest PagedRequest, string? SearchTerm = null) : IRequest<PagedResult<FineDto>>;

public class GetPaginatedFinesQueryHandler : IRequestHandler<GetPaginatedFinesQuery, PagedResult<FineDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaginatedFinesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<FineDto>> Handle(GetPaginatedFinesQuery request, CancellationToken cancellationToken)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        // Build search predicate if search term is provided
        Expression<Func<Fine, bool>>? predicate = null;
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = f => f.Description.ToLower().Contains(searchTerm) ||
                             f.Member.User.FullName.ToLower().Contains(searchTerm);
        }
        
        // Get paginated fines
        var pagedFines = await fineRepository.PagedListAsync(
            pagedRequest: request.PagedRequest,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(f => f.FineDate),
            asNoTracking: true,
            f => f.Member.User,
            f => f.Loan!
        );
        
        // Map entities to DTOs
        var fineDtos = _mapper.Map<List<FineDto>>(pagedFines.Items);
        
        // Add member names to the DTOs
        for (int i = 0; i < pagedFines.Items.Count; i++)
        {
            fineDtos[i].MemberName = pagedFines.Items[i].Member?.User?.FullName ?? "Unknown";
        }
        
        return new PagedResult<FineDto>(
            fineDtos, 
            pagedFines.TotalCount, 
            pagedFines.PageNumber, 
            pagedFines.PageSize
        );
    }
}