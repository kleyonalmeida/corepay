namespace WebApp.Blazor.Services;

public interface IFileDownloadService
{
    Task SaveFileAsync(byte[] content, string fileName, string contentType);
}
