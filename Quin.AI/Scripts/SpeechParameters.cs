using System;
using System.Runtime.InteropServices;

namespace LeastSquares.Undertone
{
    public enum SamplingStrategy
    {
        Greedy = 0,
        BeamSearch = 1
    }
    
    public struct SpeechParameters
    {
        public SamplingStrategy Strategy;

        public int NThreads;
        public int NMaxTextCtx;
        public int OffsetMs;
        public int DurationMs;

        [MarshalAs(UnmanagedType.I1)]
        public bool Translate;
        [MarshalAs(UnmanagedType.I1)]
        public bool NoContext;
        [MarshalAs(UnmanagedType.I1)]
        public bool SingleSegment;
        [MarshalAs(UnmanagedType.I1)]
        public bool PrintSpecial;
        [MarshalAs(UnmanagedType.I1)]
        public bool PrintProgress;
        [MarshalAs(UnmanagedType.I1)]
        public bool PrintRealtime;
        [MarshalAs(UnmanagedType.I1)]
        public bool PrintTimestamps;

        [MarshalAs(UnmanagedType.I1)]
        public bool TokenTimestamps;
        public float TholdPt;
        public float TholdPtsum;
        public int MaxLen;
        [MarshalAs(UnmanagedType.I1)]
        public bool SplitOnWord;
        public int MaxTokens;

        [MarshalAs(UnmanagedType.I1)]
        public bool SpeedUp;
        public int AudioCtx;

        public string InitialPrompt;
        public string PromptTokens;
        public int PromptNTokens;
        
        public string Language;

        [MarshalAs(UnmanagedType.I1)]
        public bool SuppressBlank;
        [MarshalAs(UnmanagedType.I1)]
        public bool SuppressNonSpeechTokens;

        public float Temperature;
        public float MaxInitialTs;
        public float LengthPenalty;

        public float TemperatureInc;
        public float EntropyThold;
        public float LogprobThold;
        public float NoSpeechThold;

        public GreedyStruct Greedy;
        public BeamSearchStruct BeamSearch;

        public IntPtr NewSegmentCallback;
        public IntPtr NewSegmentCallbackUserData;

        public IntPtr ProgressCallback;
        public IntPtr ProgressCallbackUserData;

        public IntPtr EncoderBeginCallback;
        public IntPtr EncoderBeginCallbackUserData;

        public IntPtr LogitsFilterCallback;
        public IntPtr LogitsFilterCallbackUserData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GreedyStruct
    {
        public int BestOf;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BeamSearchStruct
    {
        public int BeamSize;
        public float Patience;
    }
}