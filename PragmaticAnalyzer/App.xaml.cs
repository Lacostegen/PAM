using PragmaticAnalyzer.MVVM.Views.Main;
using PragmaticAnalyzer.Services;
using System.Windows;

namespace PragmaticAnalyzer
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            var infrastructureOrchestrator = new InfrastructureOrchestrator(); // создания экземпляра сервиса InfrastructureOrchestrator
            var mainVm = infrastructureOrchestrator.MainVm; // создания экземпляра MainVm

            var mainView = new MainView(mainVm); // создание экземпляра MainView
            mainView.Show();

            try
            {
                await infrastructureOrchestrator.InitializeAsync(); // запуск метода инициализации приложения
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }

            base.OnStartup(e);
        }
    }
}