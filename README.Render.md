# 🚀 Развертывание на Render.com

## Быстрый старт

### 1. Подготовка

1. Создайте аккаунт на [Render.com](https://render.com)
2. Подключите GitHub репозиторий с кодом бота
3. Получите токен бота у @BotFather

### 2. Развертывание через Render Blueprint

```bash
# Через Render CLI
render blueprint launch render.yaml
```

Или через веб-интерфейс:
1. Нажмите "New +" в дашборде
2. Выберите "Blueprint"
3. Подключите репозиторий
4. Выберите файл `render.yaml`

### 3. Настройка переменных окружения

В дашборде Render укажите:

| Переменная | Значение |
|------------|----------|
| `BotToken` | Токен от @BotFather |
| `AdminTelegramIds` | Ваш Telegram ID |
| `Proxy__Url` | (опционально) URL прокси |
| `Proxy__Username` | (опционально) Логин прокси |
| `Proxy__Password` | (опционально) Пароль прокси |

### 4. Проверка

После деплоя:
1. Откройте логи в дашборде Render
2. Проверьте что бот запустился
3. Напишите боту в Telegram

## Ручное развертывание

### 1. Создать Web Service

1. **New +** → **Web Service**
2. Подключить репозиторий
3. Настроить:

```
Name: volleyball-bot
Region: Frankfurt (Europe)
Branch: main
Root Directory: VolleyballBot
Runtime: .NET 9
Build Command: dotnet publish -c Release -o /opt/render/project/src
Start Command: dotnet VolleyballBot.dll
```

### 2. Добавить диск

1. **Disks** → **Add Disk**
2. Name: `volleyball-data`
3. Size: 1 GB
4. Mount Path: `/opt/render/project/src/data`

### 3. Переменные окружения

Добавьте в **Environment**:

```bash
DOTNET_ENVIRONMENT=Production
BotToken=your_bot_token_here
AdminTelegramIds=your_telegram_id
ConnectionStrings__DefaultConnection=Data Source=/opt/render/project/src/data/volleyball.db
```

### 4. Деплой

Нажмите **Deploy** и следите за логами.

## Стоимость

- **Starter план**: $7/месяц
- **Disk 1GB**: $0.50/месяц
- **Итого**: ~$7.50/месяц

## Метрики

- CPU: 0.5 CPU
- RAM: 512 MB
- Disk: 1 GB SSD
- Bandwidth: 1 TB

## Логи

```bash
# Через Render CLI
render logs -s volleyball-bot

# Через веб-интерфейс
Dashboard → volleyball-bot → Logs
```

## Обновление

При push в ветку `main` автоматический деплой.

Или вручную:
```bash
render deploy -s volleyball-bot
```

## Troubleshooting

### Бот не запускается
- Проверьте логи в дашборде
- Убедитесь что токен правильный
- Проверьте переменные окружения

### Ошибка БД
- Проверьте что диск подключен
- Путь: `/opt/render/project/src/data`

### 407 Proxy Error
- Удалите переменные Proxy__* для работы без прокси
- Или укажите правильные данные прокси

## Render CLI

```bash
# Установка
npm install -g @render-cloud/cli

# Логин
render login

# Список сервисов
render services list

# Логи
render logs -s volleyball-bot

# Деплой
render deploy -s volleyball-bot
```
