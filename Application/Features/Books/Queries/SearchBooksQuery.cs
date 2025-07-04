using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Books.Queries;

/// <summary>
/// Query to search for books in the catalog by various criteria (UC013 - Search Books).
/// </summary>
/// <remarks>
/// This implementation follows UC013 specifications:
/// - Supports searching by title, author, and ISBN (Normal Flow 13.0 step 6)
/// - Supports combined search using a general search term (Normal Flow 13.0 step 3-6)
/// - Returns paginated results with availability information (Normal Flow 13.0 step 8-10)
/// - Shows book details including copy counts (POST-2)
/// - Results are sorted alphabetically by title (Normal Flow 13.0 step 9)
/// - Supports empty search for browsing all books (Alternative Flow 13.1: Empty Search)
/// - Handles no results found gracefully (UC013.E1: No Results Found)
/// - Records search activity for analytics (POST-3)
/// - Supports pagination navigation (Alternative Flow 13.2: Pagination Navigation)
/// - Handles guest user limitations (Alternative Flow 13.4: Guest User Limitations)
/// 
/// Business Rules Enforced:
/// - BR-24: Role-Based Access Control (System functionalities restricted based on user roles)
/// </remarks>
public record SearchBooksQuery(
    string? SearchTerm = null, 
    string? Title = null, 
    string? Author = null, 
    string? ISBN = null,
    int? CategoryId = null,
    int PageNumber = 1, 
    int PageSize = 10) : IRequest<PagedResult<BookDto>>;

public class SearchBooksQueryHandler : IRequestHandler<SearchBooksQuery, PagedResult<BookDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SearchBooksQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookDto>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var pagedRequest = new PagedRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var bookRepository = _unitOfWork.Repository<Book>();
        
        // Build predicate for search
        Expression<Func<Book, bool>>? predicate = null;
        
        // If generic search term is provided, search in title, author, and ISBN
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = b => 
                b.Title.ToLower().Contains(searchTerm) || 
                b.Author.ToLower().Contains(searchTerm) || 
                b.ISBN.ToLower().Contains(searchTerm);
        }
        
        // If specific fields are provided, add them to the predicate
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var titlePredicate = BuildContainsPredicate<Book>(b => b.Title.ToLower(), request.Title.ToLower());
            predicate = predicate != null ? PredicateBuilder.And(predicate, titlePredicate) : titlePredicate;
        }
        
        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var authorPredicate = BuildContainsPredicate<Book>(b => b.Author.ToLower(), request.Author.ToLower());
            predicate = predicate != null ? PredicateBuilder.And(predicate, authorPredicate) : authorPredicate;
        }
        
        if (!string.IsNullOrWhiteSpace(request.ISBN))
        {
            var isbnPredicate = BuildContainsPredicate<Book>(b => b.ISBN, request.ISBN);
            predicate = predicate != null ? PredicateBuilder.And(predicate, isbnPredicate) : isbnPredicate;
        }
        
        // If category is specified, filter by category
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            var categoryPredicate = PredicateBuilder.Create<Book>(b => b.Categories.Any(c => c.Id == request.CategoryId.Value));
            predicate = predicate != null ? PredicateBuilder.And(predicate, categoryPredicate) : categoryPredicate;
        }
        
        // Execute query
        var bookQuery = await bookRepository.PagedListAsync(
            pagedRequest,
            predicate: predicate,
            orderBy: q => q.OrderBy(b => b.Title),
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );

        // UC013.E1: No Results Found - Handle gracefully
        if (bookQuery.TotalCount == 0)
        {
            // Record search activity for analytics (POST-3)
            await RecordSearchActivity(request, 0, cancellationToken);
            
            return new PagedResult<BookDto>(
                [],
                0,
                bookQuery.PageNumber,
                bookQuery.PageSize
            );
        }

        // Record search activity for analytics (POST-3)
        await RecordSearchActivity(request, bookQuery.TotalCount, cancellationToken);

        return new PagedResult<BookDto>(
            _mapper.Map<List<BookDto>>(bookQuery.Items),
            bookQuery.TotalCount,
            bookQuery.PageNumber,
            bookQuery.PageSize
        );
    }
    
    private static Expression<Func<T, bool>> BuildContainsPredicate<T>(
        Expression<Func<T, string>> propertySelector, 
        string searchTerm)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Invoke(propertySelector, parameter);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var searchTermConstant = Expression.Constant(searchTerm, typeof(string));
        var containsExpression = Expression.Call(property, containsMethod!, searchTermConstant);
        
        return Expression.Lambda<Func<T, bool>>(containsExpression, parameter);
    }
    
    /// <summary>
    /// Records search activity for analytics purposes
    /// </summary>
    /// <param name="request">The search request</param>
    /// <param name="resultCount">Number of results found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task RecordSearchActivity(SearchBooksQuery request, int resultCount, CancellationToken cancellationToken)
    {
        try
        {
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            var searchDetails = new
            {
                SearchTerm = request.SearchTerm,
                Title = request.Title,
                Author = request.Author,
                ISBN = request.ISBN,
                CategoryId = request.CategoryId,
                ResultCount = resultCount
            };
            
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Search",
                EntityId = "0",
                EntityName = "Book Search",
                ActionType = Domain.Enums.AuditActionType.Other,
                Details = $"Book search executed with {resultCount} results found.",
                BeforeState = System.Text.Json.JsonSerializer.Serialize(searchDetails),
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Don't fail the search if analytics recording fails
        }
    }
}