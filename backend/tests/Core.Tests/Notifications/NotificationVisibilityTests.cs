using Core.Application.Notifications;
using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Notifications;

public class NotificationVisibilityTests
{
    private static readonly NotificationAccessContext ManagerAccess =
        new("manager-user-id", ["Manager"]);

    private static readonly NotificationAccessContext DirectorAccess =
        new("director-user-id", ["Director"]);

    private static readonly NotificationAccessContext AdminAccess =
        new("admin-user-id", ["Admin"]);

    private static readonly NotificationAccessContext UserAccess =
        new("plain-user-id", ["User"]);

    [Fact]
    public void IsVisibleTo_UserTarget_ShouldBeVisibleOnlyToMatchingUser()
    {
        var notification = new Notification
        {
            UserId = "manager-user-id",
            RoleTarget = null
        };

        NotificationVisibility.IsVisibleTo(notification, ManagerAccess).Should().BeTrue();
        NotificationVisibility.IsVisibleTo(notification, DirectorAccess).Should().BeFalse();
    }

    [Fact]
    public void IsVisibleTo_RoleTargetDirector_ShouldBeVisibleToDirectorOnly()
    {
        var notification = new Notification
        {
            UserId = null,
            RoleTarget = "Director"
        };

        NotificationVisibility.IsVisibleTo(notification, DirectorAccess).Should().BeTrue();
        NotificationVisibility.IsVisibleTo(notification, ManagerAccess).Should().BeFalse();
        NotificationVisibility.IsVisibleTo(notification, UserAccess).Should().BeFalse();
    }

    [Fact]
    public void IsVisibleTo_RoleTargetAdmin_ShouldBeVisibleToAdminOnly()
    {
        var notification = new Notification
        {
            UserId = null,
            RoleTarget = "Admin"
        };

        NotificationVisibility.IsVisibleTo(notification, AdminAccess).Should().BeTrue();
        NotificationVisibility.IsVisibleTo(notification, DirectorAccess).Should().BeFalse();
    }

    [Fact]
    public void IsVisibleTo_UserWithMatchingRoleAndUserId_ShouldBeVisible()
    {
        var notification = new Notification
        {
            UserId = "director-user-id",
            RoleTarget = "Director"
        };

        NotificationVisibility.IsVisibleTo(notification, DirectorAccess).Should().BeTrue();
    }
}
