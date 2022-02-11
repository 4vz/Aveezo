using System;
using System.Collections.Generic;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.Collections;
using System.Net.NetworkInformation;

namespace Aveezo
{
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => TimeSpanUtil.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToISO8601());
    }

    public class BitArrayJsonConverter : JsonConverter<BitArray>
    {
        public override BitArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString().ToBitArray();

        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString('0', '1'));
    }

    public class PhysicalAddressJsonConverter : JsonConverter<PhysicalAddress>
    {
        public override PhysicalAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => NetworkEquipment.ParsePhysicalAddress(reader.GetString());

        public override void Write(Utf8JsonWriter writer, PhysicalAddress value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(":"));
    }

    public class IPAddressCidrJsonConverter : JsonConverter<IPAddressCidr>
    {
        public override IPAddressCidr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => IPAddressCidr.Parse(reader.GetString());

        public override void Write(Utf8JsonWriter writer, IPAddressCidr value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }

    public class IPAddressJsonConverter : JsonConverter<IPAddress>
    {
        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => IPAddressCidr.Parse(reader.GetString())?.IPAddress;

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }

}
