using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WhackerLinkConsoleV2.Controls
{
    public partial class ChannelBox : UserControl
    {
        public string ChannelName { get; set; }
        public string SystemName { get; set; }
        public string TGID { get; set; }

        public ChannelBox()
        {
            InitializeComponent();
            DataContext = this;

            MouseMove += ChannelBox_MouseMove;
            MouseDown += ChannelBox_MouseDown;
            MouseUp += ChannelBox_MouseUp;
        }

        public ChannelBox(string channelName, string systemName, string tgid) : this()
        {
            ChannelName = $"{channelName}";
            SystemName = $"System: {systemName}";
            TGID = $"TGID: {tgid}";
        }

        private Point _dragStartPoint;
        private bool _isDragging;

        private void ChannelBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private void ChannelBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(null);

                if (!_isDragging && (Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                     Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    _isDragging = true;
                    DataObject data = new DataObject("ChannelBox", this);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
            }
        }

        private void ChannelBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void PTTButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Imagine you were talking on {ChannelName} rn");
        }
    }
}
