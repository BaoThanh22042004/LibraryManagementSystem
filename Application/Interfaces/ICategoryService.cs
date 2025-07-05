using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Service interface for category management operations
/// Supports use cases UC037-UC041
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Creates a new category
    /// Supports UC037 (Create Category)
    /// </summary>
    /// <param name="createCategoryDto">The category data to create</param>
    /// <returns>Result containing the created category data with ID</returns>
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
    
    /// <summary>
    /// Updates an existing category
    /// Supports UC038 (Update Category)
    /// </summary>
    /// <param name="updateCategoryDto">The updated category data</param>
    /// <returns>Result containing the updated category data</returns>
    Task<Result<CategoryDto>> UpdateCategoryAsync(UpdateCategoryDto updateCategoryDto);
    
    /// <summary>
    /// Deletes a category by ID
    /// Supports UC039 (Delete Category)
    /// </summary>
    /// <param name="id">The ID of the category to delete</param>
    /// <returns>Result indicating if deletion was successful</returns>
    Task<Result<bool>> DeleteCategoryAsync(int id);
    
    /// <summary>
    /// Gets a category by ID with associated books
    /// Supports UC040 (View Category Details)
    /// </summary>
    /// <param name="id">The ID of the category to retrieve</param>
    /// <returns>Result containing the category with associated books</returns>
    Task<Result<CategoryWithBooksDto>> GetCategoryWithBooksAsync(int id);
    
    /// <summary>
    /// Gets a paginated list of categories based on search parameters
    /// Supports UC041 (Browse Categories)
    /// </summary>
    /// <param name="searchParams">The search and pagination parameters</param>
    /// <returns>Result containing the paginated list of categories</returns>
    Task<Result<PaginatedCategoriesDto>> GetCategoriesAsync(CategorySearchParametersDto searchParams);
    
    /// <summary>
    /// Gets all categories without pagination (for dropdowns and selection lists)
    /// Supports UC041 (Browse Categories) - alternative flow for complete list
    /// </summary>
    /// <returns>Result containing the list of all categories</returns>
    Task<Result<IEnumerable<CategoryDto>>> GetAllCategoriesAsync();
    
    /// <summary>
    /// Checks if a category name already exists (case-insensitive)
    /// Supports validation for UC037 (Create Category) and UC038 (Update Category)
    /// </summary>
    /// <param name="name">Category name to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>Result indicating if name exists</returns>
    Task<Result<bool>> CategoryNameExistsAsync(string name, int? excludeId = null);
    
    /// <summary>
    /// Checks if a category has any books assigned to it
    /// Supports validation for UC039 (Delete Category)
    /// </summary>
    /// <param name="id">The ID of the category to check</param>
    /// <returns>Result indicating if category has books</returns>
    Task<Result<bool>> CategoryHasBooksAsync(int id);
}