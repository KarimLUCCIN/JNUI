using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace JapanNUI.ImageProcessing
{
    public static class VectorUtils
    {
        static VectorUtils()
        {
            fBytes = new floatByteConvert();
            fBytes.bValue = new byte[4];
        }

        [StructLayout(LayoutKind.Explicit)]
        struct floatByteConvert
        {
            [FieldOffset(0)]
            public float[] floatValue;

            [FieldOffset(0)]
            public byte[] bValue;
        }

        static floatByteConvert fBytes;
        static object sync = new object();

        public static float FloatFromBytes(byte[] data, int offset = 0)
        {
            //lock (sync)
            {
                for (int i = offset; i < offset + 4; i++)
                {
                    fBytes.bValue[i - offset] = data[i];
                }

                return fBytes.floatValue[0];
            }
        }

        public static unsafe void BytesFromFloat(float f, byte[] outData, int offset = 0)
        {
            fixed (byte* p = outData)
            {
                *(p+0) = *(((byte*)&f)+0);
                *(p+1) = *(((byte*)&f)+1);
                *(p+2) = *(((byte*)&f)+2);
                *(p+3) = *(((byte*)&f)+3);
            }

            ////lock (sync)
            //{
            //    fBytes.floatValue[0] = f;

            //    for (int i = offset; i < offset + 4; i++)
            //    {
            //        outData[i] = fBytes.bValue[i - offset];
            //    }
            //}
        }
    }
}
