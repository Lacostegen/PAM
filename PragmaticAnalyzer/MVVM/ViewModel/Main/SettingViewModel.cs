using CommunityToolkit.Mvvm.Messaging;
using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.Messages;
using PragmaticAnalyzer.Services;
using PragmaticAnalyzer.WorkingServer.Retrain;
using PragmaticAnalyzer.WorkingServer.Train;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class SettingViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IFileService _fileService;
        private const string BinModelExtension = ".bin";
        private const string GgufModelExtension = ".gguf";

        public ObservableCollection<ModelConfig> WordTwoVecConfigs { get; set; }
        public ObservableCollection<ModelConfig> FastTextConfigs { get; set; }

        public ModelConfig SelectedWordTwoVecConfig { get => Get<ModelConfig>(); set => Set(value); }
        public ModelConfig SelectedFastTextConfig { get => Get<ModelConfig>(); set => Set(value); }

        public bool ProgressWordTwoVec { get => Get<bool>(); set => Set(value); }
        public bool ProgressFastText { get => Get<bool>(); set => Set(value); }

        public string WordTwoVecStatusText { get => Get<string>(); set => Set(value); }
        public string FastTextStatusText { get => Get<string>(); set => Set(value); }

        public bool HasWordTwoVecModels => WordTwoVecConfigs.Count > 0;

        public bool HasFastTextModels => FastTextConfigs.Count > 0;

        public SettingViewModel(
            ObservableCollection<ModelConfig> wordTwoVecConfig,
            ObservableCollection<ModelConfig> fastTextVecConfig,
            IApiService apiService)
        {
            WordTwoVecConfigs = wordTwoVecConfig;
            FastTextConfigs = fastTextVecConfig;
            _apiService = apiService;
            _fileService = new FileService();

            ProgressWordTwoVec = false;
            ProgressFastText = false;
            RefreshModelAvailabilityStatus();

            WordTwoVecConfigs.CollectionChanged += OnModelsCollectionChanged;
            FastTextConfigs.CollectionChanged += OnModelsCollectionChanged;

            foreach (var model in WordTwoVecConfigs)
            {
                model.PropertyChanged += OnModelPropertyChanged;
            }

            foreach (var model in FastTextConfigs)
            {
                model.PropertyChanged += OnModelPropertyChanged;
            }
        }

        public RelayCommand UploadWordTwoVecModelCommand => GetCommand(async o =>
        {
            var currentPath = DialogService.OpenFileDialog(DialogService.ModelFilter);
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                WordTwoVecStatusText = "Загрузка Word2Vec-модели отменена.";
                return;
            }

            ProgressWordTwoVec = true;
            WordTwoVecStatusText = "Копирую Word2Vec-модель в папку Models...";

            try
            {
                var finalPath = CopyModelToModelsDirectory(currentPath);
                SelectedWordTwoVecConfig = await AddOrSelectModelAsync(
                    WordTwoVecConfigs,
                    finalPath,
                    Algorithm.WordTwoVec,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath);

                WordTwoVecStatusText = $"Word2Vec-модель загружена: {Path.GetFileName(finalPath)}";
            }
            catch (Exception ex)
            {
                WordTwoVecStatusText = "Ошибка загрузки Word2Vec-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressWordTwoVec = false;
            }
        }, o => !ProgressWordTwoVec);

        public RelayCommand DeleteWordTwoVecModelCommand => GetCommand(async o =>
        {
            if (SelectedWordTwoVecConfig is null)
            {
                return;
            }

            ProgressWordTwoVec = true;
            WordTwoVecStatusText = "Удаляю Word2Vec-модель...";

            try
            {
                await DeleteModelAsync(
                    WordTwoVecConfigs,
                    SelectedWordTwoVecConfig,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath);

                SelectedWordTwoVecConfig = WordTwoVecConfigs.FirstOrDefault();
                WordTwoVecStatusText = "Word2Vec-модель удалена.";
            }
            catch (Exception ex)
            {
                WordTwoVecStatusText = "Ошибка удаления Word2Vec-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressWordTwoVec = false;
            }
        }, o => SelectedWordTwoVecConfig is not null && !ProgressWordTwoVec);

        public RelayCommand UseWordTwoVecModelCommand => GetCommand(async o =>
        {
            if (SelectedWordTwoVecConfig is null)
            {
                return;
            }

            try
            {
                if (!await UseModelAsync(
                    WordTwoVecConfigs,
                    SelectedWordTwoVecConfig,
                    Algorithm.WordTwoVec,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath,
                    "Word2Vec"))
                {
                    WordTwoVecStatusText = "Файл Word2Vec-модели не найден.";
                    return;
                }

                WordTwoVecStatusText = $"Используется Word2Vec-модель: {SelectedWordTwoVecConfig.DisplayedName}";
            }
            catch (Exception ex)
            {
                WordTwoVecStatusText = "Ошибка выбора Word2Vec-модели.";
                MessageBox.Show(ex.Message);
            }
        }, o => SelectedWordTwoVecConfig is not null && SelectedWordTwoVecConfig.IsUsed is false && !ProgressWordTwoVec);

        public RelayCommand RetrainWordTwoVecModelCommand => GetCommand(async o =>
        {
            if (SelectedWordTwoVecConfig is null)
            {
                return;
            }

            ProgressWordTwoVec = true;
            WordTwoVecStatusText = "Проверяю Word2Vec-модель и matcher-сервер...";

            try
            {
                if (!await EnsureMatcherModelReadyAsync(
                    WordTwoVecConfigs,
                    SelectedWordTwoVecConfig,
                    Algorithm.WordTwoVec,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath,
                    "Word2Vec"))
                {
                    WordTwoVecStatusText = "Файл Word2Vec-модели не найден.";
                    return;
                }

                if (!await EnsureMatcherForModelOperationAsync(status => WordTwoVecStatusText = status))
                {
                    return;
                }

                var matcherModelPathResult = _apiService.PrepareMatcherModelPath(SelectedWordTwoVecConfig.Path);
                if (!matcherModelPathResult.IsSuccess)
                {
                    WordTwoVecStatusText = "Не удалось подготовить Word2Vec-модель для matcher.";
                    MessageBox.Show(matcherModelPathResult.ErrorMessage);
                    return;
                }

                WordTwoVecStatusText = "Дообучаю Word2Vec-модель...";
                RequestRetrain request = new(
                    "127.0.0.1",
                    GlobalConfig.MatcherPort,
                    matcherModelPathResult.Value,
                    Algorithm.WordTwoVec);

                var result = await _apiService.SendRequestAsync<ResponseRetrain>(request);
                if (!result.IsSuccess)
                {
                    WordTwoVecStatusText = "Ошибка дообучения Word2Vec-модели.";
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                var modelPath = result.Value?.ModelPath;
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    WordTwoVecStatusText = "Сервер не вернул путь к дообученной Word2Vec-модели.";
                    MessageBox.Show("Сервер завершил дообучение, но не вернул путь к модели.");
                    return;
                }

                var finalModelPath = CopyModelToModelsDirectory(modelPath);

                SelectedWordTwoVecConfig = await AddOrSelectModelAsync(
                    WordTwoVecConfigs,
                    finalModelPath,
                    Algorithm.WordTwoVec,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath);

                WordTwoVecStatusText = $"Word2Vec-модель дообучена: {Path.GetFileName(finalModelPath)}";
            }
            catch (Exception ex)
            {
                WordTwoVecStatusText = "Ошибка дообучения Word2Vec-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressWordTwoVec = false;
            }
        }, o => SelectedWordTwoVecConfig is not null && !ProgressWordTwoVec);

        public RelayCommand TrainWordTwoVecModelCommand => GetCommand(async o =>
        {
            ProgressWordTwoVec = true;
            WordTwoVecStatusText = "Проверяю matcher-сервер...";

            try
            {
                if (!await EnsureMatcherForModelOperationAsync(status => WordTwoVecStatusText = status))
                {
                    return;
                }

                WordTwoVecStatusText = "Обучаю Word2Vec-модель...";
                RequestTrain request = new("127.0.0.1", GlobalConfig.MatcherPort, Algorithm.WordTwoVec);
                var result = await _apiService.SendRequestAsync<ResponseTrain>(request);

                if (!result.IsSuccess)
                {
                    WordTwoVecStatusText = "Ошибка обучения Word2Vec-модели.";
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                var modelPath = result.Value?.ModelPath;
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    WordTwoVecStatusText = "Сервер не вернул путь к Word2Vec-модели.";
                    MessageBox.Show("Сервер завершил обучение, но не вернул путь к модели.");
                    return;
                }

                var finalModelPath = CopyModelToModelsDirectory(modelPath);

                SelectedWordTwoVecConfig = await AddOrSelectModelAsync(
                    WordTwoVecConfigs,
                    finalModelPath,
                    Algorithm.WordTwoVec,
                    DataType.WordTwoVecConfig,
                    GlobalConfig.WordTwoVecConfigPath);

                WordTwoVecStatusText = $"Word2Vec-модель обучена: {Path.GetFileName(finalModelPath)}";
                MessageBox.Show($"Модель обучена по алгоритму {Algorithm.WordTwoVec} и сохранена в {finalModelPath}");
            }
            catch (Exception ex)
            {
                WordTwoVecStatusText = "Ошибка обучения Word2Vec-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressWordTwoVec = false;
            }
        }, o => !ProgressWordTwoVec);

        public RelayCommand UploadFastTextModelCommand => GetCommand(async o =>
        {
            var currentPath = DialogService.OpenFileDialog(DialogService.ModelFilter);
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                FastTextStatusText = "Загрузка FastText-модели отменена.";
                return;
            }

            ProgressFastText = true;
            FastTextStatusText = "Копирую FastText-модель в папку Models...";

            try
            {
                var finalPath = CopyModelToModelsDirectory(currentPath);
                SelectedFastTextConfig = await AddOrSelectModelAsync(
                    FastTextConfigs,
                    finalPath,
                    Algorithm.FastText,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath);

                FastTextStatusText = $"FastText-модель загружена: {Path.GetFileName(finalPath)}";
            }
            catch (Exception ex)
            {
                FastTextStatusText = "Ошибка загрузки FastText-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressFastText = false;
            }
        }, o => !ProgressFastText);

        public RelayCommand DeleteFastTextModelCommand => GetCommand(async o =>
        {
            if (SelectedFastTextConfig is null)
            {
                return;
            }

            ProgressFastText = true;
            FastTextStatusText = "Удаляю FastText-модель...";

            try
            {
                await DeleteModelAsync(
                    FastTextConfigs,
                    SelectedFastTextConfig,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath);

                SelectedFastTextConfig = FastTextConfigs.FirstOrDefault();
                FastTextStatusText = "FastText-модель удалена.";
            }
            catch (Exception ex)
            {
                FastTextStatusText = "Ошибка удаления FastText-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressFastText = false;
            }
        }, o => SelectedFastTextConfig is not null && !ProgressFastText);

        public RelayCommand UseFastTextModelCommand => GetCommand(async o =>
        {
            if (SelectedFastTextConfig is null)
            {
                return;
            }

            try
            {
                if (!await UseModelAsync(
                    FastTextConfigs,
                    SelectedFastTextConfig,
                    Algorithm.FastText,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath,
                    "FastText"))
                {
                    FastTextStatusText = "Файл FastText-модели не найден.";
                    return;
                }

                FastTextStatusText = $"Используется FastText-модель: {SelectedFastTextConfig.DisplayedName}";
            }
            catch (Exception ex)
            {
                FastTextStatusText = "Ошибка выбора FastText-модели.";
                MessageBox.Show(ex.Message);
            }
        }, o => SelectedFastTextConfig is not null && SelectedFastTextConfig.IsUsed is false && !ProgressFastText);

        public RelayCommand RetrainFastTextModelCommand => GetCommand(async o =>
        {
            if (SelectedFastTextConfig is null)
            {
                return;
            }

            ProgressFastText = true;
            FastTextStatusText = "Проверяю FastText-модель и matcher-сервер...";

            try
            {
                if (!await EnsureMatcherModelReadyAsync(
                    FastTextConfigs,
                    SelectedFastTextConfig,
                    Algorithm.FastText,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath,
                    "FastText"))
                {
                    FastTextStatusText = "Файл FastText-модели не найден.";
                    return;
                }

                if (!await EnsureMatcherForModelOperationAsync(status => FastTextStatusText = status))
                {
                    return;
                }

                var matcherModelPathResult = _apiService.PrepareMatcherModelPath(SelectedFastTextConfig.Path);
                if (!matcherModelPathResult.IsSuccess)
                {
                    FastTextStatusText = "Не удалось подготовить FastText-модель для matcher.";
                    MessageBox.Show(matcherModelPathResult.ErrorMessage);
                    return;
                }

                FastTextStatusText = "Дообучаю FastText-модель...";
                RequestRetrain request = new(
                    "127.0.0.1",
                    GlobalConfig.MatcherPort,
                    matcherModelPathResult.Value,
                    Algorithm.FastText);

                var result = await _apiService.SendRequestAsync<ResponseRetrain>(request);
                if (!result.IsSuccess)
                {
                    FastTextStatusText = "Ошибка дообучения FastText-модели.";
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                var modelPath = result.Value?.ModelPath;
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    FastTextStatusText = "Сервер не вернул путь к дообученной FastText-модели.";
                    MessageBox.Show("Сервер завершил дообучение, но не вернул путь к модели.");
                    return;
                }

                var finalModelPath = CopyModelToModelsDirectory(modelPath);

                SelectedFastTextConfig = await AddOrSelectModelAsync(
                    FastTextConfigs,
                    finalModelPath,
                    Algorithm.FastText,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath);

                FastTextStatusText = $"FastText-модель дообучена: {Path.GetFileName(finalModelPath)}";
            }
            catch (Exception ex)
            {
                FastTextStatusText = "Ошибка дообучения FastText-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressFastText = false;
            }
        }, o => SelectedFastTextConfig is not null && !ProgressFastText);

        public RelayCommand TrainFastTextModelCommand => GetCommand(async o =>
        {
            ProgressFastText = true;
            FastTextStatusText = "Проверяю matcher-сервер...";

            try
            {
                if (!await EnsureMatcherForModelOperationAsync(status => FastTextStatusText = status))
                {
                    return;
                }

                FastTextStatusText = "Обучаю FastText-модель...";
                RequestTrain request = new("127.0.0.1", GlobalConfig.MatcherPort, Algorithm.FastText);
                var result = await _apiService.SendRequestAsync<ResponseTrain>(request);

                if (!result.IsSuccess)
                {
                    FastTextStatusText = "Ошибка обучения FastText-модели.";
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                var modelPath = result.Value?.ModelPath;
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    FastTextStatusText = "Сервер не вернул путь к FastText-модели.";
                    MessageBox.Show("Сервер завершил обучение, но не вернул путь к модели.");
                    return;
                }

                var finalModelPath = CopyModelToModelsDirectory(modelPath);

                SelectedFastTextConfig = await AddOrSelectModelAsync(
                    FastTextConfigs,
                    finalModelPath,
                    Algorithm.FastText,
                    DataType.FastTextConfig,
                    GlobalConfig.FastTextConfigPath);

                FastTextStatusText = $"FastText-модель обучена: {Path.GetFileName(finalModelPath)}";
                MessageBox.Show($"Модель обучена по алгоритму {Algorithm.FastText} и сохранена в {finalModelPath}");
            }
            catch (Exception ex)
            {
                FastTextStatusText = "Ошибка обучения FastText-модели.";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ProgressFastText = false;
            }
        }, o => !ProgressFastText);

        public void NotifySelectedModels()
        {
            var wordTwoVecModel = WordTwoVecConfigs.FirstOrDefault(m => m.Algorithm == Algorithm.WordTwoVec && m.IsUsed);
            var fastTextModel = FastTextConfigs.FirstOrDefault(m => m.Algorithm == Algorithm.FastText && m.IsUsed);

            WeakReferenceMessenger.Default.Send(new FastTextModelSelectedMessage(fastTextModel?.Path));
            WeakReferenceMessenger.Default.Send(new WordTwoVecModelSelectedMessage(wordTwoVecModel?.Path));
        }

        private async Task<bool> EnsureMatcherForModelOperationAsync(Action<string> setStatus)
        {
            setStatus("Проверяю matcher-сервер...");
            var serverResult = await _apiService.EnsureMatcherServerAvailableAsync();

            if (serverResult.IsSuccess)
            {
                setStatus("Matcher-сервер доступен.");
                return true;
            }

            setStatus("Matcher-сервер недоступен.");
            MessageBox.Show(serverResult.ErrorMessage);
            return false;
        }

        private static string CopyModelToModelsDirectory(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Файл модели не найден.", sourcePath);
            }

            if (!IsBinModelPath(sourcePath))
            {
                throw new InvalidOperationException(
                    "Для Word2Vec/FastText можно загружать только .bin модели. GGUF-модели чата хранятся отдельно в папке Translator.");
            }

            Directory.CreateDirectory(GlobalConfig.ModelsPath);

            var finalPath = Path.Combine(GlobalConfig.ModelsPath, Path.GetFileName(sourcePath));
            var sourceFullPath = Path.GetFullPath(sourcePath);
            var finalFullPath = Path.GetFullPath(finalPath);

            if (string.Equals(sourceFullPath, finalFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return finalFullPath;
            }

            File.Copy(sourceFullPath, finalFullPath, overwrite: true);
            return finalFullPath;
        }

        private async Task<ModelConfig> AddOrSelectModelAsync(
            ObservableCollection<ModelConfig> configs,
            string modelPath,
            Algorithm algorithm,
            DataType configDataType,
            string configPath)
        {
            if (!TryResolveBinModelPath(modelPath, out var normalizedPath, out var errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            var existing = FindModelByPath(configs, normalizedPath);

            if (existing is not null)
            {
                await _fileService.SaveDTOAsync(configs, configDataType, configPath);
                return existing;
            }

            var modelConfig = new ModelConfig
            {
                Path = normalizedPath,
                Algorithm = algorithm,
                IsUsed = configs.Count == 0
            };

            configs.Add(modelConfig);
            await _fileService.SaveDTOAsync(configs, configDataType, configPath);
            NotifySelectedModels();

            return modelConfig;
        }

        private async Task DeleteModelAsync(
            ObservableCollection<ModelConfig> configs,
            ModelConfig selectedConfig,
            DataType configDataType,
            string configPath)
        {
            var itemToRemove = FindModelByPath(configs, selectedConfig.Path);
            if (itemToRemove is null)
            {
                return;
            }

            var wasUsed = itemToRemove.IsUsed;

            if (!string.IsNullOrWhiteSpace(itemToRemove.Path) && File.Exists(itemToRemove.Path))
            {
                File.Delete(itemToRemove.Path);
            }

            configs.Remove(itemToRemove);

            if (wasUsed && configs.Count > 0 && !configs.Any(config => config.IsUsed))
            {
                configs[0].IsUsed = true;
            }

            await _fileService.SaveDTOAsync(configs, configDataType, configPath);
            NotifySelectedModels();
        }

        private async Task<bool> UseModelAsync(
            ObservableCollection<ModelConfig> configs,
            ModelConfig selectedConfig,
            Algorithm expectedAlgorithm,
            DataType configDataType,
            string configPath,
            string modelKind)
        {
            if (!await EnsureMatcherModelReadyAsync(
                configs,
                selectedConfig,
                expectedAlgorithm,
                configDataType,
                configPath,
                modelKind))
            {
                return false;
            }

            foreach (var config in configs)
            {
                config.IsUsed = ReferenceEquals(config, selectedConfig);
            }

            await _fileService.SaveDTOAsync(configs, configDataType, configPath);
            NotifySelectedModels();
            return true;
        }

        private async Task<bool> EnsureMatcherModelReadyAsync(
            ObservableCollection<ModelConfig> configs,
            ModelConfig modelConfig,
            Algorithm expectedAlgorithm,
            DataType configDataType,
            string configPath,
            string modelKind)
        {
            if (modelConfig.Algorithm != expectedAlgorithm)
            {
                MessageBox.Show(
                    $"Выбрана модель другого алгоритма. Для {modelKind} нужна отдельная .bin-модель {modelKind}, а не GGUF-модель чата.");
                return false;
            }

            if (!TryResolveBinModelPath(modelConfig.Path, out var resolvedPath, out var errorMessage))
            {
                MessageBox.Show(errorMessage);
                return false;
            }

            if (!string.Equals(modelConfig.Path, resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                modelConfig.Path = resolvedPath;
                await _fileService.SaveDTOAsync(configs, configDataType, configPath);
                NotifySelectedModels();
            }

            return true;
        }

        private static ModelConfig? FindModelByPath(
            IEnumerable<ModelConfig> configs,
            string modelPath)
        {
            var normalizedPath = Path.GetFullPath(modelPath);

            return configs.FirstOrDefault(config =>
                !string.IsNullOrWhiteSpace(config.Path) &&
                string.Equals(
                    Path.GetFullPath(config.Path),
                    normalizedPath,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryResolveBinModelPath(
            string? configuredPath,
            out string resolvedPath,
            out string errorMessage)
        {
            resolvedPath = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                errorMessage = "Путь к .bin-модели пустой. Загрузи модель во вкладке «Работа с моделями».";
                return false;
            }

            var fileName = Path.GetFileName(configuredPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                errorMessage = $"Не удалось определить имя файла модели:{Environment.NewLine}{configuredPath}";
                return false;
            }

            if (string.Equals(Path.GetExtension(fileName), GgufModelExtension, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage =
                    "Дообучение Word2Vec/FastText не работает с GGUF-моделью чата. " +
                    "Выбери .bin-модель из папки Models; GGUF остается отдельно в папке Translator.";
                return false;
            }

            if (!IsBinModelPath(fileName))
            {
                errorMessage =
                    $"Для дообучения Word2Vec/FastText нужен файл .bin, выбран файл:{Environment.NewLine}{configuredPath}";
                return false;
            }

            var candidates = new List<string>();
            AddCandidate(configuredPath, candidates);
            AddCandidate(Path.Combine(GlobalConfig.ModelsPath, fileName), candidates);

            if (Directory.Exists(GlobalConfig.ModelsPath))
            {
                var modelFromModelsDirectory = Directory
                    .EnumerateFiles(GlobalConfig.ModelsPath, "*" + BinModelExtension, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(path => string.Equals(
                        Path.GetFileName(path),
                        fileName,
                        StringComparison.OrdinalIgnoreCase));

                AddCandidate(modelFromModelsDirectory, candidates);
            }

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(candidate))
                {
                    resolvedPath = Path.GetFullPath(candidate);
                    return true;
                }
            }

            errorMessage =
                $"Файл .bin-модели не найден:{Environment.NewLine}{configuredPath}{Environment.NewLine}{Environment.NewLine}" +
                $"Проверь, что файл {fileName} лежит в папке:{Environment.NewLine}{GlobalConfig.ModelsPath}";
            return false;
        }

        private static void AddCandidate(string? path, ICollection<string> candidates)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                candidates.Add(Path.GetFullPath(path));
            }
            catch
            {
                candidates.Add(path);
            }
        }

        private static bool IsBinModelPath(string path)
        {
            return string.Equals(
                Path.GetExtension(path),
                BinModelExtension,
                StringComparison.OrdinalIgnoreCase);
        }

        private void OnModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ModelConfig model in e.NewItems)
                {
                    model.PropertyChanged += OnModelPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ModelConfig model in e.OldItems)
                {
                    model.PropertyChanged -= OnModelPropertyChanged;
                }
            }

            RefreshModelAvailabilityStatus();
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelConfig.IsUsed))
            {
                NotifySelectedModels();
            }
        }

        private void RefreshModelAvailabilityStatus()
        {
            NotifyPropertyChanged(nameof(HasWordTwoVecModels));
            NotifyPropertyChanged(nameof(HasFastTextModels));

            if (!ProgressWordTwoVec)
            {
                WordTwoVecStatusText = HasWordTwoVecModels
                    ? $"Word2Vec: моделей в списке {WordTwoVecConfigs.Count}."
                    : "Word2Vec: моделей нет. Нажми «Загрузить модель» или «Обучить модель».";
            }

            if (!ProgressFastText)
            {
                FastTextStatusText = HasFastTextModels
                    ? $"FastText: моделей в списке {FastTextConfigs.Count}."
                    : "FastText: моделей нет. Нажми «Загрузить модель» или «Обучить модель».";
            }
        }
    }

}
