using Application.Common;
using Application.DTOs;
using Application.Features.BookCopies.Commands;
using Application.Features.BookCopies.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Services;

public class BookCopyService : IBookCopyService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookCopyService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookCopyDto>> GetPaginatedBookCopiesAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetBookCopiesQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<BookCopyDto?> GetBookCopyByIdAsync(int id)
    {
        return await _mediator.Send(new GetBookCopyByIdQuery(id));
    }

    public async Task<List<BookCopyDto>> GetBookCopiesByBookIdAsync(int bookId)
    {
        return await _mediator.Send(new GetBookCopiesByBookIdQuery(bookId));
    }

    public async Task<int> CreateBookCopyAsync(CreateBookCopyDto bookCopyDto)
    {
        var result = await _mediator.Send(new CreateBookCopyCommand(bookCopyDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateBookCopyAsync(int id, UpdateBookCopyDto bookCopyDto)
    {
        var result = await _mediator.Send(new UpdateBookCopyCommand(id, bookCopyDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteBookCopyAsync(int id)
    {
        var result = await _mediator.Send(new DeleteBookCopyCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        return await bookCopyRepository.ExistsAsync(bc => bc.Id == id);
    }

    public async Task<bool> CopyNumberExistsAsync(string copyNumber, int bookId)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        return await bookCopyRepository.ExistsAsync(bc => bc.CopyNumber == copyNumber && bc.BookId == bookId);
    }

    public async Task<List<BookCopyDto>> GetAvailableBookCopiesAsync(int bookId)
    {
        return await _mediator.Send(new GetAvailableBookCopiesQuery(bookId));
    }

    public async Task<bool> UpdateBookCopyStatusAsync(int id, CopyStatus status)
    {
        var result = await _mediator.Send(new UpdateBookCopyStatusCommand(id, status));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return true;
    }
}