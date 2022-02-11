using System;
using System.Collections.Generic;

using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public class IPAddressCidr : IPAddress
    {
        #region Fields

        public IPAddress IPAddress => this;

        public byte Prefix { get; }

        #endregion

        #region Constructors

        public IPAddressCidr(byte[] address, byte prefix) : base(address)
        {
            Prefix = prefix;
        }

        public IPAddressCidr(long newAddress, byte prefix) : base(newAddress)
        {
            Prefix = prefix;
        }

        public IPAddressCidr(ReadOnlySpan<byte> address, byte prefix) : base(address)
        {
            Prefix = prefix;
        }

        public IPAddressCidr(byte[] address, long scopeid, byte prefix) : base(address, scopeid)
        {
            Prefix = prefix;
        }

        public IPAddressCidr(ReadOnlySpan<byte> address, long scopeid, byte prefix) : base(address, scopeid)
        {
            Prefix = prefix;
        }

        public IPAddressCidr(IPAddress address, byte prefix) : base(address.GetAddressBytes())
        {
            Prefix = prefix;
        }

        public IPAddressCidr((IPAddress address, byte prefix) value) : base(value.address.GetAddressBytes())
        {
            Prefix = value.prefix;
        }

        #endregion

        #region Operators

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"{base.ToString()}/{(int)Prefix}";
        }

        #endregion

        #region Statics

        public static bool TryParse(string input, out IPAddressCidr ipAddressCidr)
        {
            if (IPAddress.TryParse(input, out var ipAddress))
            {
                ipAddressCidr = new IPAddressCidr(ipAddress, 32);
                return true;
            }
            else if (IPNetwork.TryParse(input, out var ipNetwork))
            {
                ipAddressCidr = new IPAddressCidr(ipNetwork.Network, ipNetwork.Cidr);
                return true;
            }
            else
            {
                ipAddressCidr = null;
                return false;
            }
        }

        public static new IPAddressCidr Parse(string input)
        {
            if (TryParse(input, out IPAddressCidr value)) return value;
            else return null;
        }

        #endregion
    }

    public static class IPAddressCidrExtensions
    {
        public static IPAddressCidr ToCidr(this IPAddress ipAddress)
        {
            return new IPAddressCidr(ipAddress.GetAddressBytes(), 32);
        }

        public static IPAddressCidr ToCidr(this IPAddress ipAddress, byte prefix)
        {
            return new IPAddressCidr(ipAddress.GetAddressBytes(), prefix);
        }

    }
}
