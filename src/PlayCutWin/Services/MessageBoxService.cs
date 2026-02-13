using System.Windows;

namespace PlayCutWin.Services;

public interface IMessageBoxService
{
    void ShowError(string message, string title = "Error");
    void ShowInfo(string message, string title = "Info");
}

public sealed class MessageBoxService : IMessageBoxService
{
    public void ShowError(string message, string title = "Error")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title = "Info")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
