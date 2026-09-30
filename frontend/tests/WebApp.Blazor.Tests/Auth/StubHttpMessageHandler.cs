namespace WebApp.Blazor.Tests.Auth;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public int RequestCount { get; private set; }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public void Configure(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        return Task.FromResult(_responder(request));
    }
}
