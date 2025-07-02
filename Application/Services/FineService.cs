using Application.Common;
using Application.DTOs;
using Application.Features.Fines.Commands;
using Application.Features.Fines.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

/// <summary>
/// Service for managing fine operations in the library system.
/// </summary>
public class FineService : IFineService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FineService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets a paginated list of fines with optional search functionality.
    /// </summary>
    public async Task<PagedResult<FineDto>> GetPaginatedFinesAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetPaginatedFinesQuery(request, searchTerm));
    }

    /// <summary>
    /// Gets a specific fine by its ID.
    /// </summary>
    public async Task<FineDto?> GetFineByIdAsync(int id)
    {
        return await _mediator.Send(new GetFineByIdQuery(id));
    }

    /// <summary>
    /// Gets all fines for a specific member.
    /// </summary>
    public async Task<List<FineDto>> GetFinesByMemberIdAsync(int memberId)
    {
        return await _mediator.Send(new GetFinesByMemberIdQuery(memberId));
    }

    /// <summary>
    /// Gets all fines associated with a specific loan.
    /// </summary>
    public async Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId)
    {
        return await _mediator.Send(new GetFinesByLoanIdQuery(loanId));
    }

    /// <summary>
    /// Gets all pending fines that need payment or waiver.
    /// </summary>
    public async Task<List<FineDto>> GetPendingFinesAsync()
    {
        return await _mediator.Send(new GetPendingFinesQuery());
    }

    /// <summary>
    /// Creates a new fine record.
    /// </summary>
    public async Task<int> CreateFineAsync(CreateFineDto fineDto)
    {
        var result = await _mediator.Send(new CreateFineCommand(fineDto));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    /// <summary>
    /// Updates an existing fine record.
    /// </summary>
    public async Task UpdateFineAsync(int id, UpdateFineDto fineDto)
    {
        var result = await _mediator.Send(new UpdateFineCommand(id, fineDto));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    /// <summary>
    /// Deletes a fine record that is no longer needed.
    /// </summary>
    public async Task DeleteFineAsync(int id)
    {
        var result = await _mediator.Send(new DeleteFineCommand(id));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    /// <summary>
    /// Checks if a fine with the specified ID exists.
    /// </summary>
    public async Task<bool> ExistsAsync(int id)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        return await fineRepository.ExistsAsync(f => f.Id == id);
    }

    /// <summary>
    /// Processes payment for a fine.
    /// </summary>
    public async Task<bool> PayFineAsync(int id)
    {
        var result = await _mediator.Send(new PayFineCommand(id));
        return result.IsSuccess;
    }

    /// <summary>
    /// Waives a fine due to special circumstances.
    /// </summary>
    public async Task<bool> WaiveFineAsync(int id)
    {
        var result = await _mediator.Send(new WaiveFineCommand(id));
        return result.IsSuccess;
    }

    /// <summary>
    /// Calculates the overdue fine amount for a specific loan.
    /// </summary>
    public async Task<decimal> CalculateOverdueFineAsync(int loanId)
    {
        return await _mediator.Send(new CalculateOverdueFineQuery(loanId));
    }
    
    /// <summary>
    /// Calculates and creates a fine for an overdue loan.
    /// </summary>
    public async Task<int> CalculateAndCreateOverdueFineAsync(int loanId)
    {
        var result = await _mediator.Send(new CalculateAndCreateFineCommand(loanId));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
}