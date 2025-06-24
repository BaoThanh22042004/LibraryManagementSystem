using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Librarians.Queries;

public record GetLibrarianByUserIdQuery(int UserId) : IRequest<LibrarianDto?>;

public class GetLibrarianByUserIdQueryHandler : IRequestHandler<GetLibrarianByUserIdQuery, LibrarianDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLibrarianByUserIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<LibrarianDto?> Handle(GetLibrarianByUserIdQuery request, CancellationToken cancellationToken)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        
        var librarian = await librarianRepository.GetAsync(
            l => l.UserId == request.UserId,
            l => l.User
        );
        
        if (librarian == null)
            return null;
        
        return _mapper.Map<LibrarianDto>(librarian);
    }
}