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
/// - Supports searching by title, author, and ISBN
/// - Supports combined search using a general search term
/// - Returns paginated results with availability information
/// - Shows book details including copy counts
/// - Results are sorted alphabetically by title
/// - Supports empty search for browsing all books
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
}