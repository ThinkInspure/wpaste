using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;

namespace WPaste.Windows.Ui;

internal static class TrayCommandFactory
{
    public static XamlUICommand Create(DispatcherQueue dispatcher, Action action)
    {
        var command = new XamlUICommand();
        command.ExecuteRequested += (_, _) =>
            dispatcher.TryEnqueue(new DispatcherQueueHandler(() => action()));
        return command;
    }
}
