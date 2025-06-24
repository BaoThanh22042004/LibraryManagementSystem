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

    public async Task<PagedResult<FineDto>> GetPaginatedFinesAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetPaginatedFinesQuery(request, searchTerm));
    }

    public async Task<FineDto?> GetFineByIdAsync(int id)
    {
        return await _mediator.Send(new GetFineByIdQuery(id));
    }

    public async Task<List<FineDto>> GetFinesByMemberIdAsync(int memberId)
    {
        return await _mediator.Send(new GetFinesByMemberIdQuery(memberId));
    }

    public async Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId)
    {
        return await _mediator.Send(new GetFinesByLoanIdQuery(loanId));
    }

    public async Task<int> CreateFineAsync(CreateFineDto fineDto)
    {
        var result = await _mediator.Send(new CreateFineCommand(fineDto));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateFineAsync(int id, UpdateFineDto fineDto)
    {
        var result = await _mediator.Send(new UpdateFineCommand(id, fineDto));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteFineAsync(int id)
    {
        var result = await _mediator.Send(new DeleteFineCommand(id));
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var fineRepository = _unitOfWork.Repository<Fine>();
        return await fineRepository.ExistsAsync(f => f.Id == id);
    }

    public async Task<bool> PayFineAsync(int id)
    {
        var result = await _mediator.Send(new PayFineCommand(id));
        return result.IsSuccess;
    }

    public async Task<bool> WaiveFineAsync(int id)
    {
        var result = await _mediator.Send(new WaiveFineCommand(id));
        return result.IsSuccess;
    }

    public async Task<List<FineDto>> GetPendingFinesAsync()
    {
        return await _mediator.Send(new GetPendingFinesQuery());
    }

    public async Task<decimal> CalculateOverdueFineAsync(int loanId)
    {
        return await _mediator.Send(new CalculateOverdueFineQuery(loanId));
    }
}