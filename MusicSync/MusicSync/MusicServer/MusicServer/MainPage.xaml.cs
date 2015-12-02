using MusicSubscriber;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicServer
{
    public sealed partial class MainPage : Page
    {
        private StorageFile _mediaFile;
        private Publisher _publisher;
        private Subscriber _subscriber;
        private bool _playing;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_LoadedAsync;
        }

        private async void MainPage_LoadedAsync(object sender, RoutedEventArgs e)
        {
            //get the current IP address of this machine and display it
            HostIPAddressTextBlock.Text = GetThisIPAddress();
            //set up as a publisher and bind the UI to the list of virtual speakers
            _publisher = new Publisher();
            MessageLogListView.ItemsSource = _publisher.Speakers;
            bool init = await _publisher.Initialize();

            if (init)
            {
                //allow a new media file to be selected
                SelectMediaFileButton.IsEnabled = true;
                // create a virtual speaker for this local machine to listen to the music we send
                _subscriber = new Subscriber();
                //hardcoded to localhost and name for local speaker
                _subscriber.ConnectAsync("localhost", "Host Speaker");
            }
            else
            {
                Debug.WriteLine("Error: Publisher failed to initialize");
            }
            // listen for changes in the collection of virtual speakers
            _publisher.Speakers.CollectionChanged += Speakers_CollectionChanged;
        }

        private void Speakers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // if the virtual speakers all have received the media file and are ready to play
            if (_publisher.CheckSpeakersAreReady())
            {
                //Allow media file to be played if all speakers are ready
                PlayButton.IsEnabled = true;
            }
            else
            {
                PlayButton.IsEnabled = false;
            }
        }

        private async void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            // load filepicker for user to select a media file
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeFilter.Add(".mp4");
            filePicker.FileTypeFilter.Add(".MOV");
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            _mediaFile = await filePicker.PickSingleFileAsync();

            if (_mediaFile != null)
            {
                Debug.WriteLine(_mediaFile.DisplayName);
                SendButton.IsEnabled = true;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // read the file and send it via the publisher 
            using (IRandomAccessStream stream = await _mediaFile.OpenReadAsync())
            {
                byte[] fileBytes = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
                _publisher.Send(fileBytes);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            //toggle the state between play and stop
            if (!_playing)
            {
                _publisher.ToggleMediaState(MessageType.Play);
                PlayButton.Content = "Stop";
            }
            else
            {
                _publisher.ToggleMediaState(MessageType.Stop);
                PlayButton.Content = "Play";
            }
            //toggle the playing flag
            _playing = !_playing;
        }

        private string GetThisIPAddress()
        {
            string lastHostName = string.Empty;
            IReadOnlyList<HostName> hosts = NetworkInformation.GetHostNames();
            foreach (HostName host in hosts)
            {
                // The last IPv4 host name is always this computer.
                if (host.Type == HostNameType.Ipv4)
                {
                    lastHostName = host.DisplayName;
                }
            }

            return lastHostName;
        }
    }
}
