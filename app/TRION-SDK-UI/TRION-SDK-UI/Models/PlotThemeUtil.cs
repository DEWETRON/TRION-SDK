using ScottPlot;
using ScottPlot.Plottables;

namespace TRION_SDK_UI.Models;

public static class PlotThemeUtil
{
    public static void ApplyTheme(Plot plot, AppTheme theme, Crosshair? crosshair = null, VerticalLine? lockLine = null)
    {
        IPalette palette = new ScottPlot.Palettes.Dark();
        // Base palette
        ScottPlot.Color background;
        ScottPlot.Color fontColor;
        ScottPlot.Color crosshairColor;
        ScottPlot.Color gridColor;

        if (theme == AppTheme.Light)
        {
            fontColor = ScottPlot.Colors.Black;
            crosshairColor = ScottPlot.Colors.Magenta;
            background = ScottPlot.Colors.White;
            gridColor = ScottPlot.Colors.Gray;
        }
        else if (theme == AppTheme.Dark)
        {
            fontColor = ScottPlot.Colors.White;
            crosshairColor = ScottPlot.Colors.Magenta;
            background = ScottPlot.Colors.Black;
            gridColor = ScottPlot.Colors.Gray;
        }
        else // System or default
        {
            fontColor = palette.GetColor(0);
            background = palette.GetColor(1);
            crosshairColor = palette.GetColor(2);
            gridColor = palette.GetColor(3);
        }

        // Figure & data area
        plot.FigureBackground.Color = background;
        plot.DataBackground.Color = background.Darken(0.1);

        // Grid
        plot.Grid.LineColor = gridColor;
        plot.Grid.MinorLineColor = gridColor.WithAlpha(.4);

        // Axes styling
        plot.Axes.Color(fontColor);

        // Legend
        if (plot.Legend is not null)
        {
            plot.Legend.BackgroundColor = fontColor.WithAlpha(.8);
            plot.Legend.FontColor = background;
            plot.Legend.OutlineColor = background;
        }

        if (crosshair is not null)
        {
            crosshair.LineWidth = 2;
            crosshair.LineColor = crosshairColor;
        }

        if (lockLine is not null)
        {
            lockLine.LineWidth = 2;
            lockLine.LineStyle.Color = crosshairColor;
        }
    }
}