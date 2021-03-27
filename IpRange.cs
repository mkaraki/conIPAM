using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace conIpam
{
    public class IpRange
    {
        [Obsolete("This is for Json Converter.")]
        [JsonConstructor]
        public IpRange()
        { }

        public IpRange(string ip_with_cidr, string RangeName, string Description = null)
        {
            ip_with_cidr ??= "0.0.0.0/0";
            this.IpWithCidr = ip_with_cidr;
            this.RangeName = RangeName;
            this.Description = Description;

            this.InnerRanges = new List<IpRange>();
            this.Hosts = new List<IpHost>();
        }

        public string IpWithCidr
        {
            get => ipcidr;
            set
            {
                string[] ip_s = value.Split('/');
                if (ip_s.Length != 2)
                    throw new FormatException();

                if (!IPAddress.TryParse(ip_s[0], out var ip))
                    throw new FormatException();

                int max_cidr;
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    max_cidr = 32;
                else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    max_cidr = 128;
                else
                    throw new FormatException();

                if (!int.TryParse(ip_s[1], out var cidr))
                    throw new FormatException();

                if (cidr > max_cidr)
                    throw new FormatException();

                var fullmask = BigInteger.Pow(2, max_cidr) - 1;
                var ipa = new BigInteger(ip.GetAddressBytes().Reverse().ToArray());
                int rcidr = max_cidr - cidr;
                var mask = fullmask >> rcidr << rcidr;

                var netaddr = ipa & mask;

                byte[] nipbyte = new byte[max_cidr / 8];
                netaddr.ToByteArray(true, true).CopyTo(nipbyte, 0);

                var nip = new IPAddress(nipbyte);

                IpVersion = (IpVersion)max_cidr;

                ipcidr = nip.ToString() + '/' + cidr;
            }
        }

        private string ipcidr;

        public IpVersion IpVersion { get; private set; }

        public string RangeName { get; set; }

        public string Description { get; set; }

        public List<IpHost> Hosts { get; set; }

        public List<IpRange> InnerRanges { get; set; }

        public IEnumerable<IpHost> GetAllHosts()
        {
            return Hosts;
        }

        public IEnumerable<IpHost> GetAllHosts(bool recurse)
        {
            if (recurse == false)
                return GetAllHosts();

            var hosts = new List<IpHost>();
            hosts.AddRange(Hosts);

            foreach (var range in InnerRanges)
                hosts.AddRange(range.GetAllHosts(true));

            return hosts;
        }

        public (IPAddress, int) GetIpAndCidr()
        {
            string[] s = ipcidr.Split('/');

            var ip = IPAddress.Parse(s[0]);
            var cidr = int.Parse(s[1]);

            return (ip, cidr);
        }

        public bool IsRangeInRange(IpRange range)
        {
            if (range.IpVersion != IpVersion)
                return false;

            var tgtip = range.GetIpAndCidr();
            var thisip = GetIpAndCidr();

            if (tgtip.Item2 <= thisip.Item2)
                return false;

            return IsAddressInRange(tgtip.Item1);
        }

        public bool IsRangeInChildRange(IpRange range)
        {
            foreach (var rng in InnerRanges)
                if (rng.IsRangeInRange(range))
                    return true;

            return false;
        }

        public bool IsAddressInRange(IpHost host)
            => IsAddressInRange(host.GetIP());

        public bool IsAddressInRange(IPAddress ip)
        {
            int max_cidr = (int)IpVersion;
            var t = GetIpAndCidr();

            if (t.Item1.AddressFamily != ip.AddressFamily)
                return false;

            var fullmask = BigInteger.Pow(2, max_cidr) - 1;
            var ipa = new BigInteger(ip.GetAddressBytes().Reverse().ToArray());
            int rcidr = max_cidr - t.Item2;
            var mask = fullmask >> rcidr << rcidr;
            var netaddr = ipa & mask;

            var nipa = new BigInteger(t.Item1.GetAddressBytes().Reverse().ToArray());

            return netaddr == nipa;
        }

        public bool IsAddressInChildRange(IpHost host)
            => IsAddressInChildRange(host.GetIP());

        public bool IsAddressInChildRange(IPAddress ip)
        {
            foreach (var rng in InnerRanges)
            {
                if (rng.IsAddressInRange(ip))
                    return true;
            }

            return false;
        }
    }
}