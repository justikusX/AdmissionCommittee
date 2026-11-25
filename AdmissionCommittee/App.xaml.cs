using System.Windows;
using AdmissionCommittee.Data;
using AdmissionCommittee.Infrastructure;
using AdmissionCommittee.Services;
using AdmissionCommittee.ViewModels;

namespace AdmissionCommittee;

/// <summary>
/// Точка входа. Здесь создаются сервисы, главное окно и его ViewModel.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dialogs = new DialogService();

        // Все ошибки команд (в т.ч. ошибки БД) показываем понятным сообщением
        AsyncRelayCommand.ErrorHandler = ex => dialogs.ShowError(DbErrors.ToUserMessage(ex));
        DispatcherUnhandledException += (_, args) =>
        {
            dialogs.ShowError("Непредвиденная ошибка: " + DbErrors.ToUserMessage(args.Exception));
            args.Handled = true;
        };

        AppServices services;
        try
        {
            services = new AppServices(AppConfig.LoadConnectionString(), dialogs);
        }
        catch (Exception ex)
        {
            dialogs.ShowError("Не удалось запустить приложение:\n" + ex.Message);
            Shutdown();
            return;
        }

        var window = new MainWindow { DataContext = new MainViewModel(services) };
        MainWindow = window;
        window.Show();
    }
}
