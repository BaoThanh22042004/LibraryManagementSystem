using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record CreateBookCopyCommand(CreateBookCopyDto BookCopyDto) : IRequest<Result<int>>;

public class CreateBookCopyCommandHandler : IRequestHandler<CreateBookCopyCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBookCopyCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateBookCopyCommand request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        // Validate book exists
        var book = await bookRepository.GetAsync(b => b.Id == request.BookCopyDto.BookId);
        if (book == null)
            return Result.Failure<int>($"Book with ID {request.BookCopyDto.BookId} not found.");
        
        // Validate copy number uniqueness if provided
        if (!string.IsNullOrWhiteSpace(request.BookCopyDto.CopyNumber))
        {
            var copyNumberExists = await bookCopyRepository.ExistsAsync(
                bc => bc.CopyNumber == request.BookCopyDto.CopyNumber && bc.BookId == request.BookCopyDto.BookId
            );
            
            if (copyNumberExists)
                return Result.Failure<int>($"Book copy with number '{request.BookCopyDto.CopyNumber}' already exists for this book.");
        }
        
        // Create book copy
        var bookCopy = _mapper.Map<BookCopy>(request.BookCopyDto);
        
        // If copy number is not provided, generate one
        if (string.IsNullOrWhiteSpace(bookCopy.CopyNumber))
        {
            // Count existing copies to determine next copy number
            var existingCopiesCount = await bookCopyRepository.CountAsync(bc => bc.BookId == book.Id);
            bookCopy.CopyNumber = $"{book.ISBN}-{existingCopiesCount + 1:D3}";
        }
        
        // Set default status to Available
        bookCopy.Status = CopyStatus.Available;
        
        await bookCopyRepository.AddAsync(bookCopy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(bookCopy.Id);
    }
}