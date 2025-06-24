using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.BookCopies.Queries;

public record GetBookCopyByIdQuery(int Id) : IRequest<BookCopyDto?>;

public class GetBookCopyByIdQueryHandler : IRequestHandler<GetBookCopyByIdQuery, BookCopyDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookCopyByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BookCopyDto?> Handle(GetBookCopyByIdQuery request, CancellationToken cancellationToken)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        var bookCopy = await bookCopyRepository.GetAsync(
            bc => bc.Id == request.Id,
            bc => bc.Book
        );

        if (bookCopy == null)
            return null;

        return _mapper.Map<BookCopyDto>(bookCopy);
    }
}