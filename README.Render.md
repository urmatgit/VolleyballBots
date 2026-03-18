# 🚀 Развертывание на Render.com - Постоянная работа

## Гарантия бесперебойной работы

### 1. Конфигурация render.yaml

Файл `render.yaml` настроен для постоянной работы:

```yaml
services:
  - type: web
    name: volleyball-bot
    env: docker
    region: frankfurt
    plan: starter
    
    # Auto-restart
    restartPolicy: always
    
    # Health check
    healthCheckPath: /health
    healthCheckInterval: 30s
    healthCheckTimeout: 5s
    healthCheckRetries: 3
    
    # Auto-deploy
    autoDeploy: true
```

### 2. Dockerfile оптимизирован

- Многоступенчатая сборка
- Минимальный runtime образ
- Health check встроен
- Том для данных

### 3. Настройка на Render.com

#### Вариант A: Через Blueprint

```bash
# Установите Render CLI
npm install -g @render-cloud/cli

# Логин
render login

# Деплой
render blueprint launch render.yaml
```

#### Вариант B: Веб-интерфейс

1. **Создать Web Service:**
   - New + → Web Service
   - Connect repository (GitHub/GitLab)

2. **Configure:**
   ```
   Name: volleyball-bot
   Region: Frankfurt (Europe)
   Branch: main
   Root Directory: VolleyballBot
   Runtime: Docker
   DockerfilePath: ./Dockerfile
   ```

3. **Environment Variables:**
   ```
   DOTNET_ENVIRONMENT=Production
   BotToken=YOUR_BOT_TOKEN
   AdminTelegramIds=YOUR_TELEGRAM_ID
   ConnectionStrings__DefaultConnection=Data Source=/data/volleyball.db
   TZ=Europe/Moscow
   ```

4. **Disk (1GB):**
   - Name: volleyball-data
   - Mount Path: /data

5. **Advanced:**
   - Health Check Path: /health
   - Auto-Deploy: Yes
   - Restart Policy: Always

### 4. Мониторинг

#### Health Check URLs:
- **Health:** `https://your-app.onrender.com/health`
- **Status:** `https://your-app.onrender.com/`
- **Keep-Alive:** `https://your-app.onrender.com/keep-alive`

#### Render Dashboard:
1. Откройте https://dashboard.render.com
2. Выберите сервис
3. Вкладка **Logs** - просмотр логов
4. Вкладка **Metrics** - CPU/RAM usage

### 5. Auto-Restart

Render автоматически перезапустит бота если:
- Health check не прошел (3 попытки)
- Процесс упал с ошибкой
- Превышен лимит памяти

### 6. Предотвращение sleep

Render **не усыпляет** Docker контейнеры на платных тарифах.

**Starter план ($7/мес):**
- ✅ Контейнер работает 24/7
- ✅ Auto-restart при сбоях
- ✅ Health checks

### 7. Alerts (опционально)

Настройте уведомления:

1. Dashboard → Settings → Notifications
2. Включите:
   - Deploy failures
   - Health check failures
   - Service restarts

### 8. Логи

```bash
# Render CLI
render logs -s volleyball-bot -f

# Или в дашборде
Dashboard → volleyball-bot → Logs
```

### 9. Обновление

**Автоматически:**
- Push в ветку `main` → авто-деплой

**Вручную:**
```bash
render deploy -s volleyball-bot
```

### 10. Troubleshooting

#### Бот не запускается:
```bash
# Проверьте логи
render logs -s volleyball-bot

# Проверьте переменные
render env show -s volleyball-bot
```

#### Частые рестарты:
- Проверьте логи на ошибки
- Увеличьте health check timeout
- Проверьте лимиты памяти

#### БД не сохраняется:
- Убедитесь что том подключен
- Path: `/data`

## Стоимость

| Компонент | Цена |
|-----------|------|
| Starter Plan | $7/мес |
| Disk 1GB | $0.50/мес |
| **Итого** | **$7.50/мес** |

## Uptime

- **Ожидаемый:** 99.9%
- **SLA Render:** 99.95%

## Best Practices

1. ✅ Всегда используйте health checks
2. ✅ Логируйте важные события
3. ✅ Настройте алерты
4. ✅ Регулярно проверяйте логи
5. ✅ Делайте бэкапы БД

## Команды

```bash
# Статус
render services show volleyball-bot

# Логи
render logs -s volleyball-bot -f

# Деплой
render deploy -s volleyball-bot

# Рестарт
render restart -s volleyball-bot

# Остановка
render stop -s volleyball-bot

# Запуск
render start -s volleyball-bot
```
