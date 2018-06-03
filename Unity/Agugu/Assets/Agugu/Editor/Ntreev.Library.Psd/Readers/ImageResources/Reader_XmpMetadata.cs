#region License
//Ntreev Photoshop Document Parser for .Net
//
//Released under the MIT License.
//
//Copyright (c) 2015 Ntreev Soft co., Ltd.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Text;

namespace Ntreev.Library.Psd.Readers.ImageResources
{
    [ResourceID("1060", DisplayName = "XmpMetadata")]
    class Reader_XmpMetadata : ResourceReaderBase
    {
        public Reader_XmpMetadata(PsdReader reader, long length)
            : base(reader, length)
        {

        }

        protected override void ReadValue(PsdReader reader, object userData, out IProperties value)
        {
            Properties props = new Properties(1);

            // There seems a undocumented zero padding in XMP block...
            // TODO: Find the XMP document that explains how it works
            byte[] bytes = reader.ReadBytes((int) Length);
            byte[] truncatedByteArray = _RemoveTailingZeros(bytes);
            string xmpString = Encoding.UTF8.GetString(truncatedByteArray);
            props.Add("Xmp", xmpString);

            value = props;
        }

        private byte[] _RemoveTailingZeros(byte[] source)
        {
            int zeroCount = 0;
            for (int i = source.Length - 1; 0 <= i; i--)
            {
                if (source[i] == 0)
                {
                    zeroCount++;
                }
                else
                {
                    break;
                }
            }

            if (zeroCount == 0)
            {
                return source;
            }
            else
            {
                byte[] truncatedArray = new byte[source.Length - zeroCount];
                Array.Copy(source, truncatedArray, truncatedArray.Length);
                return truncatedArray;
            }
        }
    }
}
