using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record UpdateBookCopyCommand(int Id, UpdateBookCopyDto BookCopyDto) : IRequest<Result>;

public class UpdateBookCopyCommandHandler : IRequestHandler<UpdateBookCopyCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBookCopyCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateBookCopyCommand request, CancellationToken cancellationToken)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        // Get book copy
        var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.Id);
        
        if (bookCopy == null)
            return Result.Failure($"Book copy with ID {request.Id} not found.");
        
        // Validate copy number uniqueness if changed
        if (bookCopy.CopyNumber != request.BookCopyDto.CopyNumber)
        {
            var copyNumberExists = await bookCopyRepository.ExistsAsync(
                bc => bc.CopyNumber == request.BookCopyDto.CopyNumber && 
                      bc.BookId == bookCopy.BookId && 
                      bc.Id != request.Id
            );
            
            if (copyNumberExists)
                return Result.Failure($"Book copy with number '{request.BookCopyDto.CopyNumber}' already exists for this book.");
        }
        
        // Update book copy properties
        bookCopy.CopyNumber = request.BookCopyDto.CopyNumber;
        bookCopy.Status = request.BookCopyDto.Status;
        
        bookCopyRepository.Update(bookCopy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}