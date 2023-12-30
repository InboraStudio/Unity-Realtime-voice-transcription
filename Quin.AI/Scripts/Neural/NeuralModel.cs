using System;
using System.Runtime.InteropServices;
using LeastSquares.Undertone;
using UnityEngine;

namespace LeastSquares.Neural
{
    public class NeuralModel : IDisposable
    {
        public IntPtr Data { get; private set; }
        private NeuralModel() { }
        
        public static NeuralModel FromFile(NeuralEngine engine, string filename)
        {
            var path = Marshal.StringToHGlobalAnsi(filename);
            // Currently there is a bug and files cannot be loaded from a path on windows
            var bytes = System.IO.File.ReadAllBytes(filename);
            var ptr = FixedPointerToHeapAllocatedMem.Create(bytes, (uint) bytes.Length);
            var modelPtr = NeuralNative.neural_load_model_from_memory(engine.Context, ptr.Address, ptr.SizeInBytes);
            //var modelPtr = NeuralNative.neural_load_model_from_file(engine.Context, path);
            AssertValid(modelPtr);
            try
            {
                return new NeuralModel
                { 
                    Data = modelPtr
                };
            }
            finally
            {
                Marshal.FreeHGlobal(path);
            }
        }
        
        public static NeuralModel FromMemory(NeuralEngine engine, IntPtr ptr, uint size)
        {
            var modelPtr = NeuralNative.neural_load_model_from_memory(engine.Context, ptr, size);
            AssertValid(modelPtr);
            return new NeuralModel
            {
                Data = modelPtr
            };
        }

        private static void AssertValid(IntPtr modelPtr)
        {
            if (modelPtr == IntPtr.Zero)
                throw new ArgumentException("Failed to load model");
        }

        public void Dispose()
        {
            NeuralNative.neural_free_model(Data);
            Debug.Log("Model disposed.");
        }
    }
}