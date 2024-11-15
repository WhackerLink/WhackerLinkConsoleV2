using System.Windows.Controls;

namespace WhackerLinkConsoleV2.Controls
{
    public partial class SystemStatusBox : UserControl
    {
        public string SystemName { get; set; }
        public string AddressPort { get; set; }
        public string ConnectionState { get; set; } = "Disconnected";

        public SystemStatusBox()
        {
            InitializeComponent();
            DataContext = this;
        }

        public SystemStatusBox(string systemName, string address, int port) : this()
        {
            SystemName = systemName;
            AddressPort = $"Address: {address}:{port}";
        }
    }
}
