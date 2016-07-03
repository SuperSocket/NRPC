using System;
using System.Collections.Generic;
using System.Text;

namespace NRPC.Base
{

    public static class Extensions
    {
        /// <summary>
        /// Gets string from the binary segments data.
        /// </summary>
        /// <param name="encoding">The text encoding to decode the binary data.</param>
        /// <param name="data">The binary segments data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>
        /// the decoded string
        /// </returns>
        public static string GetString(this Encoding encoding, IList<ArraySegment<byte>> data, int offset, int length)
        {
            var output = new char[encoding.GetMaxCharCount(length)];

            var decoder = encoding.GetDecoder();

            var totalCharsLen = 0;
            var totalBytesLen = 0;
            var bytesUsed = 0;
            var charsUsed = 0;
            var completed = false;

            var targetOffset = 0;

            for (var i = 0; i < data.Count; i++)
            {
                var segment = data[i];
                var srcOffset = segment.Offset;
                var srcLength = segment.Count;
                var lastSegment = false;

                //Haven't found the offset position
                if (totalBytesLen == 0)
                {
                    var targetEndOffset = targetOffset + segment.Count - 1;

                    if (offset > targetEndOffset)
                    {
                        targetOffset = targetEndOffset + 1;
                        continue;
                    }

                    //the offset locates in this segment
                    var margin = offset - targetOffset;
                    srcOffset = srcOffset + margin;
                    srcLength = srcLength - margin;

                    if (srcLength >= length)
                    {
                        srcLength = length;
                        lastSegment = true;
                    }
                }
                else
                {
                    var restLength = length - totalBytesLen;

                    if (restLength <= srcLength)
                    {
                        srcLength = restLength;
                        lastSegment = true;
                    }
                }

                decoder.Convert(segment.Array, srcOffset, srcLength, output, totalCharsLen, output.Length - totalCharsLen, lastSegment, out bytesUsed, out charsUsed, out completed);
                totalCharsLen += charsUsed;
                totalBytesLen += bytesUsed;
            }

            return new string(output, 0, totalCharsLen);
        }
    }
}