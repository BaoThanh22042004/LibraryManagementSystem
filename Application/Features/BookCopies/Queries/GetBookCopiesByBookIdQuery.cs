using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.BookCopies.Queries;

public record GetBookCopiesByBookIdQuery(int BookId) : IRequest<List<BookCopyDto>>;

public class GetBookCopiesByBookIdQueryHandler : IRequestHandler<GetBookCopiesByBookIdQuery, List<BookCopyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBookCopiesByBookIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<BookCopyDto>> Handle(GetBookCopiesByBookIdQuery request, CancellationToken cancellationToken)
    {
        var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
        
        var bookCopies = await bookCopyRepository.ListAsync(
            predicate: bc => bc.BookId == request.BookId,
            orderBy: q => q.OrderBy(bc => bc.CopyNumber),
            includes: new Expression<Func<BookCopy, object>>[] { bc => bc.Book }
        );

        return _mapper.Map<List<BookCopyDto>>(bookCopies);
    }
}