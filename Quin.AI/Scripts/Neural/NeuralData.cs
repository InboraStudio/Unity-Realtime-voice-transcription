using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LeastSquares.Neural
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NeuralDataSerialized
    {
        [MarshalAs(UnmanagedType.SysInt)]
        public IntPtr names;
        [MarshalAs(UnmanagedType.SysInt)]
        public IntPtr tensors;
        [MarshalAs(UnmanagedType.I4)]
        public int count;
    }
    
    public class NeuralData : IDisposable
    {
        public string[] Names { get; private set;  }
        public NeuralTensor[] Tensors { get; private set; }
        public int Count { get; private set;  }
        public NeuralDataSerialized _serialized;

        public NeuralDataSerialized Serialized => _serialized;
        private bool _isNative;
        private bool _disposed;

        private NeuralData() {}
        
        public NeuralData(NeuralTensor[] tensors, string[] names)
        {
            if (tensors.Length != names.Length)
            {
                throw new ArgumentException("Tensors and names must have the same length.");
            }

            Names = names;
            Tensors = tensors;
            var count = tensors.Length;
            var namesPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * names.Length);
            var tensorsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NeuralTensorSerialized)) * tensors.Length);

            for (int i = 0; i < names.Length; i++)
            {
                // Allocate and write each string.
                var strPtr = Marshal.StringToHGlobalAnsi(names[i]);
                Marshal.WriteIntPtr(namesPtr, i * Marshal.SizeOf(typeof(IntPtr)), strPtr);

                // Serialize each tensor and write the pointer to the serialized data.
                var tensorSerialized = tensors[i].Serialized;
                Marshal.StructureToPtr(tensorSerialized, tensorsPtr + i * Marshal.SizeOf(typeof(NeuralTensorSerialized)), false);
            }

            _serialized.names = namesPtr;
            _serialized.tensors = tensorsPtr;
            _serialized.count = count;
        }

        public static NeuralData FromSerialized(NeuralDataSerialized serialized)
        {
            var tensors = new List<NeuralTensor>();
            var names = new List<string>();

            var count = serialized.count;
            for (var i = 0; i < count; ++i)
            {
                var serializedTensor = Marshal.PtrToStructure<NeuralTensorSerialized>(serialized.tensors + i * Marshal.SizeOf<NeuralTensorSerialized>());
                tensors.Add(NeuralTensor.FromSerialized(serializedTensor));
                
                var namePointer = Marshal.ReadIntPtr(serialized.names + i * Marshal.SizeOf<IntPtr>());
                names.Add(Marshal.PtrToStringAuto(namePointer));
            }
            
            return new NeuralData
            {
                Names = names.ToArray(),
                Tensors = tensors.ToArray(),
                _serialized = serialized,
                _isNative = true
            };
        }

        public void DisposeManaged()
        {
            // Free the string pointers.
            for (int i = 0; i < Count; i++)
            {
                IntPtr strPtr = Marshal.ReadIntPtr(_serialized.names, i * Marshal.SizeOf(typeof(IntPtr)));
                Marshal.FreeHGlobal(strPtr);
            }

            // Free the array of pointers.
            Marshal.FreeHGlobal(_serialized.names);
            Marshal.FreeHGlobal(_serialized.tensors);
        }

        public void DisposeNative()
        {
            //NeuralNative.neural_free_data(_serialized);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            if(_isNative)
                DisposeNative();
            else
                DisposeManaged();
            

            for (var i = 0; i < Tensors.Length; i++)
            {
                Tensors[i].Dispose();
            }
            
            _disposed = true;
        }
    }
}