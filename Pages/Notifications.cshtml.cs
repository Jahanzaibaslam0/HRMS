using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HRMS.Pages;

public class NotificationsModel : PageModel
{
    private readonly NotificationService _notifications;

    public NotificationsModel(NotificationService notifications)
    {
        _notifications = notifications;
    }

    public string PageTitle => "Notifications";
    public List<NotificationItem> ActiveNotifications { get; set; } = new();
    public NotificationItem? Selected { get; set; }

    public void OnGet(int? id)
    {
        ActiveNotifications = _notifications.GetActiveNotifications();

        if (id.HasValue && id > 0)
            Selected = _notifications.GetById(id.Value);
        else if (ActiveNotifications.Count > 0)
            Selected = ActiveNotifications[0];
    }
}
