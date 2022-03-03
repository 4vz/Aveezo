using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

#if DEBUG

namespace Aveezo.Providers.Test.V1;

/// <summary>
/// Here's the summary of the test API
/// </summary>
[Path("test/api")]
public class TestApi : Api
{
    public TestApi(IServiceProvider i) : base(i) { }

    /// <summary>
    /// Get and Post the Test API
    /// </summary>
    /// <returns>TestApiResponseModel</returns>
    [Post("object")]
    public Result<TestApiObject> Model([Body] TestApiRequestObject body)
    {
        var data = new TestApiObject
        {
            Bool = false,
            Sbyte = 4,
            Byte = 200,
            Short = 21324,
            Ushort = 1204,
            Int = 241221521,
            Uint = 21321421,
            Long = 21951295219332,
            Ulong = 2151252123,
            Char = '4',
            Float = 2142132121F,
            Double = 1242421.23213D,
            Decimal = 2141221123M,
            DateTime = DateTime.Now,
            DateTimeOffset = DateTime.UtcNow.ToDateTimeOffset(),
            TimeSpan = TimeSpan.FromSeconds(1000),
            String = body.Data,
            Guid = Guid.NewGuid(),
            BitArray = "1010101".ToBitArray(),
            PhysicalAddress = PhysicalAddress.Parse("AB:CD:EF:01:23:45"),
            IPAddressCidr = IPAddressCidr.Parse("10.10.10.10/24"),
            IPAddress = IPAddress.Parse("10.10.10.10"),
            Array = new object[] { 1, "Afis", 0.5f },
            IntArray = new[] { 2, 3, 4, 5 },
            StringArray = new[] { "Afis", "Herman", "Reza", "Devara" },
            Object = TimeSpan.FromSeconds(5),


            CharList = new List<char>(new[] { 'a', 'b', 'c' }),
            DictionaryShortString = new Dictionary<short, string> { { 4, "afis" }, { 5, "anisa" } },

            DictionaryStringFloat = new Dictionary<string, float> { { "CobaSihBagaimana", 5f } },

            Child = new TestApiResponseChildModel
            {
                Name = "Anisa",
                Age = 33,
                Child = new TestApiResponseChild2Model
                {
                    Address = "Aris Munandar 56",
                    Hobby = "Cooking"
                }
            }
        };

        return data;
    }

    [Get("single")]
    public Result<TestApiSimpleObject> Simple()
    {
        return null;
    }
}

public class TestApiSimpleObject
{
    public string StringProperty { get; set; }
}

public class TestApiRequestObject
{
    public string Data { get; set; } = "DEFAULT";
}

public class TestApiObject
{
    /// <summary>
    /// And dey sey and dey sey and dey sey.
    /// </summary>
    /// <remarks>Doomfist here</remarks>
    /// <example>Rising apricot</example>
    [Example("Enggghhh")]
    public string AndDeySey { get; set; }

    // # TypeSupport
    public bool Bool { get; set; }

    public sbyte Sbyte { get; set; }

    public byte Byte { get; set; }

    public short Short { get; set; }

    public ushort Ushort { get; set; }

    public int Int { get; set; }

    public uint Uint { get; set; }

    public long Long { get; set; }

    public ulong Ulong { get; set; }

    public char Char { get; set; }

    public float Float { get; set; }

    public double Double { get; set; }

    public decimal Decimal { get; set; }

    public DateTimeOffset DateTimeOffset { get; set; }

    public DateTime DateTime { get; set; }

    public TimeSpan TimeSpan { get; set; }

    public string String { get; set; }

    public Guid Guid { get; set; }

    public BitArray BitArray { get; set; }

    public PhysicalAddress PhysicalAddress { get; set; }

    public IPAddressCidr IPAddressCidr { get; set; }

    public IPAddress IPAddress { get; set; }

    public object[] Array { get; set; }

    public int[] IntArray { get; set; }

    public string[] StringArray { get; set; }

    public object Object { get; set; }

    //// Etc
    public List<char> CharList { get; set; }

    [Example("ffff", "as")]
    public Dictionary<short, string> DictionaryShortString { get; set; }

    public Dictionary<string, float> DictionaryStringFloat { get; set; }

    public TestApiResponseChildModel Child { get; set; }
}

public class TestApiResponseChildModel
{
    public string Name { get; set; }

    public int Age { get; set; }

    public TestApiResponseChild2Model Child { get; set; }
}

public class TestApiResponseChild2Model
{
    public string Address { get; set; }

    public string Hobby { get; set; }
}

#endif

