using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;

namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса инициализирующего приложение
    /// </summary>
    public interface IInfrastructureOrchestrator
    {
        ThreatViewModel ThreatVm { get; }
        VulnerabilitieViewModel VulnerabilitieVm { get; }
        ExploitViewModel ExploitVm { get; }
        OntologyViewModel OntologyVm { get; }
        TacticViewModel TacticVm { get; }
        ViolatorViewModel ViolatorVm { get; }
        ProtectionMeasureViewModel ProtectionMeasureVm { get; }
        SpecialistViewModel SpecialistVm { get; }
        ReferenceStatusViewModel ReferenceStatusVm { get; }
        CurrentStatusViewModel CurrentStatusVm { get; }
        OutcomesViewModel OutcomeVm { get; }
        SetViewModel SetVm { get; }
        ViewerViewModel ViewerVm { get; }
        ConnectionViewModel ConnectionVm { get; }
        InformationViewModel InformationVm { get; }
        CreatorViewModel CreatorVm { get; }

        Task InitializeAsync();
        Task CompletionWorkAsync();
    }
}