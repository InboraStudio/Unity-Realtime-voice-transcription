using System;
using System.Runtime.InteropServices;

namespace LeastSquares.Neural
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct NeuralValue
    {
        [FieldOffset(0)] public float as_float;
        [FieldOffset(0)] public int as_int32;
        [FieldOffset(0)] public byte as_uint8;
        [FieldOffset(0)] public long as_int64;
        [FieldOffset(0)] public IntPtr as_string;

        public string GetString()
        {
            return Marshal.PtrToStringAnsi(as_string);
        }
    }
}