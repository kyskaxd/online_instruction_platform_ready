# Онлайн-инструктажи

Готовая заготовка сайта для прохождения инструктажей онлайн.

Стек:

- Backend: ASP.NET Core / .NET 8 Web API
- База данных: PostgreSQL + Entity Framework Core
- Frontend: Vue 3 + Vite
- Авторизация: JWT
- Роли: `Employee`, `Manager`, `Admin`

## Что уже реализовано

1. Авторизация по JWT.
2. Автоматическое создание первого администратора.
3. Роли: сотрудник, руководитель, админ.
4. Создание сотрудников и учётных записей.
5. Загрузка PDF-материалов руководителем или админом.
6. Просмотр PDF на сайте сотрудником.
7. Импорт теста из JSON-файла.
8. Назначение тестов сотрудникам.
9. Прохождение теста сотрудником.
10. Подсчёт результата на backend.
11. Отчётность по прохождениям.
12. Экспорт отчёта в CSV.

## Структура проекта

```text
online_instruction_platform_ready/
  backend/
    InstructionPlatform.Api/
      Controllers/
      Data/
      Domain/
      Dtos/
      Services/
      Program.cs
      appsettings.json
  frontend/
    instruction-platform-client/
      src/
      package.json
      vite.config.js
  docs/
    sample-test-import.json
```

## 1. Что установить

Нужно установить:

- .NET 8 SDK
- Node.js
- PostgreSQL

## 2. Создать базу данных

Через pgAdmin/psql создай базу:

```sql
CREATE DATABASE instruction_platform;
```

По умолчанию backend подключается так:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=instruction_platform;Username=postgres;Password=postgres"
```

Если у тебя другой пароль PostgreSQL, измени файл:

```text
backend/InstructionPlatform.Api/appsettings.json
```

Например:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=instruction_platform;Username=postgres;Password=12345"
```

## 3. Запустить backend

Можно открыть `OnlineInstructionPlatform.sln` в Visual Studio и запустить проект `InstructionPlatform.Api`.

Или через терминал:

```bash
cd backend/InstructionPlatform.Api
dotnet restore
dotnet run --launch-profile https
```

Swagger будет здесь:

```text
https://localhost:7150/swagger
```

При первом запуске backend сам создаст таблицы и первого администратора.

Данные первого входа:

```text
Email: admin@local.test
Password: Admin123!
```

## 4. Запустить frontend

Открой второй терминал:

```bash
cd frontend/instruction-platform-client
npm install
npm run dev
```

Сайт будет здесь:

```text
http://localhost:5173
```

## 5. Как пользоваться

### Админ или руководитель

1. Войти под `admin@local.test / Admin123!`.
2. Перейти в раздел **Сотрудники**.
3. Создать сотрудника и поставить `Создать учётную запись = Да`.
4. Для обычного сотрудника выбрать роль `Employee`.
5. Перейти в **Материалы** и загрузить PDF.
6. Перейти в **Тесты** и импортировать JSON-тест.
7. Назначить тест сотруднику.
8. Смотреть результаты в разделе **Отчёты**.

### Сотрудник

1. Войти под учётной записью, созданной админом/руководителем.
2. Перейти в **Материалы** и изучить PDF.
3. Перейти в **Мои тесты**.
4. Открыть назначенный тест и пройти его.

## 6. Формат JSON-теста

Пример лежит здесь:

```text
docs/sample-test-import.json
```

Короткий пример:

```json
{
  "title": "Инструктаж по охране труда",
  "description": "Проверка знаний после изучения PDF",
  "passingScorePercent": 80,
  "trainingMaterialId": null,
  "questions": [
    {
      "text": "Что нужно сделать при обнаружении пожара?",
      "type": "SingleChoice",
      "options": [
        { "text": "Сообщить руководителю", "isCorrect": true },
        { "text": "Продолжить работу", "isCorrect": false }
      ]
    }
  ]
}
```

Типы вопросов:

```text
SingleChoice — один правильный ответ
MultipleChoice — несколько правильных ответов
```

## 7. Важные настройки

### Порт backend

Backend запускается на:

```text
https://localhost:7150
```

Frontend проксирует `/api` туда. Это указано в файле:

```text
frontend/instruction-platform-client/vite.config.js
```

Если поменяешь порт backend, поменяй `target` в `vite.config.js`.

### JWT-ключ

Для учебного проекта ключ уже задан в `appsettings.json`. Для реального проекта его надо заменить:

```json
"Key": "INSTRUCTION_PLATFORM_SECRET_KEY_CHANGE_ME_123456789"
```

Ключ должен быть длиннее 32 символов.

### Автосоздание таблиц

Включено:

```json
"Database": {
  "EnsureCreatedOnStart": true
}
```

Это удобно для курсовой/демо. Для полноценного production-проекта лучше перейти на миграции EF Core.

## 8. Если backend не запускается

Проверь:

1. Запущен ли PostgreSQL.
2. Существует ли база `instruction_platform`.
3. Правильный ли пароль в `appsettings.json`.
4. Установлен ли .NET 8 SDK.

## 9. Если frontend не видит backend

Проверь:

1. Backend запущен на `https://localhost:7150`.
2. Открывается ли Swagger.
3. В `vite.config.js` стоит правильный `target`.
4. В браузере нет ошибки сертификата HTTPS. Если есть — сначала открой `https://localhost:7150/swagger` и согласись с локальным сертификатом.
