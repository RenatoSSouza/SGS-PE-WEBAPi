using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Enum;
using sgspe.Models.Notifications;
using sgspe.Services.Goals;

namespace sgspe.Services.Notifications;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly GoalService _goals;
    private readonly EmailSender _email;

    public NotificationService(AppDbContext db, GoalService goals, EmailSender email)
    {
        _db = db;
        _goals = goals;
        _email = email;
    }

    /// <summary>
    /// Avalia as metas da empresa no mês informado e cria notificações para alertas novos
    /// (meta próxima do limite, fora do limite ou com prazo vencido). Retorna quantas foram criadas.
    /// </summary>
    public async Task<int> GenerateGoalNotificationsAsync(Guid companyId, int year, int month)
    {
        var alerts = await _goals.GetAlertsAsync(companyId, year, month);
        var created = new List<Notification>();

        foreach (var a in alerts)
        {
            var kind = a.IsOverdue ? "overdue" : a.Status.ToString();
            var key = $"goal:{a.Goal.Id}:{kind}:{year}-{month:00}";
            if (await _db.Notifications.AnyAsync(n => n.CompanyId == companyId && n.Key == key)) continue;

            var n = new Notification
            {
                CompanyId = companyId, GoalId = a.Goal.Id, UserId = a.Goal.ResponsibleUserId, Key = key,
                Title = a.IsOverdue ? $"Meta vencida: {a.Goal.Title}"
                    : a.Status == GoalStatus.Off ? $"Meta fora do limite: {a.Goal.Title}"
                    : $"Meta em atenção: {a.Goal.Title}",
                Message = $"{a.Message} (período {month:00}/{year}" +
                          (a.CurrentValue is null ? "" : $", valor atual {a.CurrentValue:N2}") + $", alvo {a.Goal.TargetValue:N2})."
            };
            _db.Notifications.Add(n);
            created.Add(n);
        }

        if (created.Count == 0) return 0;
        await _db.SaveChangesAsync();
        await SendEmailsAsync(companyId, created);
        return created.Count;
    }

    private async Task SendEmailsAsync(Guid companyId, List<Notification> items)
    {
        if (!_email.IsConfigured) return;
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.CompanyId == companyId && u.IsActive).ToListAsync();
        var managers = users.Where(u => u.Role is EnumRole.Manager or EnumRole.Admin).ToList();

        foreach (var n in items)
        {
            var recipients = n.UserId is not null
                ? users.Where(u => u.Id == n.UserId).Concat(managers).DistinctBy(u => u.Id)
                : managers;
            var sent = false;
            foreach (var u in recipients)
                sent |= await _email.SendAsync(u.Email, $"[SGS-PE] {n.Title}", n.Message);
            if (sent)
            {
                n.EmailSent = true;
                _db.Notifications.Update(n);
            }
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>Gestores/Admin veem todas as notificações da empresa; colaboradores, só as endereçadas a eles.</summary>
    private IQueryable<Notification> Visible(Guid companyId, Guid userId, bool isManager)
        => _db.Notifications.Where(n => n.CompanyId == companyId && (isManager || n.UserId == userId));

    public async Task<List<Notification>> ListAsync(Guid companyId, Guid userId, bool isManager, bool onlyUnread)
    {
        var q = Visible(companyId, userId, isManager).AsNoTracking();
        if (onlyUnread) q = q.Where(n => n.ReadAt == null);
        return await q.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync();
    }

    public Task<int> UnreadCountAsync(Guid companyId, Guid userId, bool isManager)
        => Visible(companyId, userId, isManager).CountAsync(n => n.ReadAt == null);

    public async Task<bool> MarkReadAsync(Guid companyId, Guid userId, bool isManager, Guid id)
    {
        var n = await Visible(companyId, userId, isManager).FirstOrDefaultAsync(x => x.Id == id);
        if (n is null) return false;
        n.ReadAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllReadAsync(Guid companyId, Guid userId, bool isManager)
    {
        var items = await Visible(companyId, userId, isManager).Where(n => n.ReadAt == null).ToListAsync();
        foreach (var n in items) n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
