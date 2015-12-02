using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MusicSubscriber
{
    public sealed class Subscriber
    {
        // two streams in memory.
        // One stream is used to receive the file
        // the other is used to play the file
        private static IRandomAccessStream _incomingStream;
        private static IRandomAccessStream _playingStream;

        private StreamSocket _socket;
        private DataWriter _writer;
        // this max packet size is used in case of a poor internet connection in order to get the packet in reasonable size transferred
        private const uint MAX_PACKET_SIZE = 10000;
        private uint _totalBytesRead = 0;

        public Subscriber()
        {
            //the two streams actually point to the same underlying data 
            _incomingStream = new InMemoryRandomAccessStream();
            _playingStream = _incomingStream.CloneStream();
        }

        public async void ConnectAsync(string host, string name)
        {
            HostName hostName;
            try
            {
                hostName = new HostName(host);
            }
            catch (ArgumentException)
            {
                Debug.WriteLine("Error: Invalid host name {0}.", host);
                return;
            }

            _socket = new StreamSocket();
            _socket.Control.KeepAlive = true;
            _socket.Control.QualityOfService = SocketQualityOfService.LowLatency;

            //hard coded port - can be user-specified but to keep this sample simple it is 21121
            await _socket.ConnectAsync(hostName, "21121");
            Debug.WriteLine("Connected");

            //first message to send is the name of this virtual speaker
            _writer = new DataWriter(_socket.OutputStream);
            _writer.WriteUInt32(_writer.MeasureString(name));
            _writer.WriteString(name);

            try
            {
                await _writer.StoreAsync();
                Debug.WriteLine("{0} registered successfully.", name);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Send failed with error: " + exception.Message);
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }

            //set no current message type
            MessageType currentMessageType = MessageType.Unknown;
            // then wait for the audio to be sent to us 
            DataReader reader = new DataReader(_socket.InputStream);

            while (true)
            {
                uint x = await reader.LoadAsync(sizeof(short));
                short t = reader.ReadInt16();
                currentMessageType = (MessageType)t;

                switch (currentMessageType)
                {
                    case MessageType.Media:
                        await ReadMediaFileAsync(reader);
                        break;
                    case MessageType.Play:
                        {
                            MediaPlayer mediaPlayer = BackgroundMediaPlayer.Current;
                            if (mediaPlayer != null)
                            {
                                if (mediaPlayer.CurrentState != MediaPlayerState.Playing)
                                {
                                    mediaPlayer.SetStreamSource(_playingStream);
                                    mediaPlayer.Play();
                                    Debug.WriteLine("Player playing. TotalDuration = " +
                                        mediaPlayer.NaturalDuration.Minutes + ':' + mediaPlayer.NaturalDuration.Seconds);
                                }
                            }
                        }
                        break;
                    case MessageType.Stop:
                        {
                            MediaPlayer mediaPlayer = BackgroundMediaPlayer.Current;
                            if (mediaPlayer != null)
                            {
                                if (mediaPlayer.CurrentState == MediaPlayerState.Playing)
                                {
                                    mediaPlayer.Pause();
                                    Debug.WriteLine("Player paused");
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
                currentMessageType = MessageType.Unknown;
            }
        }

        private async Task ReadMediaFileAsync(DataReader reader)
        {
            //a media file will always start with an int32 containing the file length
            await reader.LoadAsync(sizeof(int));
            int messageLength = reader.ReadInt32();

            Debug.WriteLine("Message Length " + messageLength);

            _totalBytesRead = 0;
            uint bytesRead = 0;
            IBuffer readBuffer = new Windows.Storage.Streams.Buffer(MAX_PACKET_SIZE);

            // read as many blocks as are in the incoming stream - this prevents blocks getting dropped
            do
            {
                await reader.LoadAsync(sizeof(int));
                int partNumber = reader.ReadInt32();
                Debug.WriteLine("Part " + partNumber);

                readBuffer = await _socket.InputStream.ReadAsync(readBuffer, MAX_PACKET_SIZE,
                    InputStreamOptions.Partial);

                bytesRead = readBuffer.Length;
                Debug.WriteLine("Bytes read " + bytesRead);

                if (bytesRead > 0)
                {
                    _incomingStream.WriteAsync(readBuffer).GetResults();
                    _totalBytesRead += bytesRead;
                }
                Debug.WriteLine("Total bytes read: " + _totalBytesRead);

            }
            while (_totalBytesRead < messageLength);

            Debug.WriteLine("Incoming stream length " + _incomingStream.Size);

            if (_totalBytesRead >= messageLength)
            {
                if (_writer == null)
                {
                    _writer = new DataWriter(_socket.OutputStream);
                }

                _writer.WriteUInt16((UInt16)MessageType.Ready);
                await _writer.StoreAsync();

                messageLength = 0;
            }
        }
    }
}
