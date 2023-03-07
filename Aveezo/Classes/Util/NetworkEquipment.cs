using System;
using System.Collections.Generic;

using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public static class NetworkEquipment
    {
        #region Statics

        public static PhysicalAddress ParsePhysicalAddress(string value)
        {
            if (PhysicalAddress.TryParse(value, out PhysicalAddress address))
                return address;
            else
                return null;
        }

        #endregion
    }

    public static class NetworkEquipmentExtensions
    {
        public static string ToString(this PhysicalAddress value, string separator)
        {
            var spec = value.GetAddressBytes().Each(o => o.ToString("X2").ToUpper());
            return spec.Join(separator);
        }

    }
}
