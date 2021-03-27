namespace conIpam
{
    public class IpFile
    {
        public IpRange IPv4 { get; set; } = new IpRange("0.0.0.0/0", "v4", "This is default range of conIpam");

        public IpRange IPv6 { get; set; } = new IpRange("::/0", "v6", "This is default range of conIpam");
    }
}