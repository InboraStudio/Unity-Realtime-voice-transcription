using System;
using System.Runtime.InteropServices;

namespace LeastSquares.Undertone
{
    /// <summary>
    /// A class that provides a fixed pointer to heap allocated memory.
    /// </summary>
    public class FixedPointerToHeapAllocatedMem : IDisposable
    {
        private GCHandle _handle;
        public IntPtr Address { get; private set; }

        /// <summary>
        /// Frees the memory allocated by the class.
        /// </summary>
        public void Dispose()
        {
            _handle.Free();
        }

        /// <summary>
        /// Creates a new instance of the FixedPointerToHeapAllocatedMem class.
        /// </summary>
        /// <typeparam name="T">The type of object to allocate memory for.</typeparam>
        /// <param name="Object">The object to allocate memory for.</param>
        /// <param name="SizeInBytes">The size of the memory to allocate in bytes.</param>
        /// <returns>A new instance of the FixedPointerToHeapAllocatedMem class.</returns>
        public static FixedPointerToHeapAllocatedMem Create<T>(T Object, uint SizeInBytes)
        {
            var pointer = new FixedPointerToHeapAllocatedMem
            {
                _handle = GCHandle.Alloc(Object, GCHandleType.Pinned),
                SizeInBytes = SizeInBytes
            };
            pointer.Address = pointer._handle.AddrOfPinnedObject();
            return pointer;
        }

        public uint SizeInBytes { get; private set; }
    }
}