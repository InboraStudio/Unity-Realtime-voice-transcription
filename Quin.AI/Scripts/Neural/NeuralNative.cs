using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LeastSquares.Neural
{
    public static class NeuralNative
    {
        
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            private const string LibName = "__Internal";
#else
        private const string LibName = "neural";
#endif
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallback(IntPtr message);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_register_log_callback(LogCallback callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr neural_create();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern IntPtr neural_load_model_from_file(IntPtr ctx, IntPtr filename);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr neural_load_model_from_memory(IntPtr ctx, IntPtr modelBuffer, uint modelBufferSize);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern NeuralDataSerialized neural_infer(IntPtr ctx, IntPtr model, NeuralDataSerialized data);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_free_data(NeuralDataSerialized data);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_free_tensor(NeuralTensorSerialized tensor);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_free_model(IntPtr model);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_free(IntPtr ctx);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern NeuralDataSerialized neural_get_whisper_data(IntPtr model, IntPtr bytes, uint audio_size, IntPtr language, IntPtr task,
            IntPtr prompt, [MarshalAs(UnmanagedType.I1)] bool predict_timestamps, int max_length, int num_of_beams);
        
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void neural_load_whisper_tokenizer_from_memory(IntPtr model, IntPtr bytes, int size);
    }
}