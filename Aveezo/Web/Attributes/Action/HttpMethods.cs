using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GetAttribute : HttpGetAttribute
    {
        public GetAttribute() { }
        public GetAttribute(string template) : base($"{template.TrimStart('/').TrimEnd('/')}")
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PostAttribute : HttpPostAttribute
    {
        public PostAttribute() { }
        public PostAttribute(string template) : base($"{template.TrimStart('/').TrimEnd('/')}")
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PutAttribute : HttpPutAttribute
    {
        public PutAttribute() { }
        public PutAttribute(string template) : base($"{template.TrimStart('/').TrimEnd('/')}")
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PatchAttribute : HttpPatchAttribute
    {
        public PatchAttribute() { }
        public PatchAttribute(string template) : base($"{template.TrimStart('/').TrimEnd('/')}")
        {

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DeleteAttribute : HttpDeleteAttribute
    {
        public DeleteAttribute() { }
        public DeleteAttribute(string template) : base($"{template.TrimStart('/').TrimEnd('/')}")
        {

        }
    }
}
