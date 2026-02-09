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
            _recorder.SetLockCrossVisibility();
        }

        void OnPointerExited(object sender, PointerEventArgs e)
        {
            _recorder.HideLockCross();
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

            _recorder = new RecorderGraph(MauiPlot1, 
                                          CursorLabel, 
                                          CursorLabelText,
                                          MauiPlot1.Plot.Add.VerticalLine(0),
                                          MauiPlot1.Plot.Add.Crosshair(0, 0));

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);

            if (Application.Current is null) return;
            _recorder.ApplyTheme();

            Application.Current.RequestedThemeChanged += (s, a) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _recorder.ApplyTheme();
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

                _recorder.UpdateValuesAtLockLine();
                MauiPlot1.Refresh();

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
                _recorder.ApplyTheme();
                var vm = (MainViewModel)BindingContext;

                _recorder.IsScrollLocked = !vm.FollowLatest;
                _recorder.SetLockCrossVisibility();
                _recorder.UpdateValuesAtLockLine();
                MauiPlot1.Refresh();

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