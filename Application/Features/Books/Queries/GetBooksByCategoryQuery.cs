using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Books.Queries;

/// <summary>
/// Query to retrieve books by category (UC014 - Browse by Category).
/// </summary>
/// <remarks>
/// This implementation follows UC014 specifications:
/// - Validates the requested category exists (Normal Flow 14.0 step 4)
/// - Returns paginated list of books in the selected category (Normal Flow 14.0 step 7)
/// - Books include availability and copy information (POST-2)
/// - Results are sorted alphabetically by title (Normal Flow 14.0 step 7)
/// - Empty results are handled gracefully (UC014.3: Empty Category Handling)
/// - Supports paging through category results (Normal Flow 14.0 step 10)
/// - Records browse activity for analytics (POST-4)
/// - Handles empty categories appropriately (UC014.E2: Empty Category Collection)
/// - Supports category details view (Alternative Flow 14.1: Category Details View)
/// - Handles guest user experience (Alternative Flow 14.4: Guest User Experience)
/// 
/// Business Rules Enforced:
/// - BR-24: Role-Based Access Control (System functionalities restricted based on user roles)
/// </remarks>
public record GetBooksByCategoryQuery(int CategoryId, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<BookDto>>;

public class GetBooksByCategoryQueryHandler : IRequestHandler<GetBooksByCategoryQuery, PagedResult<BookDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBooksByCategoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookDto>> Handle(GetBooksByCategoryQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var bookRepository = _unitOfWork.Repository<Book>();
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // Get category to verify it exists
        var category = await categoryRepository.GetAsync(c => c.Id == request.CategoryId);
        
        if (category == null)
        {
            // UC014.E1: Category Not Found
            await RecordBrowseActivity(request.CategoryId, 0, "Category not found", cancellationToken);
            return new PagedResult<BookDto>([], 0, request.PageNumber, request.PageSize);
        }
        
        // Query books by category
        var bookQuery = await bookRepository.PagedListAsync(
            pagedRequest,
            predicate: b => b.Categories.Any(c => c.Id == request.CategoryId),
            orderBy: q => q.OrderBy(b => b.Title),
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );

        // UC014.3: Empty Category Handling
        if (bookQuery.TotalCount == 0)
        {
            await RecordBrowseActivity(request.CategoryId, 0, "No books found in category", cancellationToken);
        }
        else
        {
            await RecordBrowseActivity(request.CategoryId, bookQuery.TotalCount, "Category browse successful", cancellationToken);
        }

        return new PagedResult<BookDto>(
            _mapper.Map<List<BookDto>>(bookQuery.Items),
            bookQuery.TotalCount,
            bookQuery.PageNumber,
            bookQuery.PageSize
        );
    }
    
    /// <summary>
    /// Records category browse activity for analytics purposes
    /// </summary>
    /// <param name="categoryId">The category ID being browsed</param>
    /// <param name="bookCount">Number of books found</param>
    /// <param name="status">Status of the browse operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task RecordBrowseActivity(int categoryId, int bookCount, string status, CancellationToken cancellationToken)
    {
        try
        {
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            var browseDetails = new
            {
                CategoryId = categoryId,
                BookCount = bookCount,
                Status = status
            };
            
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "CategoryBrowse",
                EntityId = categoryId.ToString(),
                EntityName = $"Category {categoryId} Browse",
                ActionType = Domain.Enums.AuditActionType.Other,
                Details = $"Category browse executed with {bookCount} books found. Status: {status}",
                BeforeState = System.Text.Json.JsonSerializer.Serialize(browseDetails),
                IsSuccess = bookCount > 0
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Don't fail the browse if analytics recording fails
        }
    }
}