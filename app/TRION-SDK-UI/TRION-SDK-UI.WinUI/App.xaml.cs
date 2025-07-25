using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop; // For WindowNative
using Microsoft.UI; // For WindowId
using System.Linq; // For FirstOrDefault

namespace TRION_SDK_UI.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // Get the first MAUI window and its native WinUI window
            var mauiWindow = Microsoft.Maui.Controls.Application.Current.Windows.FirstOrDefault();
            var nativeWindow = mauiWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

            if (nativeWindow is not null)
            {
                var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                // Set your desired window size here
                appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 800 });
            }
        }
    }
}
