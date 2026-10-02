using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Dashboard.Interfaces;

namespace TaskManagement.Dashboard.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetProjectDashboard(Guid projectId)
    {
        var dashboard =
            await _dashboardService.GetProjectDashboardAsync(projectId);

        if (dashboard == null)
        {
            return NotFound("Project dashboard data not found.");
        }

        return Ok(dashboard);
    }

    [HttpGet("project/{projectId:guid}/status-counts")]
    public async Task<IActionResult> GetProjectStatusCounts(Guid projectId)
    {
        var statusCounts =
            await _dashboardService.GetProjectStatusCountsAsync(projectId);

        return Ok(statusCounts);
    }

    [HttpGet("project/{projectId:guid}/overdue")]
    public async Task<IActionResult> GetOverdueTasks(Guid projectId)
    {
        var overdueTasks =
            await _dashboardService.GetOverdueTasksAsync(projectId);

        return Ok(new
        {
            projectId,
            overdueTasks
        });
    }

    [HttpGet("sprint/{sprintId:guid}/progress")]
    public async Task<IActionResult> GetSprintProgress(Guid sprintId)
    {
        var progress =
            await _dashboardService.GetSprintProgressAsync(sprintId);

        if (progress == null)
        {
            return NotFound("Sprint progress data not found.");
        }

        return Ok(progress);
    }
}