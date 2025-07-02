using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries;

public record GetUserDetailsQuery(int Id) : IRequest<UserDetailsDto?>;

public class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserDetailsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDetailsDto?> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Get user with related entities
        var user = await userRepository.GetAsync(
            u => u.Id == request.Id,
            u => u.Member!,
            u => u.Librarian!);
            
        if (user == null)
        {
            return null;
        }
        
        // Map to DTO
        var userDetailsDto = _mapper.Map<UserDetailsDto>(user);
        
        // Map related entities if they exist
        if (user.Member != null)
        {
            userDetailsDto.Member = _mapper.Map<MemberDto>(user.Member);
        }
        
        if (user.Librarian != null)
        {
            userDetailsDto.Librarian = _mapper.Map<LibrarianDto>(user.Librarian);
        }
        
        return userDetailsDto;
    }
}