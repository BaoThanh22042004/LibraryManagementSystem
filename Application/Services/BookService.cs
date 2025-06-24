using Application.Common;
using Application.DTOs;
using Application.Features.Books.Commands;
using Application.Features.Books.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Services;

public class BookService : IBookService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookDto>> GetPaginatedBooksAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetBooksQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<BookDetailsDto?> GetBookByIdAsync(int id)
    {
        return await _mediator.Send(new GetBookByIdQuery(id));
    }

    public async Task<BookDto?> GetBookByIsbnAsync(string isbn)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var book = await bookRepository.GetAsync(
            b => b.ISBN == isbn,
            b => b.Categories,
            b => b.Copies
        );
        
        return book == null ? null : _mapper.Map<BookDto>(book);
    }

    public async Task<int> CreateBookAsync(CreateBookDto bookDto)
    {
        var result = await _mediator.Send(new CreateBookCommand(bookDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateBookAsync(int id, UpdateBookDto bookDto)
    {
        var result = await _mediator.Send(new UpdateBookCommand(id, bookDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteBookAsync(int id)
    {
        var result = await _mediator.Send(new DeleteBookCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        return await bookRepository.ExistsAsync(b => b.Id == id);
    }

    public async Task<bool> IsbnExistsAsync(string isbn)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        return await bookRepository.ExistsAsync(b => b.ISBN == isbn);
    }

    public async Task<List<BookDto>> GetBooksByCategoryAsync(int categoryId)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var books = await bookRepository.ListAsync(
            predicate: b => b.Categories.Any(c => c.Id == categoryId),
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );
        
        return _mapper.Map<List<BookDto>>(books);
    }

    public async Task<List<BookDto>> GetRecentBooksAsync(int count = 10)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var books = await bookRepository.ListAsync(
            orderBy: q => q.OrderByDescending(b => b.Id),
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );
        
        return _mapper.Map<List<BookDto>>(books.Take(count).ToList());
    }

    public async Task<List<BookDto>> GetPopularBooksAsync(int count = 10)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var loanRepository = _unitOfWork.Repository<Loan>();
        
        // Get all books with their copies and loan counts
        var books = await bookRepository.ListAsync(
            includes: new Expression<Func<Book, object>>[] { b => b.Categories, b => b.Copies }
        );
        
        var bookLoans = new Dictionary<int, int>();
        
        foreach (var book in books)
        {
            var copyIds = book.Copies.Select(c => c.Id).ToList();
            var loans = await loanRepository.ListAsync(
                predicate: l => copyIds.Contains(l.BookCopyId)
            );
            
            bookLoans[book.Id] = loans.Count;
        }
        
        // Order books by loan count and take the top ones
        var popularBooks = books
            .OrderByDescending(b => bookLoans.GetValueOrDefault(b.Id, 0))
            .Take(count)
            .ToList();
        
        return _mapper.Map<List<BookDto>>(popularBooks);
    }
}