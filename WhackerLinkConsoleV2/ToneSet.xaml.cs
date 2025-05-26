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
            //if (ToneSetSelectBtn != null)
            {
                //ToneSetSelectBtn.Content = _isSelected ? "Deselect" : "Select";
                //ToneSetSelectBtn.Background = _isSelected ? Brushes.LightGreen : Brushes.LightGray;
            }
        }
    }
}
