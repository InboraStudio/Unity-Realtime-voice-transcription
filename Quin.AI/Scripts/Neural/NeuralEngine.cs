using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AOT;
using Debug = UnityEngine.Debug;

namespace LeastSquares.Neural
{
    public class NeuralEngine : IDisposable
    {
        private bool _verbose;
        private IntPtr _neuralContext;
        public IntPtr Context => _neuralContext;
        
        public NeuralEngine(bool verbose = false)
        {
            _verbose = verbose;
            NeuralNative.neural_register_log_callback(HandleLog);
            _neuralContext = NeuralNative.neural_create();
            if (_neuralContext == IntPtr.Zero)
                throw new ArgumentException("Failed to create neural context");
        }

            
        [MonoPInvokeCallback(typeof(NeuralNative.LogCallback))]
        static void HandleLog(IntPtr message)
        {
            Debug.Log("Received log from neural: " + Marshal.PtrToStringAnsi(message));
        }

        public void Log(object text)
        {
            if (_verbose)
                Debug.Log(text);
        }
        
        public NeuralData Inference(NeuralModel model, NeuralData input)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var output = NeuralNative.neural_infer(_neuralContext, model.Data, input._serialized);
            stopwatch.Stop();
            Log(Marshal.SizeOf<NeuralValue>());
            Log($"Neural inference took {stopwatch.ElapsedMilliseconds} ms");
            return NeuralData.FromSerialized(output);
        }

        public void Dispose()
        {
            NeuralNative.neural_free(_neuralContext);
            Log("Engine disposed.");
        }
    }
}