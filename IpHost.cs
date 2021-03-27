using System;
using System.Net;
using System.Text.Json.Serialization;

namespace conIpam
{

    public class IpHost
    {
        [Obsolete("This is for Json Converter.")]
        [JsonConstructor]
        public IpHost()
        { }

        public IpHost(string Ip, string Hostname, string Description = null)
        {
            this.Ip = Ip;

            this.Hostname = Hostname;

            this.Description = Description;
        }

        public string Ip
        {
            get => ip;
            set
            {
                if (!IPAddress.TryParse(value, out var ip))
                    throw new FormatException();

                int max_cidr;
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    max_cidr = 32;
                else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    max_cidr = 128;
                else
                    throw new FormatException();

                IpVersion = (IpVersion)max_cidr;

                this.ip = value;
            }
        }

        private string ip;

        public IpVersion IpVersion { get; private set; }

        public string Hostname { get; set; }

        public string Description { get; set; }

        public IPAddress GetIP()
        {
            return IPAddress.Parse(ip);
        }

    }

}