# Установка проекта для разработки

## 1. Установить Git

Скачать Git можно с официального сайта:

```text
https://git-scm.com/download/win
```

После установки проверить:

```powershell
git --version
```

## 2. Установить .NET SDK 8

Проверить наличие SDK:

```powershell
dotnet --version
```

Если SDK не установлен, нужно поставить .NET 8 SDK для Windows.

## 3. Склонировать проект

```powershell
git clone https://github.com/OWNER/REPOSITORY.git
cd REPOSITORY\PragmaticAnalyzer
```

`OWNER/REPOSITORY` нужно заменить на настоящий адрес репозитория.

## 4. Собрать основной проект

```powershell
dotnet restore .\PragmaticAnalyzer.csproj
dotnet build .\PragmaticAnalyzer.csproj
```

## 5. Собрать отдельный проект чата

```powershell
dotnet restore .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
dotnet build .\PragmaticAnalyzerChatOnly\PragmaticAnalyzerChatOnly.csproj
```

## 6. Добавить локальные модели

Для чата с GGUF-моделью рядом с exe или в папке проекта должны быть:

```text
Translator/
NativeLlama/
```

В `Translator/` кладется `.gguf`-модель.

В `NativeLlama/` кладутся `llama-server.exe` и DLL из сборки `llama.cpp`.

Эти файлы не хранятся в GitHub и передаются отдельно.
