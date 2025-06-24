using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Librarians.Queries;

public record GetLibrariansQuery(int PageNumber, int PageSize, string? SearchTerm) : IRequest<PagedResult<LibrarianDto>>;

public class GetLibrariansQueryHandler : IRequestHandler<GetLibrariansQuery, PagedResult<LibrarianDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLibrariansQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<LibrarianDto>> Handle(GetLibrariansQuery request, CancellationToken cancellationToken)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        
        // Build search predicate if search term is provided
        Expression<Func<Librarian, bool>>? predicate = null;
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = l => 
                l.EmployeeId.ToLower().Contains(searchTerm) || 
                l.User.FullName.ToLower().Contains(searchTerm) || 
                l.User.Email.ToLower().Contains(searchTerm);
        }
        
        // Create paged request
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        
        // Get paginated librarians
        var pagedLibrarians = await librarianRepository.PagedListAsync(
            pagedRequest: pagedRequest,
            predicate: predicate,
            orderBy: q => q.OrderBy(l => l.User.FullName),
            asNoTracking: true,
            l => l.User
        );
        
        // Map to DTOs
        var librarianDtos = _mapper.Map<List<LibrarianDto>>(pagedLibrarians.Items);
        
        return new PagedResult<LibrarianDto>(
            librarianDtos,
            pagedLibrarians.TotalCount,
            pagedLibrarians.PageNumber,
            pagedLibrarians.PageSize
        );
    }
}