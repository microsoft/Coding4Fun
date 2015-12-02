using MusicSubscriber;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace MusicServer
{
    public class Publisher
    {
        // this is a the port number in a released app this should be configurable
        private const string SERVICE_NAME = "21121";
        // this max packet size is used in case of a poor internet connection in order to get the packet in reasonable size transferred
        private const int MAX_PACKET_SIZE = 10000;
        private StreamSocketListener _listener;
        //the collection of virtual speakers
        // observable so that the UI can update when it changes
        private ObservableCollection<Speaker> _speakers;
        public ObservableCollection<Speaker> Speakers
        {
            get
            {
                if (_speakers == null)
                {
                    _speakers = new ObservableCollection<Speaker>();
                }
                return _speakers;
            }
        }

        public async Task<bool> Initialize()
        {
            bool success = true;
            try
            {
                //set up a listener socket for speakers to connect with
                _listener = new StreamSocketListener();
                _listener.Control.KeepAlive = true;
                _listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
                _listener.ConnectionReceived += Listener_ConnectionReceived;
                await _listener.BindServiceNameAsync(SERVICE_NAME);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex);
                success = false;
            }

            return success;
        }

        private async void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            Debug.WriteLine("Connection Received on Port {0}", sender.Information.LocalPort);
            StreamSocket streamSocket = args.Socket;
            if (streamSocket != null)
            {
                DataReader reader = new DataReader(streamSocket.InputStream);
                try
                {
                    // Read first 4 bytes (length of the subsequent string).
                    uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
                    if (sizeFieldCount != sizeof(uint))
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }

                    // Read the length of the 'packet'.
                    uint length = reader.ReadUInt32();
                    uint actualLength = await reader.LoadAsync(length);
                    if (length != actualLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data.
                        return;
                    }

                    string name = reader.ReadString(actualLength);
                    Speaker speaker = new Speaker()
                    {
                        Name = name,
                        Address = streamSocket.Information.RemoteAddress.DisplayName,
                        Status = "Connected",
                        Socket = streamSocket
                    };

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Speakers.Add(speaker);
                    });

                    reader.DetachStream();

                    Debug.WriteLine("New speaker added " + name);

                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error in connection received: " + e);
                }
            }
        }

        public async void Send(byte[] fileBytes)
        {
            try
            {
                if (Speakers != null && Speakers.Count > 0 && fileBytes != null)
                {
                    //iterate through the speakers and send out the media file to each speaker
                    foreach (Speaker speaker in Speakers)
                    {
                        StreamSocket socket = speaker.Socket;

                        if (socket != null)
                        {
                            IOutputStream outStream = socket.OutputStream;
                            using (DataWriter dataWriter = new DataWriter(outStream))
                            {
                                //write header bytes to indicate to the subscriber 
                                //information about the file to be sent
                                dataWriter.WriteInt16((short)MessageType.Media);
                                dataWriter.WriteInt32(fileBytes.Length);
                                await dataWriter.StoreAsync();
                                //start from 0 and increase by packet size
                                int partNumber = 0;
                                int sourceIndex = 0;
                                int bytesToWrite = fileBytes.Length;
                                while (bytesToWrite > 0)
                                {
                                    dataWriter.WriteInt32(partNumber);
                                    int packetSize = bytesToWrite;
                                    if (packetSize > MAX_PACKET_SIZE)
                                    {
                                        packetSize = MAX_PACKET_SIZE;
                                    }
                                    byte[] fragmentedPixels = new byte[packetSize];
                                    Array.Copy(fileBytes, sourceIndex, fragmentedPixels, 0, packetSize);
                                    dataWriter.WriteBytes(fragmentedPixels);
                                    Debug.WriteLine("sent byte packet length " + packetSize);
                                    await dataWriter.StoreAsync();
                                    sourceIndex += packetSize;
                                    bytesToWrite -= packetSize;
                                    partNumber++;
                                    Debug.WriteLine("sent total bytes " + (fileBytes.Length - bytesToWrite));
                                }
                                //Finally DetachStream
                                dataWriter.DetachStream();
                            }
                        }
                    }


                    //check the speakers have all received the file
                    foreach (Speaker speaker in Speakers)
                    {
                        StreamSocket socket = speaker.Socket;
                        if (socket != null)
                        {
                            //wait for the 'I got it' message
                            DataReader reader = new DataReader(socket.InputStream);
                            uint x = await reader.LoadAsync(sizeof(short));
                            MessageType t = (MessageType)reader.ReadInt16();
                            if (MessageType.Ready == t)
                            {
                                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    Speakers.Remove(speaker);
                                    speaker.Status = "Ready";
                                    Speakers.Add(speaker);
                                });
                            }
                            reader.DetachStream();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async void ToggleMediaState(MessageType type)
        {
            try
            {
                if (Speakers != null && Speakers.Count > 0)
                {
                    foreach (Speaker speaker in Speakers)
                    {
                        StreamSocket socket = speaker.Socket;
                        if (socket != null)
                        {
                            IOutputStream outStream = socket.OutputStream;
                            using (DataWriter dataWriter = new DataWriter(outStream))
                            {
                                dataWriter.WriteInt16((Int16)type);
                                await dataWriter.StoreAsync();
                                dataWriter.DetachStream();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error playing: " + ex);
            }
        }

        public bool CheckSpeakersAreReady()
        {
            bool speakersReady = true;

            foreach (Speaker speaker in Speakers)
            {
                if (!Equals(speaker.Status, "Ready"))
                {
                    speakersReady = false;
                    break;
                }
            }

            return speakersReady;
        }
    }
}
