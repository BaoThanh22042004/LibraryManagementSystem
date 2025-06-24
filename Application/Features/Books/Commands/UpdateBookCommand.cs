using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Books.Commands;

public record UpdateBookCommand(int Id, UpdateBookDto BookDto) : IRequest<Result>;

public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBookCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        // Get existing book
        var book = await bookRepository.GetAsync(
            b => b.Id == request.Id,
            b => b.Categories
        );
        
        if (book == null)
        {
            return Result.Failure($"Book with ID {request.Id} not found.");
        }
        
        // Update book properties
        book.Title = request.BookDto.Title;
        book.Author = request.BookDto.Author;
        book.Publisher = request.BookDto.Publisher;
        book.Description = request.BookDto.Description;
        book.CoverImageUrl = request.BookDto.CoverImageUrl;
        book.PublicationDate = request.BookDto.PublicationDate;
        book.Status = request.BookDto.Status;
        
        // Update categories if provided
        if (request.BookDto.CategoryIds.Count > 0)
        {
            var categories = await categoryRepository.ListAsync(
                c => request.BookDto.CategoryIds.Contains(c.Id)
            );
            
            book.Categories.Clear();
            foreach (var category in categories)
            {
                book.Categories.Add(category);
            }
        }
        
        bookRepository.Update(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}