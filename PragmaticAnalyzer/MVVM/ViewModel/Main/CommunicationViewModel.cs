using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.WorkingServer.Communication;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using PragmaticAnalyzer.Services;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class CommunicationViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private const int MaxHistoryMessages = 12;
        private readonly IApiService _apiService;
        private readonly CancellationTokenSource _cts;
        private ChatMessage _typingMessage;
        public ObservableCollection<ChatMessage> Messages { get; } = [];
        public string UserInput { get => Get<string>(); set => Set(value); }
        public bool IsSending { get => Get<bool>(); set => Set(value); }

        public string ServerStatusText
        {
            get => Get<string>();
            set => Set(value);
        }
        public string KoboldCppPath
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ModelPath
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TranslatorPort
        {
            get => Get<string>();
            set => Set(value);
        }

        public int ContextSize
        {
            get => Get<int>();
            set => Set(value);
        }

        public int MaxTokens
        {
            get => Get<int>();
            set => Set(value);
        }

        public double Temperature
        {
            get => Get<double>();
            set => Set(value);
        }

        public double TopP
        {
            get => Get<double>();
            set => Set(value);
        }

        public double RepeatPenalty
        {
            get => Get<double>();
            set => Set(value);
        }
        public int HistoryMessagesCount
        {
            get => Get<int>();
            set => Set(value);
        }

        public string SystemPrompt
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsServerAvailable
        {
            get => Get<bool>();
            set => Set(value);
        }

        private async void LoadTranslatorConfigAsync()
        {
            var config = await _fileService.LoadFileToPathAsync<TranslatorConfig>(
                GlobalConfig.TranslatorConfigPath,
                _cts.Token);

            config ??= new TranslatorConfig();

            KoboldCppPath = config.KoboldCppPath;
            ModelPath = config.ModelPath;
            TranslatorPort = config.Port;
            ContextSize = config.ContextSize;
            MaxTokens = config.MaxTokens;
            Temperature = config.Temperature;
            TopP = config.TopP;
            RepeatPenalty = config.RepeatPenalty;
            HistoryMessagesCount = config.HistoryMessagesCount;
            SystemPrompt = config.SystemPrompt;
        }

        private async Task SaveTranslatorConfigAsync()
        {
            var config = new TranslatorConfig
            {
                
                KoboldCppPath = KoboldCppPath,
                ModelPath = ModelPath,
                Port = TranslatorPort,
                ContextSize = ContextSize,

                MaxTokens = MaxTokens,
                Temperature = Temperature,
                TopP = TopP,
                RepeatPenalty = RepeatPenalty,
                HistoryMessagesCount = HistoryMessagesCount,
                SystemPrompt = SystemPrompt
            };

            await _fileService.SaveFileAsync(
                config,
                GlobalConfig.TranslatorConfigPath,
                _cts.Token);
        }

        public CommunicationViewModel(IApiService apiService, IFileService fileService)
        {
            IsSending = false;
            IsServerAvailable = false;
            ServerStatusText = "🔴 Модель не проверена";

            _apiService = apiService;
            _fileService = fileService;
            _cts = new CancellationTokenSource();

            LoadTranslatorConfigAsync();

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "👋 Привет! Я готов к диалогу. Задавайте вопросы."
            });
        }

        public RelayCommand StartModelCommand => GetCommand(async o =>
        {
            await StartModelAsync();
        });


        public RelayCommand SelectKoboldCppPathCommand => GetCommand(o =>
        {
            var path = DialogService.OpenFileDialog(DialogService.ExeFilter);

            if (!string.IsNullOrWhiteSpace(path))
            {
                KoboldCppPath = path;
            }
        });

        public RelayCommand SelectModelPathCommand => GetCommand(o =>
        {
            var path = DialogService.OpenFileDialog(DialogService.GgufModelFilter);

            if (!string.IsNullOrWhiteSpace(path))
            {
                ModelPath = path;
            }
        });

        public RelayCommand SaveTranslatorConfigCommand => GetCommand(async o =>
        {
            await SaveTranslatorConfigAsync();

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "✅ Настройки модели сохранены."
            });
        });

        public RelayCommand StopModelCommand => GetCommand(async o =>
        {
            await StopModelAsync();
        });

        public RelayCommand SendCommand => GetCommand(async o =>
        {
            await SendMessageAsync();
        }, o => !IsSending);

        public RelayCommand CheckServerCommand => GetCommand(async o =>
        {
            await CheckServerAvailabilityAsync(showMessage: true);
        });

        private static string CleanAssistantText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var cleaned = text;

            // Удаляет полноценный блок <think> ... </think>, даже если внутри много строк.
            cleaned = Regex.Replace(
                cleaned,
                @"<think\b[^>]*>.*?</think>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // На случай, если модель вернула одиночные теги без пары.
            cleaned = Regex.Replace(
                cleaned,
                @"</?think\b[^>]*>",
                string.Empty,
                RegexOptions.IgnoreCase);

            return cleaned.Trim();
        }
        public RelayCommand ClearCommand => GetCommand(o =>
        {
            ClearChat();
        });

        private async Task StartModelAsync()
        {
            ServerStatusText = "🟡 Запускаю модель...";
            IsServerAvailable = false;

            await SaveTranslatorConfigAsync();

            var result = await _apiService.StartTranslatorServerAsync(
                KoboldCppPath,
                ModelPath,
                TranslatorPort,
                ContextSize,
                _cts.Token);

            if (!result.IsSuccess)
            {
                ServerStatusText = "🔴 Модель не запущена";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"❌ {result.ErrorMessage}"
                });

                return;
            }

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text =
                    "🚀 Запуск модели начат.\n\n" +
                    "Qwen может загружаться 1–3 минуты. Я буду автоматически проверять подключение."
            });

            ServerStatusText = $"🟡 Модель загружается: 127.0.0.1:{TranslatorPort}";

            await WaitForModelReadyAsync();
        }

        private async Task WaitForModelReadyAsync()
        {
            const int maxAttempts = 60;
            const int delayMilliseconds = 3000;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                var isAvailable = await _apiService.IsServerAvailableAsync(
                    "127.0.0.1",
                    TranslatorPort,
                    _cts.Token);

                if (isAvailable)
                {
                    IsServerAvailable = true;
                    ServerStatusText = $"🟢 Модель доступна: 127.0.0.1:{TranslatorPort}";

                    Messages.Add(new ChatMessage
                    {
                        Sender = MessageSender.Assistant,
                        Text = "✅ Модель загружена и готова к работе. Можно отправлять сообщения."
                    });

                    return;
                }

                ServerStatusText =
                    $"🟡 Модель загружается: попытка {attempt}/{maxAttempts}";

                await Task.Delay(delayMilliseconds, _cts.Token);
            }

            IsServerAvailable = false;
            ServerStatusText = $"🔴 Модель не ответила: 127.0.0.1:{GlobalConfig.TranslatorPort}";

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text =
                    "⚠️ Модель не стала доступна за отведённое время.\n\n" +
                    "Проверь, что файл модели корректный, хватает памяти, и KoboldCpp не завершился с ошибкой."
            });
        }

        private async Task StopModelAsync()
        {
            var result = await _apiService.StopTranslatorServerAsync();

            if (result.IsSuccess)
            {
                IsServerAvailable = false;
                ServerStatusText = "🔴 Модель остановлена";

                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = "🛑 Модель остановлена."
                });
            }
            else
            {
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"⚠️ {result.ErrorMessage}"
                });
            }
        }

        private async Task<bool> CheckServerAvailabilityAsync(bool showMessage)
        {
            var port = string.IsNullOrWhiteSpace(TranslatorPort)
                ? GlobalConfig.TranslatorPort
                : TranslatorPort;

            var isAvailable = await _apiService.IsServerAvailableAsync(
                "127.0.0.1",
                port,
                _cts.Token);

            IsServerAvailable = isAvailable;

            ServerStatusText = isAvailable
                ? $"🟢 Модель доступна: 127.0.0.1:{port}"
                : $"🔴 Модель недоступна: 127.0.0.1:{port}";

            if (showMessage)
            {
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = isAvailable
                        ? "✅ Подключение к модели успешно. Можно отправлять сообщения."
                        : $"❌ Модель недоступна. Запусти KoboldCpp на порту {port} или дождись окончания загрузки модели."
                });
            }

            return isAvailable;
        }

        private async Task SendMessageAsync()
        {
            if (IsSending || string.IsNullOrWhiteSpace(UserInput)) return;

            var userMessage = UserInput.Trim();
            UserInput = string.Empty;
            IsSending = true;

            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.User,
                Text = userMessage
            });

            _typingMessage = new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "🤔 Думаю...",
                IsTyping = true
            };
            Messages.Add(_typingMessage);

            try
            {
                var isServerAvailable = await _apiService.IsServerAvailableAsync(
                    "127.0.0.1",
                    GlobalConfig.TranslatorPort,
                    _cts.Token);

                if (!isServerAvailable)
                {
                    Messages.Remove(_typingMessage);

                    Messages.Add(new ChatMessage
                    {
                        Sender = MessageSender.Assistant,
                        Text =
                            "⏳ Модель ещё загружается или сервер KoboldCpp недоступен.\n\n" +
                            "Проверь, что KoboldCpp запущен на порту 5001.\n" +
                            "Если приложение запускает модель автоматически, подожди 1–2 минуты и отправь сообщение ещё раз."
                    });

                    return;
                }
                var promptMessages = BuildPromptMessages();
                var request = new RequestCommunication(
                        promptMessages,
                        TranslatorPort,
                        ContextSize,
                        MaxTokens,
                        Temperature,
                        TopP,
                        RepeatPenalty,
                        SystemPrompt);

                var response = await _apiService.SendRequestAsync<ResponseCommunication>(request, _cts.Token, 1000);

                // Удаляем индикатор
                Messages.Remove(_typingMessage);

                if (response.IsSuccess && response.Value?.Results?.Length > 0)
                {
                    var assistantText = CleanAssistantText(response.Value.Results[0].Text);

                    Messages.Add(new ChatMessage
                    {
                        Sender = MessageSender.Assistant,
                        Text = string.IsNullOrWhiteSpace(assistantText)
                            ? "⚠️ Пустой ответ"
                            : assistantText
                    });
                }
                else
                {
                    var errorMessage = response?.ErrorMessage ?? "Неизвестная ошибка";

                    if (errorMessage.Contains("Сетевая ошибка", StringComparison.OrdinalIgnoreCase) ||
                        errorMessage.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                        errorMessage.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
                        errorMessage.Contains("Подключение не установлено", StringComparison.OrdinalIgnoreCase))
                    {
                        Messages.Add(new ChatMessage
                        {
                            Sender = MessageSender.Assistant,
                            Text =
                                "⏳ Модель ещё загружается или сервер KoboldCpp недоступен.\n\n" +
                                "Дождись строки в окне KoboldCpp:\n" +
                                "Please connect to custom endpoint at http://localhost:5001\n\n" +
                                "После этого отправь сообщение ещё раз."
                        });
                    }
                    else
                    {
                        Messages.Add(new ChatMessage
                        {
                            Sender = MessageSender.Assistant,
                            Text = $"❌ Ошибка: {errorMessage}"
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Messages.Remove(_typingMessage);
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = "⏹️ Запрос отменён"
                });
            }
            catch (Exception ex)
            {
                Messages.Remove(_typingMessage);
                Messages.Add(new ChatMessage
                {
                    Sender = MessageSender.Assistant,
                    Text = $"💥 Исключение: {ex.Message}"
                });
            }
            finally
            {
                IsSending = false;
            }
        }
        private List<CommunicationPromptMessage> BuildPromptMessages()
        {
            var historyLimit = HistoryMessagesCount <= 0
                ? 12
                : HistoryMessagesCount;

            return Messages
                .Where(message =>
                    !message.IsTyping &&
                    !string.IsNullOrWhiteSpace(message.Text) &&
                    !IsSystemChatMessage(message.Text))
                .TakeLast(historyLimit)
                .Select(message => new CommunicationPromptMessage(
                    message.Sender,
                    message.Text))
                .ToList();
        }

        private static bool IsSystemChatMessage(string text)
        {
            return text.StartsWith("👋") ||
                   text.StartsWith("🧹") ||
                   text.StartsWith("🤔") ||
                   text.StartsWith("❌") ||
                   text.StartsWith("💥") ||
                   text.StartsWith("⏹️") ||
                   text.StartsWith("⚠️") ||
                   text.StartsWith("✅") ||
                   text.StartsWith("🚀") ||
                   text.StartsWith("🛑") ||
                   text.StartsWith("🔴") ||
                   text.StartsWith("🟢") ||
                   text.StartsWith("🟡") ||
                   text.StartsWith("⏳");
        }
        private void ClearChat()
        {
            Messages.Clear();
            Messages.Add(new ChatMessage
            {
                Sender = MessageSender.Assistant,
                Text = "🧹 Чат очищен. Начнём сначала!"
            });
        }
        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
    public class ChatMessage : ViewModelBase
    {
        public MessageSender Sender { get; set; }
        public string Text { get => Get<string>(); set => Set(value); }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsTyping { get => Get<bool>(); set => Set(value); }
    }
}