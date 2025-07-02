using Application.Common;
using Application.DTOs;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class LoanService : ILoanService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LoanService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<LoanDto>> GetPaginatedLoansAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetLoansQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<LoanDto?> GetLoanByIdAsync(int id)
    {
        return await _mediator.Send(new GetLoanByIdQuery(id));
    }

    public async Task<List<LoanDto>> GetLoansByMemberIdAsync(int memberId)
    {
        return await _mediator.Send(new GetLoansByMemberIdQuery(memberId));
    }

    public async Task<List<LoanDto>> GetLoansByBookCopyIdAsync(int bookCopyId)
    {
        return await _mediator.Send(new GetLoansByBookCopyIdQuery(bookCopyId));
    }

    public async Task<int> CreateLoanAsync(CreateLoanDto loanDto)
    {
        var result = await _mediator.Send(new CreateLoanCommand(loanDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateLoanAsync(int id, UpdateLoanDto loanDto)
    {
        var result = await _mediator.Send(new UpdateLoanCommand(id, loanDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExtendLoanAsync(ExtendLoanDto extendLoanDto)
    {
        var result = await _mediator.Send(new ExtendLoanCommand(extendLoanDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return true;
    }

    public async Task<bool> ReturnBookAsync(int loanId)
    {
        var result = await _mediator.Send(new ReturnBookCommand(loanId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var loanRepository = _unitOfWork.Repository<Loan>();
        return await loanRepository.ExistsAsync(l => l.Id == id);
    }

    public async Task<List<LoanDto>> GetOverdueLoansAsync()
    {
        return await _mediator.Send(new GetOverdueLoansQuery());
    }
    
    public async Task<LoanEligibilityDto> CheckLoanEligibilityAsync(int memberId)
    {
        var result = await _mediator.Send(new CheckLoanEligibilityQuery(memberId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    public async Task<List<int>> BulkCheckoutAsync(BulkCheckoutDto bulkCheckoutDto)
    {
        var result = await _mediator.Send(new BulkCheckoutCommand(bulkCheckoutDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    public async Task<List<(int LoanId, bool Success, string? ErrorMessage)>> BulkReturnAsync(BulkReturnDto bulkReturnDto)
    {
        var result = await _mediator.Send(new BulkReturnCommand(bulkReturnDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
    
    public async Task<LoanHistoryDto> GetLoanHistoryAsync(int memberId, int? recentLoansCount = 5)
    {
        var result = await _mediator.Send(new GetLoanHistoryQuery(memberId, recentLoansCount));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }
}