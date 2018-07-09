using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleChatRoomClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private String m_user_name;

        private ClientAgent m_agent = null;

        public MainWindow()
        {
            InitializeComponent();
            m_agent = new ClientAgent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window1 popup_window = new Window1();
            if(popup_window.ShowDialog() == true)
            {
                m_user_name = popup_window.UserName;
                String ipaddress = popup_window.IPAddress;
                String portnum = popup_window.PortNumber;
                if(0 == m_agent.Connect(ipaddress, portnum))
                {
                    MenuLabel.Content = $"{m_user_name} is connected to {ipaddress}: {portnum}";
                }
            }
        }
    }
}
