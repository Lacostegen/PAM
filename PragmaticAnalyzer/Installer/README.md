# Сборка установщика

Установщик собирается через Inno Setup 6. Скрипт подготовки пакета:

```powershell
powershell -ExecutionPolicy Bypass -File .\Installer\build-installer.ps1
```

По умолчанию используется режим `-ModelMode One`: в установщик кладется одна GGUF-модель из папки `Translator`.
Приоритет выбора:

1. `YandexGPT-5-Lite-8B-instruct.Q3_K_M.gguf`
2. `yandexgpt-5-lite-8b-instruct-q5_k_m.gguf`
3. `gemma-2b-it-Q4_K_M.gguf`
4. самая маленькая найденная `.gguf` модель

Другие варианты:

```powershell
powershell -ExecutionPolicy Bypass -File .\Installer\build-installer.ps1 -ModelMode None
powershell -ExecutionPolicy Bypass -File .\Installer\build-installer.ps1 -ModelMode All
powershell -ExecutionPolicy Bypass -File .\Installer\build-installer.ps1 -ModelMode One -ModelFile "C:\Models\model.gguf"
```

Что попадает в установку:

- исполняемые файлы программы и .NET runtime;
- базы данных;
- модели FastText/Word2Vec из папки `Models`;
- `NativeLlama`;
- `SemanticSearchEngine` для GloVe + CVSS;
- `matcher.exe`;
- выбранная GGUF-модель в `Translator`;
- Microsoft Visual C++ Redistributable x64, если файл найден на компьютере сборки.

Готовый файл появляется в:

```text
bin\Installer\SPPR-ZI-Setup-1.0.exe
```

Поведение установщика:

- папка установки выбирается на стандартной странице Inno Setup, новый путь можно вписать вручную или создать через кнопку обзора;
- ярлык в меню `Пуск` создается только при включенной галочке `Создать ярлык в меню Пуск`;
- ярлык на рабочем столе создается только при включенной галочке `Создать ярлык на рабочем столе`.
