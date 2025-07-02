using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.AuditLogs.Queries;

/// <summary>
/// Query to retrieve a paginated list of audit logs with optional filtering
/// </summary>
public record GetPaginatedAuditLogsQuery(PagedRequest PagedRequest, AuditLogFilterDto? Filter = null)
	: IRequest<PagedResult<AuditLogDto>>;

/// <summary>
/// Handler for retrieving paginated audit logs
/// </summary>
public class GetPaginatedAuditLogsQueryHandler
	: IRequestHandler<GetPaginatedAuditLogsQuery, PagedResult<AuditLogDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetPaginatedAuditLogsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<PagedResult<AuditLogDto>> Handle(
		GetPaginatedAuditLogsQuery request,
		CancellationToken cancellationToken)
	{
		var filter = request.Filter;
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		Expression<Func<AuditLog, bool>>? predicate = null;

		// Build predicate based on filter criteria
		if (filter != null)
		{
			// Start with a true predicate
			predicate = PredicateBuilder.True<AuditLog>();

			if (filter.UserId.HasValue)
			{
				predicate = predicate.And(a => a.UserId == filter.UserId);
			}

			if (filter.ActionTypes != null && filter.ActionTypes.Length > 0)
			{
				predicate = predicate.And(a => filter.ActionTypes.Contains(a.ActionType));
			}

			if (!string.IsNullOrEmpty(filter.EntityType))
			{
				predicate = predicate.And(a => a.EntityType == filter.EntityType);
			}

			if (!string.IsNullOrEmpty(filter.EntityId))
			{
				predicate = predicate.And(a => a.EntityId == filter.EntityId);
			}

			if (filter.StartDate.HasValue)
			{
				predicate = predicate.And(a => a.CreatedAt >= filter.StartDate.Value);
			}

			if (filter.EndDate.HasValue)
			{
				// Include the entire end date
				var endDatePlusOneDay = filter.EndDate.Value.AddDays(1);
				predicate = predicate.And(a => a.CreatedAt < endDatePlusOneDay);
			}

			if (filter.IsSuccess.HasValue)
			{
				predicate = predicate.And(a => a.IsSuccess == filter.IsSuccess.Value);
			}

			if (!string.IsNullOrEmpty(filter.Module))
			{
				predicate = predicate.And(a => a.Module == filter.Module);
			}

			if (!string.IsNullOrEmpty(filter.SearchTerm))
			{
				var searchTerm = filter.SearchTerm.ToLower();
				predicate = predicate.And(a =>
					(a.Details != null && a.Details.ToLower().Contains(searchTerm)) ||
					(a.EntityName != null && a.EntityName.ToLower().Contains(searchTerm)) ||
					(a.EntityType != null && a.EntityType.ToLower().Contains(searchTerm))
				);
			}
		}

		// Get paginated audit logs
		var pagedAuditLogs = await auditLogRepository.PagedListAsync(
			request.PagedRequest,
			predicate,
			orderBy: q => q.OrderByDescending(a => a.CreatedAt),
			includes: a => a.User!
		);

		// Map to DTOs
		var auditLogDtos = _mapper.Map<List<AuditLogDto>>(pagedAuditLogs.Items);

		// Set user names
		foreach (var dto in auditLogDtos)
		{
			var auditLog = pagedAuditLogs.Items.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog?.User != null)
			{
				dto.UserName = auditLog.User.FullName;
			}

			// Set timestamp from CreatedAt
			dto.Timestamp = auditLog!.CreatedAt;
		}

		return new PagedResult<AuditLogDto>(
			auditLogDtos,
			pagedAuditLogs.TotalCount,
			pagedAuditLogs.PageNumber,
			pagedAuditLogs.PageSize
		);
	}
}

/// <summary>
/// Query to retrieve a specific audit log by ID
/// </summary>
public record GetAuditLogByIdQuery(int Id) : IRequest<AuditLogDto?>;

/// <summary>
/// Handler for retrieving an audit log by ID
/// </summary>
public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto?>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<AuditLogDto?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
	{
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		var auditLog = await auditLogRepository.GetAsync(
			a => a.Id == request.Id,
			a => a.User!
		);

		if (auditLog == null)
			return null;

		var auditLogDto = _mapper.Map<AuditLogDto>(auditLog);

		// Set user name if available
		if (auditLog.User != null)
		{
			auditLogDto.UserName = auditLog.User.FullName;
		}

		// Set timestamp from CreatedAt
		auditLogDto.Timestamp = auditLog.CreatedAt;

		return auditLogDto;
	}
}

/// <summary>
/// Query to retrieve audit logs for a specific entity
/// </summary>
public record GetAuditLogsForEntityQuery(string EntityType, string EntityId)
	: IRequest<List<AuditLogDto>>;

/// <summary>
/// Handler for retrieving audit logs for a specific entity
/// </summary>
public class GetAuditLogsForEntityQueryHandler
	: IRequestHandler<GetAuditLogsForEntityQuery, List<AuditLogDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogsForEntityQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<List<AuditLogDto>> Handle(
		GetAuditLogsForEntityQuery request,
		CancellationToken cancellationToken)
	{
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		var auditLogs = await auditLogRepository.ListAsync(
			a => a.EntityType == request.EntityType && a.EntityId == request.EntityId,
			q => q.OrderByDescending(a => a.CreatedAt),
			includes: a => a.User!
		);

		var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

		// Set user names and timestamps
		foreach (var dto in auditLogDtos)
		{
			var auditLog = auditLogs.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog?.User != null)
			{
				dto.UserName = auditLog.User.FullName;
			}

			// Set timestamp from CreatedAt
			dto.Timestamp = auditLog!.CreatedAt;
		}

		return auditLogDtos;
	}
}

/// <summary>
/// Query to retrieve audit logs for a specific user
/// </summary>
public record GetAuditLogsByUserIdQuery(int UserId) : IRequest<List<AuditLogDto>>;

/// <summary>
/// Handler for retrieving audit logs by user ID
/// </summary>
public class GetAuditLogsByUserIdQueryHandler
	: IRequestHandler<GetAuditLogsByUserIdQuery, List<AuditLogDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogsByUserIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<List<AuditLogDto>> Handle(
		GetAuditLogsByUserIdQuery request,
		CancellationToken cancellationToken)
	{
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();
		var userRepository = _unitOfWork.Repository<User>();

		var user = await userRepository.GetAsync(u => u.Id == request.UserId);
		if (user == null)
			return new List<AuditLogDto>();

		var auditLogs = await auditLogRepository.ListAsync(
			a => a.UserId == request.UserId,
			q => q.OrderByDescending(a => a.CreatedAt)
		);

		var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

		// Set user name and timestamps
		foreach (var dto in auditLogDtos)
		{
			dto.UserName = user.FullName;

			var auditLog = auditLogs.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog != null)
			{
				dto.Timestamp = auditLog.CreatedAt;
			}
		}

		return auditLogDtos;
	}
}

/// <summary>
/// Query to retrieve audit logs for a specific action type
/// </summary>
public record GetAuditLogsByActionTypeQuery(AuditActionType ActionType)
	: IRequest<List<AuditLogDto>>;

/// <summary>
/// Handler for retrieving audit logs by action type
/// </summary>
public class GetAuditLogsByActionTypeQueryHandler
	: IRequestHandler<GetAuditLogsByActionTypeQuery, List<AuditLogDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogsByActionTypeQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<List<AuditLogDto>> Handle(
		GetAuditLogsByActionTypeQuery request,
		CancellationToken cancellationToken)
	{
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		var auditLogs = await auditLogRepository.ListAsync(
			a => a.ActionType == request.ActionType,
			q => q.OrderByDescending(a => a.CreatedAt),
			includes: a => a.User!
		);

		var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

		// Set user names and timestamps
		foreach (var dto in auditLogDtos)
		{
			var auditLog = auditLogs.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog?.User != null)
			{
				dto.UserName = auditLog.User.FullName;
			}

			// Set timestamp from CreatedAt
			dto.Timestamp = auditLog!.CreatedAt;
		}

		return auditLogDtos;
	}
}

/// <summary>
/// Query to retrieve audit logs for a specific date range
/// </summary>
public record GetAuditLogsByDateRangeQuery(DateTime StartDate, DateTime EndDate)
	: IRequest<List<AuditLogDto>>;

/// <summary>
/// Handler for retrieving audit logs by date range
/// </summary>
public class GetAuditLogsByDateRangeQueryHandler
	: IRequestHandler<GetAuditLogsByDateRangeQuery, List<AuditLogDto>>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogsByDateRangeQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<List<AuditLogDto>> Handle(
		GetAuditLogsByDateRangeQuery request,
		CancellationToken cancellationToken)
	{
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		// Include the entire end date
		var endDatePlusOneDay = request.EndDate.AddDays(1);

		var auditLogs = await auditLogRepository.ListAsync(
			a => a.CreatedAt >= request.StartDate && a.CreatedAt < endDatePlusOneDay,
			q => q.OrderByDescending(a => a.CreatedAt),
			includes: a => a.User!
		);

		var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

		// Set user names and timestamps
		foreach (var dto in auditLogDtos)
		{
			var auditLog = auditLogs.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog?.User != null)
			{
				dto.UserName = auditLog.User.FullName;
			}

			// Set timestamp from CreatedAt
			dto.Timestamp = auditLog!.CreatedAt;
		}

		return auditLogDtos;
	}
}

/// <summary>
/// Query to retrieve an audit log report
/// </summary>
public record GetAuditLogReportQuery(AuditLogFilterDto? Filter = null)
	: IRequest<AuditLogReportDto>;

/// <summary>
/// Handler for retrieving an audit log report
/// </summary>
public class GetAuditLogReportQueryHandler
	: IRequestHandler<GetAuditLogReportQuery, AuditLogReportDto>
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;

	public GetAuditLogReportQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
	}

	public async Task<AuditLogReportDto> Handle(
		GetAuditLogReportQuery request,
		CancellationToken cancellationToken)
	{
		var filter = request.Filter;
		var auditLogRepository = _unitOfWork.Repository<AuditLog>();

		Expression<Func<AuditLog, bool>>? predicate = null;

		// Build predicate based on filter criteria
		if (filter != null)
		{
			// Start with a predicate that's always true
			predicate = PredicateBuilder.True<AuditLog>();

			// Add filter conditions
			if (filter.UserId.HasValue)
			{
				predicate = predicate.And(a => a.UserId == filter.UserId);
			}

			if (filter.ActionTypes != null && filter.ActionTypes.Length > 0)
			{
				predicate = predicate.And(a => filter.ActionTypes.Contains(a.ActionType));
			}

			if (!string.IsNullOrEmpty(filter.EntityType))
			{
				predicate = predicate.And(a => a.EntityType == filter.EntityType);
			}

			if (!string.IsNullOrEmpty(filter.EntityId))
			{
				predicate = predicate.And(a => a.EntityId == filter.EntityId);
			}

			if (filter.StartDate.HasValue)
			{
				predicate = predicate.And(a => a.CreatedAt >= filter.StartDate.Value);
			}

			if (filter.EndDate.HasValue)
			{
				// Include the entire end date
				var endDatePlusOneDay = filter.EndDate.Value.AddDays(1);
				predicate = predicate.And(a => a.CreatedAt < endDatePlusOneDay);
			}

			if (filter.IsSuccess.HasValue)
			{
				predicate = predicate.And(a => a.IsSuccess == filter.IsSuccess.Value);
			}

			if (!string.IsNullOrEmpty(filter.Module))
			{
				predicate = predicate.And(a => a.Module == filter.Module);
			}

			if (!string.IsNullOrEmpty(filter.SearchTerm))
			{
				var searchTerm = filter.SearchTerm.ToLower();
				predicate = predicate.And(a =>
					(a.Details != null && a.Details.ToLower().Contains(searchTerm)) ||
					(a.EntityName != null && a.EntityName.ToLower().Contains(searchTerm)) ||
					(a.EntityType != null && a.EntityType.ToLower().Contains(searchTerm))
				);
			}
		}

		// Get audit logs
		var auditLogs = await auditLogRepository.ListAsync(
			predicate,
			q => q.OrderByDescending(a => a.CreatedAt),
			includes: a => a.User!
		);

		// Build the report
		var report = new AuditLogReportDto
		{
			GeneratedAt = DateTime.UtcNow,
			FilterCriteria = filter,
			AuditLogs = _mapper.Map<List<AuditLogDto>>(auditLogs)
		};

		// Set user names and timestamps
		foreach (var dto in report.AuditLogs)
		{
			var auditLog = auditLogs.FirstOrDefault(a => a.Id == dto.Id);
			if (auditLog?.User != null)
			{
				dto.UserName = auditLog.User.FullName;
			}

			// Set timestamp from CreatedAt
			dto.Timestamp = auditLog!.CreatedAt;
		}

		// Calculate statistics
		report.ActionTypeCounts = auditLogs
			.GroupBy(a => a.ActionType)
			.ToDictionary(g => g.Key, g => g.Count());

		report.EntityTypeCounts = auditLogs
			.GroupBy(a => a.EntityType)
			.ToDictionary(g => g.Key, g => g.Count());

		report.SuccessFailureCounts = auditLogs
			.GroupBy(a => a.IsSuccess)
			.ToDictionary(g => g.Key, g => g.Count());

		return report;
	}
}