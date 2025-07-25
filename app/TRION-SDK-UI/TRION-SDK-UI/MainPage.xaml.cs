using System.Collections.ObjectModel;

namespace TRION_SDK_UI
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<string> ChannelNames { get; } = new();
        public ObservableCollection<string> LogMessages { get; } = new();

        double _startX;
        double _startWidth;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            ChannelNames.Add("Channel 1");
            ChannelNames.Add("Channel 2");
            ChannelNames.Add("Channel 3");

            LogMessages.Add("App started.");

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        private void OnDragHandlePanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startWidth = SidebarColumn.Width.Value;
                    break;
                case GestureStatus.Running:
                    double newWidth = _startWidth - e.TotalX;
                    if (newWidth < 100) newWidth = 100;
                    if (newWidth > 400) newWidth = 400;
                    SidebarColumn.Width = new GridLength(newWidth);
                    break;
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            LogMessages.Add($"Button clicked at {DateTime.Now:T}");
        }
    }
}