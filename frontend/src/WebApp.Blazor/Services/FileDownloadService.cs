using Microsoft.JSInterop;

namespace WebApp.Blazor.Services;

public sealed class FileDownloadService(IJSRuntime jsRuntime) : IFileDownloadService
{
    public async Task SaveFileAsync(byte[] content, string fileName, string contentType)
    {
        var base64 = Convert.ToBase64String(content);
        await jsRuntime.InvokeVoidAsync("corepayDownload.saveFile", fileName, contentType, base64);
    }
}
