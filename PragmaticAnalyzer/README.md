# PragmaticAnalyzer

PragmaticAnalyzer - WPF-приложение для работы с базами данных по информационной безопасности, онтологиями, моделями Word2Vec/FastText и локальной GGUF-моделью чата с RAG-подходом.

В репозитории есть два проекта:

- `PragmaticAnalyzer.csproj` - основной проект со всеми вкладками.
- `PragmaticAnalyzerChatOnly/PragmaticAnalyzerChatOnly.csproj` - отдельная версия только для общения с моделью.

## Что нужно для разработки

- Windows 10/11.
- Git.
- .NET SDK 8.
- Visual Studio 2022 или Rider, если удобнее работать через IDE.

Проверить установленный SDK:

```powershell
dotnet --version
```

## Как запустить проект

```powershell
git clone https://github.com/OWNER/REPOSITORY.git
cd REPOSITORY\PragmaticAnalyzer
dotnet restore .\PragmaticAnalyzer.csproj
dotnet build .\PragmaticAnalyzer.csproj
dotnet run --project .\PragmaticAnalyzer.csproj
```

Для отдельной версии чата:

```powershell
dotnet restore .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
dotnet build .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
dotnet run --project .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
```

## Как другим людям вносить изменения

Основная ветка `main` должна оставаться рабочей. Все изменения лучше делать через отдельные ветки и Pull Request.

Обычный порядок работы:

```powershell
git checkout main
git pull
git checkout -b feature/short-description
```

После внесения изменений:

```powershell
dotnet build .\PragmaticAnalyzer.csproj
git status
git add .
git commit -m "Краткое описание изменения"
git push -u origin feature/short-description
```

После этого на GitHub нужно открыть Pull Request из своей ветки в `main`.

## Что не хранится в GitHub

В репозиторий не нужно добавлять тяжелые и локальные файлы:

- `bin/`, `obj/`, `.vs/`.
- GGUF-модели из `Translator/`.
- Word2Vec/FastText-модели из `Models/`.
- `NativeLlama/` с `llama-server.exe` и DLL.
- Локальные JSON-конфиги из `Config/`.
- Собранные portable-папки и временные отчеты.

Модели и `NativeLlama` передаются отдельно: архивом, через облако, через GitHub Releases или по внутренней инструкции.

## Дополнительные инструкции

- [Правила внесения изменений](docs/contributing-workflow.md)
- [Установка и запуск](docs/installation.md)
- [Модели и локальный llama-server](docs/models.md)
- [Сборка EXE для другого ПК](docs/publish.md)
