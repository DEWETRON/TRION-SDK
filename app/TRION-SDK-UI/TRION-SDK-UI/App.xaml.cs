namespace TRION_SDK_UI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            window.Width = 1200;
            window.Height = 800;
            return window;
        }
    }
}
