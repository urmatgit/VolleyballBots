using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VolleyballBot;

public class HealthCheck
{
    public static void Configure(WebApplication app)
    {
        // Health check endpoint для Render
        app.MapGet("/health", async context =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            
            try
            {
                // Простая проверка - если сервис запустился, значит работает
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OK - Bot is healthy");
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger<HealthCheck>>();
                logger?.LogError(ex, "Health check failed");
                
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync($"Error: {ex.Message}");
            }
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
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; background: #f5f5f5; }
        h1 { color: #0088cc; }
        .status { padding: 15px; background: #e7f5ff; border-radius: 5px; margin: 20px 0; }
        .success { color: #28a745; font-weight: bold; }
        .info { background: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }
        code { background: #f8f9fa; padding: 2px 6px; border-radius: 3px; }
    </style>
</head>
<body>
    <h1>🏐 Volleyball Bot</h1>
    <div class='status'>
        <p class='success'>✅ Бот работает!</p>
        <p><strong>Статус:</strong> Работает непрерывно</p>
        <p><strong>Платформа:</strong> Render.com</p>
        <p><strong>Версия:</strong> 1.0.0</p>
        <p><strong>Health Check:</strong> <code>/health</code></p>
    </div>
    <h2>Как использовать:</h2>
    <ol>
        <li>Откройте Telegram</li>
        <li>Найдите вашего бота</li>
        <li>Нажмите <code>/start</code></li>
    </ol>
    <div class='info'>
        <p>ℹ️ <strong>Для админов:</strong></p>
        <ul>
            <li>➕ Создать игру — пошаговое создание</li>
            <li>📊 Мои игры — управление играми</li>
        </ul>
    </div>
</body>
</html>");
        });
        
        // Keep-alive endpoint для предотвращения sleep
        app.MapGet("/keep-alive", async context =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Alive");
        });
    }
}
