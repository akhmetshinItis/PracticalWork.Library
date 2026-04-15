using System.Text;

namespace PracticalWork.Library.Web.Services.Email;

/// <summary>
/// Рендер шаблонов email из файлов resources.
/// </summary>
public sealed class EmailTemplateRenderer
{
    private readonly IWebHostEnvironment _environment;

    public EmailTemplateRenderer(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> RenderAsync(
        string templateFileName,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(
            _environment.ContentRootPath,
            "resources",
            "email",
            templateFileName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Не найден шаблон email: {templatePath}");
        }

        var content = await File.ReadAllTextAsync(templatePath, Encoding.UTF8, cancellationToken);

        foreach (var (key, value) in placeholders)
        {
            content = content.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
        }

        return content;
    }
}
