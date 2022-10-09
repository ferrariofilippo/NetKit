namespace NetKit.Model
{
    public class Network
    {
        public string NetworkAddress { get; set; }

        public string BroadcastAddress { get; set; }

        public string SubnetMask { get; set; }

        public byte PrefixLength { get; set; }

        public uint HostNumber { get; set; }
    }
}
