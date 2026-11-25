using System.Windows;

namespace AdmissionCommittee.Services;

/// <summary>
/// Окна сообщений. ViewModel работает с интерфейсом и не вызывает MessageBox напрямую —
/// так логику можно тестировать без окон.
/// </summary>
public interface IDialogService
{
    void ShowInfo(string message);
    void ShowError(string message);
    bool Confirm(string message);
}

public class DialogService : IDialogService
{
    private const string Caption = "Приёмная комиссия";

    public void ShowInfo(string message)
        => MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message)
        => MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message)
        => MessageBox.Show(message, Caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
