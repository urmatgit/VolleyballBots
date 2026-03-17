using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace VolleyballBot;

public class HealthCheck
{
    public static void Configure(WebApplication app)
    {
        // Health check endpoint для Render
        app.MapGet("/health", async context =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("OK");
        });

        // Root endpoint
        app.MapGet("/", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Volleyball Bot</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; }
        h1 { color: #0088cc; }
        .status { padding: 10px; background: #e7f5ff; border-radius: 5px; }
        .success { color: #28a745; }
    </style>
</head>
<body>
    <h1>🏐 Volleyball Bot</h1>
    <div class='status'>
        <p class='success'>✅ Бот работает!</p>
        <p><strong>Статус:</strong> Работает</p>
        <p><strong>Платформа:</strong> Render.com</p>
        <p><strong>Версия:</strong> 1.0.0</p>
    </div>
    <h2>Как использовать:</h2>
    <ol>
        <li>Откройте Telegram</li>
        <li>Найдите вашего бота</li>
        <li>Нажмите /start</li>
    </ol>
</body>
</html>");
        });
    }
}
