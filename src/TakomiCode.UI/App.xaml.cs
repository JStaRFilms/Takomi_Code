using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TakomiCode.UI;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register MVVM ViewModels
                services.AddSingleton<ViewModels.MainViewModel>();

                // Register Infrastructure and Domain Services
                services.AddSingleton<TakomiCode.Application.Contracts.Persistence.IAuditLogRepository, TakomiCode.Infrastructure.Persistence.LocalAuditLogRepository>();
                services.AddSingleton<TakomiCode.Application.Contracts.Persistence.IChatSessionRepository, TakomiCode.Infrastructure.Persistence.LocalChatSessionRepository>();
                services.AddSingleton<TakomiCode.Application.Contracts.Persistence.IOrchestrationRepository, TakomiCode.Infrastructure.Persistence.LocalOrchestrationRepository>();
                services.AddSingleton<TakomiCode.Application.Contracts.Persistence.IWorkspaceRepository, TakomiCode.Infrastructure.Persistence.LocalWorkspaceRepository>();
                services.AddSingleton<TakomiCode.Application.Contracts.Runtime.ICodexRuntimeAdapter, TakomiCode.RuntimeAdapters.Codex.CodexCliAdapter>();
                services.AddSingleton<TakomiCode.Application.Contracts.Runtime.ITakomiConfigurationLoader, TakomiCode.Infrastructure.Runtime.TakomiConfigurationLoader>();
                services.AddSingleton<TakomiCode.Application.Contracts.Services.IOrchestratorExecutionEngine, TakomiCode.Application.Services.OrchestratorExecutionEngine>();
                services.AddSingleton<TakomiCode.Application.Contracts.Services.IInterventionCommandHandler, TakomiCode.Application.Services.InterventionCommandHandler>();
                services.AddSingleton<TakomiCode.Application.Contracts.Services.IGitService, TakomiCode.Infrastructure.Services.GitService>();
                services.AddSingleton<TakomiCode.Application.Contracts.Services.IBillingService, TakomiCode.Infrastructure.Services.PaystackMockBillingService>();
            })
            .Build();

        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;
}
