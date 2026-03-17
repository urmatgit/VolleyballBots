# 🐳 Docker для Volleyball Bot

## Быстрый старт

### 1. Запустите Docker Desktop
Убедитесь, что Docker Desktop запущен (иконка в трее)

### 2. Настройте переменные окружения

Создайте файл `.env` в папке `C:\git\VolleyballBots\`:

```bash
# Скопируйте пример
copy .env.example .env

# Откройте .env и заполните значения
notepad .env
```

Пример `.env`:
```env
BOT_TOKEN=1234567890:AABBccDDeeFFggHHiiJJkkLLmmNNooP
ADMIN_TELEGRAM_IDS=85732650
PROXY_URL=http://proxy.karavay.spb.ru:8080
PROXY_USERNAME=erkimbaev
PROXY_PASSWORD=
```

### 3. Запустите бота

```bash
# Перейдите в папку проекта
cd C:\git\VolleyballBots

# Запустите через docker-compose
docker-compose up -d

# Просмотр логов
docker-compose logs -f
```

## Команды управления

```bash
# Запуск
docker-compose up -d

# Остановка
docker-compose down

# Перезапуск
docker-compose restart

# Обновление образа
docker-compose pull
docker-compose up -d

# Логи
docker-compose logs -f volleyball-bot

# Очистка (удаление контейнера и volume)
docker-compose down -v
```

## Ручной запуск (без docker-compose)

```bash
# Сборка образа
docker build -t volleyball-bot VolleyballBot/

# Запуск
docker run -d ^
  --name volleyball-bot ^
  --restart unless-stopped ^
  -e BotToken="YOUR_BOT_TOKEN" ^
  -e AdminTelegramIds="123456789" ^
  -v volleyball-data:/app/data ^
  volleyball-bot

# Логи
docker logs -f volleyball-bot
```

## Структура

```
VolleyballBots/
├── docker-compose.yml      # Конфигурация Docker Compose
├── .env                    # Переменные окружения (создать вручную)
├── .env.example            # Пример переменных
└── VolleyballBot/
    ├── Dockerfile          # Образ Docker
    ├── .dockerignore       # Исключения для сборки
    ├── appsettings.Production.json  # Продакшен конфиг
    └── ...
```

## Тома

- `volleyball-data` — база данных SQLite и файлы

## Конфигурация

Все настройки задаются через переменные окружения:

| Переменная | Описание | Пример |
|------------|----------|--------|
| `BOT_TOKEN` | Токен Telegram бота | `123456:ABC-...` |
| `ADMIN_TELEGRAM_IDS` | ID админов (через запятую) | `123456789,987654321` |
| `PROXY_URL` | URL прокси (опционально) | `http://proxy:8080` |
| `PROXY_USERNAME` | Логин прокси | `user` |
| `PROXY_PASSWORD` | Пароль прокси | `pass` |

## Troubleshooting

### Docker не запускается
```bash
# Проверьте что Docker Desktop запущен
docker --version

# Перезапустите Docker Desktop
```

### Ошибка сборки
```bash
# Очистите кэш Docker
docker system prune -a

# Попробуйте собрать заново
docker-compose build --no-cache
```

### Бот не отвечает
```bash
# Проверьте логи
docker-compose logs volleyball-bot

# Перезапустите контейнер
docker-compose restart volleyball-bot
```
