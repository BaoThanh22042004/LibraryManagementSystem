using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookCopies.Commands;

public record UpdateBookCopyCommand(int Id, UpdateBookCopyDto BookCopyDto) : IRequest<Result<bool>>;

public class UpdateBookCopyCommandHandler : IRequestHandler<UpdateBookCopyCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UpdateBookCopyDtoValidator _validator;

    public UpdateBookCopyCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, UpdateBookCopyDtoValidator validator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Result<bool>> Handle(UpdateBookCopyCommand request, CancellationToken cancellationToken)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        // Get book copy
        var bookCopy = await bookCopyRepository.GetAsync(bc => bc.Id == request.Id);
        
        if (bookCopy == null)
            return Result.Failure<bool>($"Book copy with ID {request.Id} not found.");
        
        // Set context for validator
        _validator.SetBookCopyId(request.Id);
        _validator.SetBookId(bookCopy.BookId);
        
        // Validate the DTO
        var validationResult = await _validator.ValidateAsync(request.BookCopyDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<bool>(errors);
        }
        
        // Update book copy properties
        bookCopy.CopyNumber = request.BookCopyDto.CopyNumber;
        bookCopy.Status = request.BookCopyDto.Status;
        bookCopy.LastModifiedAt = DateTime.UtcNow;
        
        bookCopyRepository.Update(bookCopy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success(true);
    }
}