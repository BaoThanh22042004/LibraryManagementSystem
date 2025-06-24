using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Categories.Queries;

public record GetCategoryByIdQuery(int Id) : IRequest<CategoryDetailsDto?>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCategoryByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<CategoryDetailsDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();
        
        var category = await categoryRepository.GetAsync(
            c => c.Id == request.Id,
            c => c.Books
        );

        if (category == null)
            return null;

        return _mapper.Map<CategoryDetailsDto>(category);
    }
}