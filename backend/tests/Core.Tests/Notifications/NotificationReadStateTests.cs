using Core.Application.Notifications;
using Core.Domain;
using FluentAssertions;

namespace Core.Tests.Notifications;

public class NotificationReadStateTests
{
    [Fact]
    public void IsReadByUser_WhenReceiptExists_ShouldReturnTrue()
    {
        var notificationId = Guid.NewGuid();
        var notification = new Notification { Id = notificationId };
        var readIds = new HashSet<Guid> { notificationId };

        NotificationReadState.IsReadByUser(notification, "user-1", readIds).Should().BeTrue();
    }

    [Fact]
    public void IsReadByUser_WhenReceiptMissing_ShouldReturnFalse()
    {
        var notification = new Notification { Id = Guid.NewGuid() };
        var readIds = new HashSet<Guid>();

        NotificationReadState.IsReadByUser(notification, "user-1", readIds).Should().BeFalse();
    }
}
