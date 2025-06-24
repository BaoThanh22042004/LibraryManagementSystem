using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries;

public record GetUserByIdQuery(int Id) : IRequest<UserDto?>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Using GetAsync instead of GetByIdAsync
        var user = await userRepository.GetAsync(u => u.Id == request.Id);

        if (user == null)
            return null;

        return _mapper.Map<UserDto>(user);
    }
}