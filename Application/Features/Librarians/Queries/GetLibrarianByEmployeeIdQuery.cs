using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Librarians.Queries;

public record GetLibrarianByEmployeeIdQuery(string EmployeeId) : IRequest<LibrarianDto?>;

public class GetLibrarianByEmployeeIdQueryHandler : IRequestHandler<GetLibrarianByEmployeeIdQuery, LibrarianDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLibrarianByEmployeeIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<LibrarianDto?> Handle(GetLibrarianByEmployeeIdQuery request, CancellationToken cancellationToken)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        
        var librarian = await librarianRepository.GetAsync(
            l => l.EmployeeId == request.EmployeeId,
            l => l.User
        );
        
        if (librarian == null)
            return null;
        
        return _mapper.Map<LibrarianDto>(librarian);
    }
}