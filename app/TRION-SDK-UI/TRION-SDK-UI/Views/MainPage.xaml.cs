using ScottPlot;
using ScottPlot.Plottables;
using System.Collections.Specialized;
using System.ComponentModel;
using TRION_SDK_UI.Models;
using TRION_SDK_UI.POCO;
using TRION_SDK_UI.ViewModels;

namespace TRION_SDK_UI
{
    public partial class MainPage : ContentPage
    {
        private readonly RecorderGraph _recorder;

        void OnPointerEntered(object sender, PointerEventArgs e)
        {
            if (_recorder.IsScrollLocked)
            {
                if (_recorder.Crosshair is not null && _recorder.LockLine is not null)
                {
                    _recorder.LockLine.IsVisible = true;
                    _recorder.Crosshair.IsVisible = false;
                }
            }
            else
            {
                if (_recorder.Crosshair is not null && _recorder.LockLine is not null)
                {

                    _recorder.Crosshair.IsVisible = true;
                    _recorder.LockLine.IsVisible = false;
                }
            }

            CursorLabel.IsVisible = true;
            MauiPlot1.Refresh();
        }

        void OnPointerExited(object sender, PointerEventArgs e)
        {
            if (_recorder.Crosshair is not null && _recorder.LockLine is not null)
            {
                _recorder.Crosshair.IsVisible = false;
                _recorder.LockLine.IsVisible = false;
                CursorLabel.IsVisible = false;
                MauiPlot1.Refresh();
            }
        }

        void OnPointerMoved(object sender, PointerEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _recorder.UpdatePointer(e);
            });
        }

        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainViewModel();
            var vm = (MainViewModel)BindingContext;
            vm.AcquisitionStarting += VmOnAcquisitionStarted;
            vm.SamplesBatchAppended += VmOnSamplesBatchAppended;
            vm.LogMessages.CollectionChanged += VmLogMessagesCollectionChanged;
            vm.PropertyChanged += VmOnPropertyChanged;

            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Elapsed Seconds");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Plot.Axes.Hairline(true);

            _recorder = new RecorderGraph(MauiPlot1, CursorLabel, CursorLabelText)
            {
                LockLine = MauiPlot1.Plot.Add.VerticalLine(0),
                Crosshair = MauiPlot1.Plot.Add.Crosshair(0, 0)
            };
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);

            if (Application.Current is null) return;
            PlotThemeUtil.ApplyTheme(MauiPlot1.Plot, Application.Current.RequestedTheme, _recorder.Crosshair, _recorder.LockLine);

            Application.Current.RequestedThemeChanged += (s, a) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PlotThemeUtil.ApplyTheme(MauiPlot1.Plot, Application.Current.RequestedTheme, _recorder.Crosshair, _recorder.LockLine);

                    MauiPlot1.Refresh();
                });
            };
        }

        private void VmOnSamplesBatchAppended(object? sender, MainViewModel.SamplesBatchAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var (channelKey, samples) in e.Batches)
                {
                    if (samples is { Length: > 0 })
                    {
                        _recorder.AddSamples(samples, channelKey, vm.FollowLatest);
                    }
                }

                MauiPlot1.Refresh();

                if (_recorder.IsScrollLocked && _recorder.LockLine.IsVisible)
                {
                    _recorder.UpdateValuesAtLockLine();
                }
            });
        }

        private void VmLogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 })
            {
                var last = e.NewItems[^1];
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogView?.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                });
            }
        }

        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            if (Application.Current is null)
            {
                return;
            }

            var keys = channels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToHashSet();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _recorder.StartAcquisition(keys);
            });
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.FollowLatest))
            {
                return;
            }
            if (Application.Current is null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                PlotThemeUtil.ApplyTheme(MauiPlot1.Plot, Application.Current.RequestedTheme, _recorder.Crosshair, _recorder.LockLine);
                var vm = (MainViewModel)BindingContext;
                _recorder.IsScrollLocked = !vm.FollowLatest;

                _recorder.LockLine.IsVisible = _recorder.IsScrollLocked && CursorLabel.IsVisible;
                _recorder.Crosshair.IsVisible = !_recorder.IsScrollLocked && CursorLabel.IsVisible;

                if (_recorder.IsScrollLocked)
                {
                    _recorder.LockLine.X = _recorder.HasCursor ? _recorder.LastCursorCoordinates.X : _recorder.Crosshair.X;
                }

                MauiPlot1.Refresh();

                if (_recorder.IsScrollLocked)
                {
                    _recorder.UpdateValuesAtLockLine();
                }
            });
        }
        private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _recorder.StartWidth = SidebarColumn.Width.Value;
                    break;
                case GestureStatus.Running:
                    double newWidth = _recorder.StartWidth - e.TotalX;
                    if (newWidth < 100) newWidth = 100;
                    if (newWidth > 400) newWidth = 400;
                    SidebarColumn.Width = new GridLength(newWidth);
                    break;
            }
        }
    }
}