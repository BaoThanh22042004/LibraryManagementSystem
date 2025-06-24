using Application.Common;
using Application.DTOs;
using Application.Features.Librarians.Commands;
using Application.Features.Librarians.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class LibrarianService : ILibrarianService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LibrarianService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<LibrarianDto>> GetPaginatedLibrariansAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetLibrariansQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<LibrarianDto?> GetLibrarianByIdAsync(int id)
    {
        return await _mediator.Send(new GetLibrarianByIdQuery(id));
    }

    public async Task<LibrarianDto?> GetLibrarianByEmployeeIdAsync(string employeeId)
    {
        return await _mediator.Send(new GetLibrarianByEmployeeIdQuery(employeeId));
    }

    public async Task<LibrarianDto?> GetLibrarianByUserIdAsync(int userId)
    {
        return await _mediator.Send(new GetLibrarianByUserIdQuery(userId));
    }

    public async Task<int> CreateLibrarianAsync(CreateLibrarianDto librarianDto)
    {
        var result = await _mediator.Send(new CreateLibrarianCommand(librarianDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateLibrarianAsync(int id, UpdateLibrarianDto librarianDto)
    {
        var result = await _mediator.Send(new UpdateLibrarianCommand(id, librarianDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteLibrarianAsync(int id)
    {
        var result = await _mediator.Send(new DeleteLibrarianCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        return await librarianRepository.ExistsAsync(l => l.Id == id);
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId)
    {
        var librarianRepository = _unitOfWork.Repository<Librarian>();
        return await librarianRepository.ExistsAsync(l => l.EmployeeId == employeeId);
    }
}