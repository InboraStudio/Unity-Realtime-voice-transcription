using System;
using System.Runtime.InteropServices;

namespace LeastSquares.Undertone
{
    public class FixedMemoryBlock : IDisposable
    {
        public static FixedMemoryBlock Create(long size)
        {
            return new FixedMemoryBlock()
            {
                SizeInBytes = size,
                Address = Marshal.AllocHGlobal(new IntPtr(size))     
            };
        }
        
        public IntPtr Address { get; private set; }
        
        public long SizeInBytes { get; private set; }
        
        public void Free()
        {
            if (Address != IntPtr.Zero)
                Marshal.FreeHGlobal(Address);
            Address = IntPtr.Zero;
        }

        public void Dispose()
        {
            Free();
        }
    }
}