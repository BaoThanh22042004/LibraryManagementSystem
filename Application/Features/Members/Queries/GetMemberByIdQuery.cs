using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Members.Queries;

public record GetMemberByIdQuery(int Id) : IRequest<MemberDetailsDto?>;

public class GetMemberByIdQueryHandler : IRequestHandler<GetMemberByIdQuery, MemberDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMemberByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MemberDetailsDto?> Handle(GetMemberByIdQuery request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        
        var member = await memberRepository.GetAsync(
            m => m.Id == request.Id,
            m => m.User,
            m => m.Loans,
            m => m.Reservations,
            m => m.Fines
        );

        if (member == null)
            return null;

        // Load additional related entities for detailed view
        var loanRepository = _unitOfWork.Repository<Loan>();
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        // Get active loans with book copy and book details
        var activeLoans = await loanRepository.ListAsync(
            predicate: l => l.MemberId == request.Id && l.Status == Domain.Enums.LoanStatus.Active,
            includes: new System.Linq.Expressions.Expression<Func<Loan, object>>[] { 
                l => l.BookCopy, 
                l => l.BookCopy.Book 
            }
        );
        
        // Get active reservations with book details
        var activeReservations = await reservationRepository.ListAsync(
            predicate: r => r.MemberId == request.Id && r.Status == Domain.Enums.ReservationStatus.Active,
            includes: new System.Linq.Expressions.Expression<Func<Reservation, object>>[] { r => r.Book }
        );
        
        // Get unpaid fines
        var unpaidFines = await fineRepository.ListAsync(
            predicate: f => f.MemberId == request.Id && f.Status == Domain.Enums.FineStatus.Pending
        );
        
        // Map to DTO
        var memberDto = _mapper.Map<MemberDetailsDto>(member);
        
        // Set related collections
        memberDto.ActiveLoans = _mapper.Map<List<LoanDto>>(activeLoans);
        memberDto.ActiveReservations = _mapper.Map<List<ReservationDto>>(activeReservations);
        memberDto.UnpaidFines = _mapper.Map<List<FineDto>>(unpaidFines);
        
        return memberDto;
    }
}