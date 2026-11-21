using sgspe.Models.Companies;

namespace sgspe.Models.Notifications;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    /// <summary>Destinatário específico (responsável pela meta). Nulo = todos os gestores da empresa.</summary>
    public Guid? UserId { get; set; }
    public Guid? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Chave de deduplicação: a mesma situação não gera notificação repetida.</summary>
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public bool EmailSent { get; set; }

    public Company? Company { get; set; }
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Read { get; set; }

    public static NotificationResponse FromEntity(Notification n) => new()
    {
        Id = n.Id, GoalId = n.GoalId, Title = n.Title, Message = n.Message,
        CreatedAt = n.CreatedAt, Read = n.ReadAt is not null
    };
}
