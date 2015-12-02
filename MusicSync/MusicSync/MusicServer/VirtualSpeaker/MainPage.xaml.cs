using MusicSubscriber;
using System.Collections.Generic;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VirtualSpeaker
{
    public sealed partial class MainPage : Page
    {
        private Subscriber _subscriber;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            HostIPAddressTextBlock.Text = GetThisIPAddress();
            _subscriber = new Subscriber();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string host = HostNameTextBox.Text;
            string name = SpeakerNameTextBox.Text;
            _subscriber.ConnectAsync(host, name);
        }

        private string GetThisIPAddress()
        {
            string lastHostName = string.Empty;
            IReadOnlyList<HostName> hosts = NetworkInformation.GetHostNames();
            foreach (HostName host in hosts)
            {
                // The last host name is always this computer.
                if (host.Type == HostNameType.Ipv4)
                {
                    lastHostName = host.DisplayName;
                }
            }

            return lastHostName;
        }        
    }
}
