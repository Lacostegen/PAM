# Runtime-файлы для работы после GitHub

Эта папка содержит файлы, которые не хранятся в GitHub, но нужны для полной работы проекта.

## Как использовать

1. Склонировать репозиторий:

```powershell
git clone https://github.com/Lacostegen/PAM.git
```

2. Скопировать содержимое папки `PragmaticAnalyzer` из комплекта `Необходимое для работы` поверх папки `PragmaticAnalyzer` из репозитория.

3. Отдельно положить GGUF-модель в папку:

```text
PragmaticAnalyzer/Translator/
```

Для отдельного проекта чата модель можно положить также сюда:

```text
PragmaticAnalyzer/PragmaticAnalyzerChatOnly/Translator/
```

## Что входит в комплект

- `matcher.exe` - исполняемый файл для вспомогательных операций проекта.
- `NativeLlama/` - `llama-server.exe` и DLL для запуска локальной GGUF-модели.
- `Models/` - Word2Vec/FastText `.bin`-модели.
- `Config/` - рабочие настройки моделей и RAG.
- `Database/` - локальные базы данных приложения.
- `KnowledgeBase/` - локальная база знаний и индекс RAG.
- `ExploitText/` - рабочая папка под тексты эксплойтов.
- `Translator/` - папка для GGUF-модели. Сама `.gguf`-модель в комплект не включается.

## Что нужно добавить вручную

GGUF-модель не входит в комплект. Ее нужно передать отдельно и положить в `Translator/`.

Старый `koboldcpp.exe` в комплект не входит, потому что программа переведена на запуск одной GGUF-модели через `NativeLlama/llama-server.exe`.

Пример:

```text
Translator/Qwen3.5-9B-Uncensored-HauhauCS-Aggressive-Q4_K_M.gguf
```
