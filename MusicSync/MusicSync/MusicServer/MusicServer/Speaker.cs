using Windows.Networking.Sockets;

namespace MusicServer
{
    /// <summary>
    /// Speaker class encapsulates the information on a
    /// virtual speaker
    /// </summary>
    public class Speaker
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public StreamSocket Socket { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}   {2}", Name, Address, Status);
        }
    }
}
