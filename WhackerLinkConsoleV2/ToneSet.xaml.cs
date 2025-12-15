using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WhackerLinkConsoleV2.Controls
{
    public partial class ToneSet : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal LinearGradientBrush grayGradient;
        internal LinearGradientBrush orangeGradient;

        public string ToneName { get; set; }
        public double ToneA { get; set; }
        public double ToneB { get; set; }

        public bool IsEditMode { get; set; }

        public event EventHandler PlayClicked;
        public event EventHandler SelectToggled;

        private bool _isSelected = false;

        public ToneSet(string toneName, double toneA, double toneB)
        {
            InitializeComponent();
            UpdateBackground();

            grayGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };

            grayGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#888888"), 0.485));
            grayGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#888888"), 0.517));

            orangeGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };

            orangeGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFAF00"), 0.485));
            orangeGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFEEA400"), 0.517));
            ToneSetSelectBtn.Background = grayGradient;

            ToneName = toneName;
            ToneA = toneA;
            ToneB = toneB;

            MouseLeftButtonDown += ToneSet_MouseLeftButtonDown;

            DataContext = this;
        }

        private void ToneSet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEditMode) return;
        }


        private void ToneSetPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ToneSetSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectToggled?.Invoke(this, EventArgs.Empty);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateSelectButton();
            if (selected)
                this.Background = System.Windows.Media.Brushes.LightBlue;
            else
                this.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void UpdateSelectButton()
        {
            if (ToneSetSelectBtn != null)
            {
                //ToneSetSelectBtn.Content = _isSelected ? "Deselect" : "Select";
                ToneSetSelectBtn.Background = _isSelected ? orangeGradient : grayGradient;
            }
        }

        private void UpdateBackground()
        {
            Background = _isSelected ? (Brush)new BrushConverter().ConvertFrom("#FF0B004B") : Brushes.DarkGray;
        }
    }
}
