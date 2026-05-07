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
        public ObservableCollection<ModelConfig> WordTwoVecConfigs { get; set; }
        public ObservableCollection<ModelConfig> FastTextConfigs { get; set; }
        public ModelConfig SelectedWordTwoVecConfig { get => Get<ModelConfig>(); set => Set(value); }
        public ModelConfig SelectedFastTextConfig { get => Get<ModelConfig>(); set => Set(value); }
        public bool ProgressWordTwoVec { get => Get<bool>(); set => Set(value); }
        public bool ProgressFastText { get => Get<bool>(); set => Set(value); }

        public SettingViewModel(ObservableCollection<ModelConfig> wordTwoVecConfig, ObservableCollection<ModelConfig> fastTextVecConfig, IApiService apiService)
        {
            WordTwoVecConfigs = wordTwoVecConfig;
            FastTextConfigs = fastTextVecConfig;
            _apiService = apiService;
            _fileService = new FileService();
            ProgressWordTwoVec = false;
            ProgressFastText = false;

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
            if (currentPath is null)
            {
                return;
            }
            var finalPath = Path.Combine(GlobalConfig.ModelsPath, Path.GetFileName(currentPath));
            if (finalPath == currentPath)
            {
                return;
            }
            try
            {
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(currentPath, finalPath);

                WordTwoVecConfigs.Add(new()
                {
                    Path = finalPath,
                    Algorithm = Algorithm.WordTwoVec,
                    IsUsed = WordTwoVecConfigs.Count is 0
                });
                await _fileService.SaveDTOAsync(WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
            }
            catch (Exception ex)
            {
            }
        });

        public RelayCommand DeleteWordTwoVecModelCommand => GetCommand(async o =>
        {
            if (SelectedWordTwoVecConfig is null) return;
            if (File.Exists(SelectedWordTwoVecConfig.Path))
            {
                File.Delete(SelectedWordTwoVecConfig.Path);
            }

            var itemToRemove = WordTwoVecConfigs.FirstOrDefault(x => x.Path == SelectedWordTwoVecConfig.Path);
            if (itemToRemove is not null)
            {
                if (SelectedWordTwoVecConfig.IsUsed && WordTwoVecConfigs.Count is not 0)
                {
                    WordTwoVecConfigs.First().IsUsed = true;
                }
                WordTwoVecConfigs.Remove(itemToRemove);
            }
            await _fileService.SaveDTOAsync(WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);

        }, o => SelectedWordTwoVecConfig is not null);

        public RelayCommand UseWordTwoVecModelCommand => GetCommand(async o =>
        {
            if (SelectedWordTwoVecConfig is not null)
            {
                foreach (var config in WordTwoVecConfigs)
                {
                    config.IsUsed = config == SelectedWordTwoVecConfig;
                }
                await _fileService.SaveDTOAsync(WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
            }
        }, o => SelectedWordTwoVecConfig is not null && SelectedWordTwoVecConfig.IsUsed is false);

        public RelayCommand RetrainWordTwoVecModelCommand => GetCommand(async o =>
        {
            ProgressWordTwoVec = true;

            RequestRetrain request = new("127.0.0.1", "5000", SelectedWordTwoVecConfig.Path, Algorithm.WordTwoVec);
            var result = await _apiService.SendRequestAsync<ResponseRetrain>(request);

            if (result.IsSuccess)
            {
                WordTwoVecConfigs.Add(new()
                {
                    Path = result.Value.ModelPath,
                    Algorithm = Algorithm.WordTwoVec,
                    IsUsed = WordTwoVecConfigs.Count is 0
                });
                await _fileService.SaveDTOAsync(WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
            }
            else
            {
                ProgressWordTwoVec = false;
                MessageBox.Show(result.ErrorMessage);
            }

            ProgressWordTwoVec = false;
        }, o => SelectedWordTwoVecConfig is not null);

        public RelayCommand TrainWordTwoVecModelCommand => GetCommand(async o =>
        {
            ProgressWordTwoVec = true;

            RequestTrain request = new("127.0.0.1", "5000", Algorithm.WordTwoVec);
            var result = await _apiService.SendRequestAsync<ResponseTrain>(request);

            if (result.IsSuccess)
            {
                WordTwoVecConfigs.Add(new()
                {
                    Path = result.Value.ModelPath,
                    Algorithm = Algorithm.WordTwoVec,
                    IsUsed = WordTwoVecConfigs.Count is 0
                });
                await _fileService.SaveDTOAsync(WordTwoVecConfigs, DataType.WordTwoVecConfig, GlobalConfig.WordTwoVecConfigPath);
                MessageBox.Show($"Модель обучена по алгоритму {Algorithm.WordTwoVec} и сохранена в {result.Value.ModelPath}");
            }
            else
            {
                ProgressWordTwoVec = false;
                MessageBox.Show(result.ErrorMessage);
            }

            ProgressWordTwoVec = false;
        });

        public RelayCommand UploadFastTextModelCommand => GetCommand(async o =>
        {
            var currentPath = DialogService.OpenFileDialog(DialogService.ModelFilter);
            if (currentPath is null)
            {
                return;
            }

            try
            {
                var finalPath = Path.Combine(GlobalConfig.ModelsPath, Path.GetFileName(currentPath));
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(currentPath, finalPath);

                var isUsed = false;
                if (FastTextConfigs.Count is 0)
                {
                    isUsed = true;
                }
                FastTextConfigs.Add(new()
                {
                    Path = finalPath,
                    Algorithm = Algorithm.FastText,
                    IsUsed = isUsed
                });
                await _fileService.SaveDTOAsync(FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
            }
            catch (Exception ex)
            {
            }
        });

        public RelayCommand DeleteFastTextModelCommand => GetCommand(async o =>
        {
            if (SelectedFastTextConfig is null) return;
            if (File.Exists(SelectedFastTextConfig.Path))
            {
                File.Delete(SelectedFastTextConfig.Path);
            }

            var itemToRemove = FastTextConfigs.FirstOrDefault(x => x.Path == SelectedFastTextConfig.Path);
            if (itemToRemove is not null)
            {
                if (SelectedFastTextConfig.IsUsed && FastTextConfigs.Count is not 0)
                {
                    FastTextConfigs.First().IsUsed = true;
                }
                FastTextConfigs.Remove(itemToRemove);
            }

            await _fileService.SaveDTOAsync(FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);

        }, o => SelectedFastTextConfig is not null);

        public RelayCommand UseFastTextModelCommand => GetCommand(async o =>
        {
            if (SelectedFastTextConfig is not null)
            {
                foreach (var config in FastTextConfigs)
                {
                    config.IsUsed = config == SelectedFastTextConfig;
                }
                await _fileService.SaveDTOAsync(FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
            }
        }, o => SelectedFastTextConfig is not null && SelectedFastTextConfig.IsUsed is false);

        public RelayCommand RetrainFastTextModelCommand => GetCommand(async o =>
        {
            ProgressFastText = true;

            RequestRetrain request = new("127.0.0.1", "5000", SelectedFastTextConfig.Path, Algorithm.FastText);
            var result = await _apiService.SendRequestAsync<ResponseRetrain>(request);

            if (result.IsSuccess)
            {
                FastTextConfigs.Add(new()
                {
                    Path = result.Value.ModelPath,
                    Algorithm = Algorithm.FastText,
                    IsUsed = FastTextConfigs.Count is 0
                });
                await _fileService.SaveDTOAsync(FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
            }
            else
            {
                ProgressFastText = false;
                MessageBox.Show(result.ErrorMessage);
            }

            ProgressFastText = false;
        }, o => SelectedFastTextConfig is not null);

        public RelayCommand TrainFastTextModelCommand => GetCommand(async o =>
        {
            ProgressFastText = true;

            RequestTrain request = new("127.0.0.1", "5000", Algorithm.FastText);
            var result = await _apiService.SendRequestAsync<ResponseTrain>(request);

            if (result.IsSuccess)
            {
                FastTextConfigs.Add(new()
                {
                    Path = result.Value.ModelPath,
                    Algorithm = Algorithm.FastText,
                    IsUsed = FastTextConfigs.Count is 0
                });
                await _fileService.SaveDTOAsync(FastTextConfigs, DataType.FastTextConfig, GlobalConfig.FastTextConfigPath);
                MessageBox.Show($"Модель обучена по алгоритму {Algorithm.FastText} и сохранена в {result.Value.ModelPath}");
            }
            else
            {
                ProgressFastText = false;
                MessageBox.Show(result.ErrorMessage);
            }

            ProgressFastText = false;
        });

        public void NotifySelectedModels()
        {
            var wordTwoVecModel = WordTwoVecConfigs.FirstOrDefault(m => m.Algorithm == Algorithm.WordTwoVec && m.IsUsed);
            var fastTextModel = FastTextConfigs.FirstOrDefault(m => m.Algorithm == Algorithm.FastText && m.IsUsed);

            WeakReferenceMessenger.Default.Send(new FastTextModelSelectedMessage(fastTextModel?.Path));
            WeakReferenceMessenger.Default.Send(new WordTwoVecModelSelectedMessage(wordTwoVecModel?.Path));
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
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelConfig.IsUsed))
            {
                NotifySelectedModels();
            }
        }
    }
}