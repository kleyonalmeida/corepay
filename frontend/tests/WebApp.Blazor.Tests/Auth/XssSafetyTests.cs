using FluentAssertions;
using System.Reflection;

namespace WebApp.Blazor.Tests.Auth;

public sealed class XssSafetyTests
{
    [Fact]
    public void BlazorComponents_ShouldNotUseUnsafeHtmlSinks()
    {
        var repoRoot = FindRepoRoot();
        var componentRoot = Path.Combine(repoRoot, "frontend", "src", "WebApp.Blazor");
        var forbiddenPatterns = new[]
        {
            "innerHTML",
            "outerHTML",
            "insertAdjacentHTML",
            "MarkupString",
            "eval(",
            "new Function("
        };

        var razorFiles = Directory.GetFiles(componentRoot, "*.razor", SearchOption.AllDirectories);
        foreach (var file in razorFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in forbiddenPatterns)
            {
                content.Contains(pattern, StringComparison.Ordinal).Should().BeFalse(
                    because: $"{Path.GetRelativePath(repoRoot, file)} must not use {pattern}");
            }
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "agents.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
