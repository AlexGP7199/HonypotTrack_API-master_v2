using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HonypotTrack.Application.Commons.Bases;
using HonypotTrack.Application.Dtos.Security;
using HoneypotTrack.Infrastrcture.Persistences.Context;

namespace HoneypotTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditReportController(AppDbContext dbContext) : ControllerBase
{
    private readonly AppDbContext _dbContext = dbContext;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditReportItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] AuditReportFilters filters)
    {
        var query = _dbContext.AuditLogs.AsQueryable();

        if (filters.StartDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filters.StartDate.Value);
        }

        if (filters.EndDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= filters.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.IpAddress))
        {
            query = query.Where(a => a.IpAddress == filters.IpAddress);
        }

        if (!string.IsNullOrWhiteSpace(filters.UserEmail))
        {
            query = query.Where(a => a.UserEmail == filters.UserEmail);
        }

        if (!string.IsNullOrWhiteSpace(filters.ActionType))
        {
            query = query.Where(a => a.ActionType == filters.ActionType);
        }

        if (!string.IsNullOrWhiteSpace(filters.ThreatType))
        {
            query = query.Where(a => a.EntityName == filters.ThreatType);
        }

        if (!string.IsNullOrWhiteSpace(filters.RequestPath))
        {
            query = query.Where(a => a.RequestPath.Contains(filters.RequestPath));
        }

        if (!string.IsNullOrWhiteSpace(filters.HttpMethod))
        {
            query = query.Where(a => a.HttpMethod == filters.HttpMethod);
        }

        if (filters.StatusCode.HasValue)
        {
            query = query.Where(a => a.StatusCode == filters.StatusCode.Value);
        }

        if (filters.IsSuccessful.HasValue)
        {
            query = query.Where(a => a.IsSuccessful == filters.IsSuccessful.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.CorrelationId))
        {
            query = query.Where(a => a.CorrelationId == filters.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(filters.SessionId))
        {
            query = query.Where(a => a.SessionId == filters.SessionId);
        }

        if (filters.OnlyThreats)
        {
            query = query.Where(a => a.ActionType == "SECURITY_THREAT");
        }

        if (filters.OnlyHoneypot)
        {
            query = query.Where(a => a.ActionType != null && a.ActionType.StartsWith("HONEYPOT_"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            query = query.Where(a =>
                (a.IpAddress != null && a.IpAddress.Contains(filters.Search)) ||
                (a.UserEmail != null && a.UserEmail.Contains(filters.Search)) ||
                (a.RequestPath != null && a.RequestPath.Contains(filters.Search)) ||
                (a.EntityName != null && a.EntityName.Contains(filters.Search)) ||
                (a.ErrorMessage != null && a.ErrorMessage.Contains(filters.Search)));
        }

        query = ApplyOrdering(query, filters);

        var totalRecords = await query.CountAsync();

        var items = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .Select(a => new AuditReportItemDto
            {
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress,
                UserEmail = a.UserEmail,
                HttpMethod = a.HttpMethod,
                RequestPath = a.RequestPath,
                StatusCode = a.StatusCode,
                IsSuccessful = a.IsSuccessful,
                ActionType = a.ActionType,
                ThreatType = a.EntityName,
                ExecutionTimeMs = a.ExecutionTimeMs,
                CorrelationId = a.CorrelationId,
                ErrorMessage = a.ErrorMessage
            })
            .ToListAsync();

        return Ok(PagedResponse<AuditReportItemDto>.Create(
            items,
            filters.PageNumber,
            filters.PageSize,
            totalRecords));
    }

    private static IQueryable<HoneypotTrack.Domain.Entities.AuditLog> ApplyOrdering(
        IQueryable<HoneypotTrack.Domain.Entities.AuditLog> query,
        AuditReportFilters filters)
    {
        var orderBy = filters.OrderBy?.Trim().ToLowerInvariant();
        var isDescending = filters.IsDescending;

        return orderBy switch
        {
            "ipaddress" => isDescending ? query.OrderByDescending(a => a.IpAddress) : query.OrderBy(a => a.IpAddress),
            "statuscode" => isDescending ? query.OrderByDescending(a => a.StatusCode) : query.OrderBy(a => a.StatusCode),
            "actiontype" => isDescending ? query.OrderByDescending(a => a.ActionType) : query.OrderBy(a => a.ActionType),
            "requestpath" => isDescending ? query.OrderByDescending(a => a.RequestPath) : query.OrderBy(a => a.RequestPath),
            "useremail" => isDescending ? query.OrderByDescending(a => a.UserEmail) : query.OrderBy(a => a.UserEmail),
            _ => isDescending ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp)
        };
    }
}
