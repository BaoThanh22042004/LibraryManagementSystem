using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Application.Interfaces.Services;
using System.IO;

namespace Application.Features.Members.Commands;

public record SignUpMemberCommand(MemberSignUpDto SignUpDto) : IRequest<Result<int>>;

public class SignUpMemberCommandHandler : IRequestHandler<SignUpMemberCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public SignUpMemberCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<Result<int>> Handle(SignUpMemberCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var memberRepository = _unitOfWork.Repository<Member>();
        
        // Step 1: Check if email already exists
        var emailExists = await userRepository.ExistsAsync(u => u.Email.ToLower() == request.SignUpDto.Email.ToLower());
        if (emailExists)
        {
            return Result.Failure<int>($"User with email '{request.SignUpDto.Email}' already exists.");
        }
        
        // Step 2: Generate a membership number if not provided
        string membershipNumber = request.SignUpDto.MembershipNumber ?? GenerateMembershipNumber(request.SignUpDto.FullName);
        
        // Step 3: Check if membership number already exists
        var membershipNumberExists = await memberRepository.ExistsAsync(
            m => m.MembershipNumber == membershipNumber
        );
        
        if (membershipNumberExists)
        {
            // If the membership number already exists, generate a new one with a timestamp
            membershipNumber = GenerateMembershipNumber(request.SignUpDto.FullName, true);
        }
        
        try
        {
            // Step 4: Begin transaction
            await _unitOfWork.BeginTransactionAsync();
            
            // Step 5: Create user
            var user = new User
            {
                FullName = request.SignUpDto.FullName,
                Email = request.SignUpDto.Email,
                PasswordHash = PasswordHasher.HashPassword(request.SignUpDto.Password),
                Role = UserRole.Member
            };
            
            await userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Step 6: Create member
            var member = new Member
            {
                MembershipNumber = membershipNumber,
                UserId = user.Id,
                MembershipStartDate = DateTime.UtcNow,
                MembershipStatus = MembershipStatus.Active,
                OutstandingFines = 0
            };
            
            await memberRepository.AddAsync(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Step 7: Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            // Send welcome email to the new member
            string templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "Application", "EmailTemplates", "MemberWelcomeEmail.html");
            string emailBody = File.Exists(templatePath)
                ? File.ReadAllText(templatePath)
                : $@"
                    <h2>Welcome to the Library!</h2>
                    <p>Dear {user.FullName},</p>
                    <p>Your membership has been successfully registered.</p>
                    <ul>
                        <li>Membership Number: {membershipNumber}</li>
                    </ul>
                    <p>You can now log in and start using library services.</p>
                    <p>Regards,<br>Library Management System</p>
                ";
            emailBody = emailBody
                .Replace("{{FullName}}", user.FullName)
                .Replace("{{MembershipNumber}}", membershipNumber);

            string emailSubject = "Welcome to the Library!";
            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

            return Result.Success(member.Id);
        }
        catch (Exception ex)
        {
            // Roll back transaction on error
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create member: {ex.Message}");
        }
    }
    
    private string GenerateMembershipNumber(string fullName, bool includeTimestamp = false)
    {
        // Generate a membership number based on the user's name (first 3 letters of their name) 
        // followed by a random 4-digit number
        
        // Clean the name (remove spaces, special characters)
        var cleanName = Regex.Replace(fullName, @"[^a-zA-Z0-9]", "");
        
        // Take the first 3 letters (or fewer if name is shorter)
        var namePrefix = cleanName.Length >= 3 
            ? cleanName.Substring(0, 3).ToUpper() 
            : cleanName.PadRight(3, 'X').ToUpper();
        
        // Generate a random 4-digit number
        var random = new Random();
        var randomPart = random.Next(1000, 9999).ToString();
        
        // Add a timestamp suffix if needed (to ensure uniqueness)
        var timestampPart = includeTimestamp 
            ? DateTime.UtcNow.ToString("MMddHHmm") 
            : string.Empty;
        
        return $"{namePrefix}-{randomPart}{timestampPart}";
    }
}