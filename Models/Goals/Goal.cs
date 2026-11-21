using System.ComponentModel.DataAnnotations;
using sgspe.Models.Companies;
using sgspe.Models.Enum;
using sgspe.Models.Users;

namespace sgspe.Models.Goals;

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public GoalMetric Metric { get; set; }
    public double TargetValue { get; set; }
    public DateOnly Deadline { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
    public User? ResponsibleUser { get; set; }

    public bool HigherIsBetter => Metric == GoalMetric.RecyclingRatePercent;
}

public class CreateGoalRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public GoalMetric Metric { get; set; }
    [Range(0, double.MaxValue)] public double TargetValue { get; set; }
    public DateOnly Deadline { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}

public class UpdateGoalRequest : CreateGoalRequest
{
    public bool IsActive { get; set; } = true;
}

public class GoalResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public GoalMetric Metric { get; set; }
    public double TargetValue { get; set; }
    public DateOnly Deadline { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public bool IsActive { get; set; }

    public static GoalResponse FromEntity(Goal g) => new()
    {
        Id = g.Id, CompanyId = g.CompanyId, Title = g.Title, Metric = g.Metric,
        TargetValue = g.TargetValue, Deadline = g.Deadline,
        ResponsibleUserId = g.ResponsibleUserId, IsActive = g.IsActive
    };
}

public class GoalProgress
{
    public GoalResponse Goal { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public double? CurrentValue { get; set; }
    public GoalStatus Status { get; set; }
    public bool IsOverdue { get; set; }
    public string? Message { get; set; }
}
