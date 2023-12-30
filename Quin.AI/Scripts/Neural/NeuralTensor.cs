using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LeastSquares.Neural
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NeuralTensorSerialized
    {
        public IntPtr data;
        [MarshalAs(UnmanagedType.I4)]
        public NeuralType type;
        public IntPtr dims;
        public int dims_count;
        public int size;
    }

    public class NeuralTensor : IDisposable
    {
        public NeuralType Type { get; private set; }
        public NeuralValue[] Values { get; private set; }
        public int[] Dimensions { get; private set; }
        private bool _disposed;
        private NeuralTensorSerialized _serialized;
        private bool _isNative;
        
        private NeuralTensor() {}

        public NeuralTensor(NeuralType type, NeuralValue[] values) : this(type, values, new []{ values.Length }) {}
        
        public NeuralTensor(NeuralType type, NeuralValue[] values, int[] dimensions)
        {
            Values = values;
            Dimensions = dimensions;
            Type = type;

            var dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NeuralValue)) * values.Length);
            var dimsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * dimensions.Length);
            for (var i = 0; i < values.Length; i++)
            {
                Marshal.StructureToPtr(values[i], dataPtr + i * Marshal.SizeOf(typeof(NeuralValue)), false);
            }

            var dataCount = values.Length;
            for (var i = 0; i < dimensions.Length; i++)
            {
                Marshal.WriteInt32(dimsPtr + i * Marshal.SizeOf(typeof(int)), dimensions[i]);
            }

            var dimsCount = dimensions.Length; 
            _serialized = new NeuralTensorSerialized
            {
                data = dataPtr,
                type = type,
                dims = dimsPtr,
                dims_count = dimsCount,
                size = dataCount,
            };
        }

        public static NeuralTensor FromSerialized(NeuralTensorSerialized serialized)
        {
            var dimensions = new List<int>();
            var values = new List<NeuralValue>();

            var dimLength = serialized.dims_count;
            var offset = 0;
            for (var i = 0; i < dimLength; ++i)
            {
                var dim = Marshal.ReadInt32(serialized.dims + i * Marshal.SizeOf<int>());
                dimensions.Add(dim);
                for (var k = 0; k < dim; ++k)
                {
                    values.Add(Marshal.PtrToStructure<NeuralValue>(serialized.data + offset * Marshal.SizeOf<NeuralValue>()));
                    offset += 1;
                }
            }
            
            return new NeuralTensor
            {
                Type = serialized.type,
                Dimensions = dimensions.ToArray(),
                Values = values.ToArray(),
                _serialized = serialized,
                _isNative = true
            };
        }

        public NeuralTensorSerialized Serialized => _serialized;

        public void DisposeManaged()
        {
            Marshal.FreeHGlobal(Serialized.data);
            Marshal.FreeHGlobal(Serialized.dims);
        }

        public void DisposeNative()
        {
            NeuralNative.neural_free_tensor(Serialized);
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
        
            if (_isNative)
                DisposeNative();
            else
                DisposeManaged();

            _disposed = true;
        }
    }
}