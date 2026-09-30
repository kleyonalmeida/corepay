using FluentAssertions;
using WebApp.Blazor.Formatting;
using WebApp.Blazor.Services;

namespace WebApp.Blazor.Tests.Formatting;

public class NotificationToneMapperTests
{
    [Theory]
    [InlineData(NotificationTypes.PayrollRejected, "notification-row__icon--red")]
    [InlineData(NotificationTypes.PayrollSubmitted, "notification-row__icon--yellow")]
    [InlineData(NotificationTypes.PayrollApproved, "notification-row__icon--emerald")]
    public void Resolve_MatchesVisualRules(string type, string expectedClass)
    {
        NotificationToneMapper.Resolve(type).IconCssClass.Should().Be(expectedClass);
    }
}
