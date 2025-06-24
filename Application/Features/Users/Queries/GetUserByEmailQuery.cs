using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries;

public record GetUserByEmailQuery(string Email) : IRequest<UserDto?>;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserByEmailQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        var user = await userRepository.GetAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
            return null;

        return _mapper.Map<UserDto>(user);
    }
}