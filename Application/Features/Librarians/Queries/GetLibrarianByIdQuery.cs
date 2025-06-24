using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Librarians.Queries;

public record GetLibrarianByIdQuery(int Id) : IRequest<LibrarianDto?>;

public class GetLibrarianByIdQueryHandler : IRequestHandler<GetLibrarianByIdQuery, LibrarianDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLibrarianByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<LibrarianDto?> Handle(GetLibrarianByIdQuery request, CancellationToken cancellationToken)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        
        var librarian = await librarianRepository.GetAsync(
            l => l.Id == request.Id,
            l => l.User
        );
        
        if (librarian == null)
            return null;
        
        return _mapper.Map<LibrarianDto>(librarian);
    }
}