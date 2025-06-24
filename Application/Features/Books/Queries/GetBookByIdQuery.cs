using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Books.Queries;

public record GetBookByIdQuery(int Id) : IRequest<BookDetailsDto?>;

public class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, BookDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BookDetailsDto?> Handle(GetBookByIdQuery request, CancellationToken cancellationToken)
    {
        var bookRepository = _unitOfWork.Repository<Book>();
        
        var book = await bookRepository.GetAsync(
            b => b.Id == request.Id,
            b => b.Categories,
            b => b.Copies
        );

        if (book == null)
            return null;

        return _mapper.Map<BookDetailsDto>(book);
    }
}