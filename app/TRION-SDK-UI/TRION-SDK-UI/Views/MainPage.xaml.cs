using System.Collections.ObjectModel;

namespace TRION_SDK_UI
{
    public partial class MainPage : ContentPage
    {
        double _startWidth;

        public MainPage()
        {
            InitializeComponent();

            // Set up the sample data
            double[] dataX = { 1, 2, 3, 4, 5 };
            double[] dataY = { 1, 4, 9, 16, 25 };
            
            // Set the BindingContext
            BindingContext = new MainViewModel();
            
            // Add the plot data AFTER setting BindingContext
            MauiPlot1.Plot.Clear(); // Clear any previous data
            MauiPlot1.Plot.Add.Scatter(dataX, dataY);
            MauiPlot1.Plot.Title("Sample Data");
            MauiPlot1.Plot.XLabel("X Values");
            MauiPlot1.Plot.YLabel("Y Values");
            MauiPlot1.Refresh();
            
            // Rest of your initialization
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
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
            //LogMessages.Add($"Button clicked at {DateTime.Now:T}");
        }
    }
}