using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for creating a new book copy
/// Supports UC015 (Add Copy)
/// </summary>
public class CreateBookCopyDto
{
    /// <summary>
    /// The ID of the parent book this copy belongs to
    /// </summary>
    public int BookId { get; set; }
    
    /// <summary>
    /// Optional custom copy number. If not provided, system will auto-generate one.
    /// </summary>
    public string? CopyNumber { get; set; }
    
    /// <summary>
    /// Initial status of the copy (default: Available)
    /// </summary>
    public CopyStatus Status { get; set; } = CopyStatus.Available;
}

/// <summary>
/// Data transfer object for creating multiple book copies at once
/// Supports UC015 (Add Copy) - bulk creation alternative flow
/// </summary>
public class CreateMultipleBookCopiesDto
{
    /// <summary>
    /// The ID of the parent book these copies belong to
    /// </summary>
    public int BookId { get; set; }
    
    /// <summary>
    /// Number of copies to create (minimum: 1)
    /// </summary>
    public int Quantity { get; set; } = 1;
    
    /// <summary>
    /// Initial status for all created copies (default: Available)
    /// </summary>
    public CopyStatus Status { get; set; } = CopyStatus.Available;
}

/// <summary>
/// Data transfer object for updating a book copy's status
/// Supports UC016 (Update Copy Status)
/// </summary>
public class UpdateBookCopyStatusDto
{
    /// <summary>
    /// The ID of the copy to update
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The new status for the copy
    /// </summary>
    public CopyStatus Status { get; set; }
    
    /// <summary>
    /// Optional notes about the status change
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Basic book copy information DTO
/// Used in BookDetailDto and copy management operations
/// </summary>
public class BookCopyBasicDto
{
    /// <summary>
    /// The unique identifier of the copy
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the parent book
    /// </summary>
    public int BookId { get; set; }
    
    /// <summary>
    /// The copy number
    /// </summary>
    public string CopyNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The current status of the copy
    /// </summary>
    public CopyStatus Status { get; set; }
    
    /// <summary>
    /// Creation date of the copy
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last update date of the copy
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Detailed book copy information DTO
/// Supports copy management operations with complete information
/// </summary>
public class BookCopyDetailDto : BookCopyBasicDto
{
    /// <summary>
    /// Basic information about the parent book
    /// </summary>
    public BookBasicDto Book { get; set; } = null!;
    
    /// <summary>
    /// Flag indicating if this copy has active loans
    /// </summary>
    public bool HasActiveLoans { get; set; }
    
    /// <summary>
    /// Flag indicating if this copy has active reservations
    /// </summary>
    public bool HasActiveReservations { get; set; }
}