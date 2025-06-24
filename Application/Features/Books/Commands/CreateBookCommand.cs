using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Books.Commands;

public record CreateBookCommand(CreateBookDto BookDto) : IRequest<Result<int>>;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBookCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // Check if ISBN already exists
        var isbnExists = await bookRepository.ExistsAsync(b => b.ISBN == request.BookDto.ISBN);
        if (isbnExists)
        {
            return Result.Failure<int>($"A book with ISBN {request.BookDto.ISBN} already exists.");
        }

        // Create new book
        var book = _mapper.Map<Book>(request.BookDto);
        book.Status = BookStatus.Available;
        
        await bookRepository.AddAsync(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Add categories
        if (request.BookDto.CategoryIds.Count > 0)
        {
            var categories = await categoryRepository.ListAsync(
                c => request.BookDto.CategoryIds.Contains(c.Id)
            );
            
            book.Categories = categories.ToList();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        // Create initial copies
        if (request.BookDto.InitialCopiesCount > 0)
        {
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var copies = new List<BookCopy>();
            
            for (int i = 1; i <= request.BookDto.InitialCopiesCount; i++)
            {
                var copy = new BookCopy
                {
                    BookId = book.Id,
                    CopyNumber = $"{book.ISBN}-{i:D3}",
                    Status = CopyStatus.Available
                };
                copies.Add(copy);
            }
            
            await bookCopyRepository.AddRangeAsync(copies);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        return Result.Success(book.Id);
    }
}