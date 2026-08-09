using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.Abstractions
{
    public interface IInfrastructureOrchestrator
    {
        CommunicationViewModel CommunicationVm { get; }

        Task CompletionWorkAsync();
    }
}
