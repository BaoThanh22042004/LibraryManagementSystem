using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for category management operations
/// Supports use cases UC037-UC041
/// </summary>
public interface ICategoryService
{
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request);
    
    Task<Result<CategoryDto>> UpdateCategoryAsync(UpdateCategoryRequest request);
    
    Task<Result<bool>> DeleteCategoryAsync(int id);
    
    Task<Result<CategoryWithBooksDto>> GetCategoryWithBooksAsync(int id);
    
    Task<Result<PagedResult<CategoryDto>>> GetCategoriesAsync(CategorySearchRequest request);
    
    Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();
    
    Task<Result<bool>> CategoryNameExistsAsync(string name, int? excludeId = null);
    
    Task<Result<bool>> CategoryHasBooksAsync(int id);
}