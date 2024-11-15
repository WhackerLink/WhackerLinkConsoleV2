using System.Windows;

namespace WhackerLinkConsoleV2
{
    public partial class WidgetSelectionWindow : Window
    {
        public bool ShowSystemStatus { get; private set; } = true;
        public bool ShowChannels { get; private set; } = true;

        public WidgetSelectionWindow()
        {
            InitializeComponent();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSystemStatus = SystemStatusCheckBox.IsChecked ?? false;
            ShowChannels = ChannelCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }
    }
}
