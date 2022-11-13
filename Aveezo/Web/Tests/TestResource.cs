using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

#if DEBUG

namespace Aveezo.Providers.TestResource.V1;

public class TestResource : Resource
{
    [Field("Data1", FieldOptions.CanQuery)]
    public string Data1 { get; set; }

    public string Data2 { get; set; }

    public int Data3 { get; set; }
}

/// <summary>
/// Here's the summary of the test API
/// </summary>
[Path("test")]
public class TestResources : Api
{
    public TestResources(IServiceProvider i) : base(i) { }

    /// <summary>
    /// Get and Post the Test API
    /// </summary>
    /// <returns>TestApiResponseModel</returns>
    [Get("resource")]
    public Result<TestResource> Get()
    {
        return null;
    }
}

#endif

