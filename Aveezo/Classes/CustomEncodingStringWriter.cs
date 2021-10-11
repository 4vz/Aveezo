using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aveezo
{
    public sealed class CustomEncodingStringWriter : StringWriter
    {
        private readonly Encoding stringWriterEncoding;

        public CustomEncodingStringWriter(StringBuilder builder, Encoding encoding) : base(builder)
        {
            stringWriterEncoding = encoding;
        }

        public CustomEncodingStringWriter(Encoding encoding) : this(new StringBuilder(), encoding)
        {
        }

        public override Encoding Encoding => stringWriterEncoding;
    }
}
