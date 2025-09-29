using LiveChartsCore.SkiaSharpView.Maui;
using ScottPlot.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace TRION_SDK_UI.WinUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp()
                .UseLiveCharts()
                .UseSkiaSharp()
                .UseScottPlot();

            return builder.Build();
        }
    }
}
