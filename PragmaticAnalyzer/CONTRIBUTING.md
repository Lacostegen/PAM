# Как вносить изменения

Этот проект лучше вести через ветки и Pull Request, чтобы основная версия не ломалась случайными изменениями.

## Рабочий процесс

1. Обновить основную ветку:

```powershell
git checkout main
git pull
```

2. Создать отдельную ветку:

```powershell
git checkout -b feature/name-of-change
```

3. Внести изменения и проверить сборку:

```powershell
dotnet build .\PragmaticAnalyzer.csproj
```

4. Зафиксировать изменения:

```powershell
git status
git add .
git commit -m "Описание изменения"
git push -u origin feature/name-of-change
```

5. Создать Pull Request на GitHub.

## Что писать в Pull Request

В описании Pull Request желательно указать:

- что изменено;
- какие вкладки или функции затронуты;
- как проверялось;
- есть ли файлы, которые нужно передать отдельно.

## Что нельзя добавлять в Pull Request

Не добавляйте в GitHub:

- папки `bin/`, `obj/`, `.vs/`;
- `.gguf`-модели;
- папку `NativeLlama/`;
- локальные конфиги из `Config/`;
- временные PDF/DOCX-отчеты;
- личные файлы IDE.

## Проверка перед отправкой

Минимальная проверка для основного проекта:

```powershell
dotnet build .\PragmaticAnalyzer.csproj
```

Проверка отдельного проекта:

```powershell
dotnet build .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
```

Если изменение связано с интерфейсом, нужно вручную открыть затронутую вкладку и проверить, что элементы не налезают друг на друга.
