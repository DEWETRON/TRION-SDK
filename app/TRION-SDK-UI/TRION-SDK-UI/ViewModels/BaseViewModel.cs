using System.ComponentModel;
using System.Runtime.CompilerServices;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected static Task ShowAlertAsync(string title, string message, string ok = "OK")
    {
        return MainThread.InvokeOnMainThreadAsync(() => (Application.Current?.MainPage?.DisplayAlert(title, message, ok)) ?? Task.CompletedTask);
    }
}