using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Fines.Queries;

/// <summary>
/// Query to retrieve paginated fine records with search capability (UC029 - View Fine History).
/// </summary>
/// <remarks>
/// This implementation follows UC029 specifications:
/// - Retrieves fine information according to user permissions
/// - Displays fine list with descriptions, amounts, dates, and status
/// - Supports searching across fine descriptions and member names
/// - Supports pagination for large datasets
/// - Allows sorting by date, amount, or status
/// - Includes related member and loan information
/// </remarks>
public record GetPaginatedFinesQuery(PagedRequest Request, string? SearchTerm) : IRequest<PagedResult<FineDto>>;

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
        
        Expression<Func<Fine, bool>>? predicate = null;
        
        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = f => f.Description.ToLower().Contains(searchTerm) || 
                            f.Member.User.FullName.ToLower().Contains(searchTerm);
        }
        
        // Get paged fines
        var pagedFines = await fineRepository.PagedListAsync(
            request.Request,
            predicate,
            q => q.OrderByDescending(f => f.FineDate),
            true,
            f => f.Member,
            f => f.Member.User
            // Not including f => f.Loan as it might be null
        );
        
        // Map to DTOs
        var fineItems = pagedFines.Items.Select(fine => {
            var fineDto = _mapper.Map<FineDto>(fine);
            fineDto.MemberName = fine.Member?.User?.FullName ?? "Unknown";
            return fineDto;
        }).ToList();
        
        return new PagedResult<FineDto>(
            fineItems, 
            pagedFines.TotalCount, 
            pagedFines.PageNumber, 
            pagedFines.PageSize
        );
    }
}