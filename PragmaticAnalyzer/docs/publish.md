# Сборка EXE для другого ПК

## Основной проект

Команда публикации:

```powershell
dotnet publish .\PragmaticAnalyzer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\bin\Portable\PragmaticAnalyzer
```

Готовая папка:

```text
bin/Portable/PragmaticAnalyzer/
```

На другой ПК нужно передавать всю эту папку целиком.

## Отдельный проект чата

Команда публикации:

```powershell
dotnet publish .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\PragmaticAnalyzerChatOnly\bin\Portable\PragmaticAnalyzerChatOnly
```

Готовая папка:

```text
PragmaticAnalyzerChatOnly/bin/Portable/PragmaticAnalyzerChatOnly/
```

## Что добавить в portable-папку

Для работы вкладки общения с моделью рядом с exe должны быть:

```text
Translator/
NativeLlama/
```

В `Translator/` должна быть `.gguf`-модель.

В `NativeLlama/` должны быть `llama-server.exe` и DLL.

Если используются Word2Vec/FastText, также нужна папка:

```text
Models/
```

## Проверка перед передачей

После сборки откройте exe на своем ПК и проверьте:

- запускается ли программа;
- открываются ли нужные вкладки;
- видит ли программа модель;
- завершается ли `llama-server` после закрытия программы.
