using System;
using System.IO;
using System.Threading.Tasks;

namespace LeastSquares.Undertone
{
    /// <summary>
    /// A struct representing a transcription request.
    /// </summary>
    public struct TranscriptionRequest
    {
        /// <summary>
        /// A task completion source for the transcription request.
        /// </summary>
        public TaskCompletionSource<SpeechSegment[]> tcs;

        /// <summary>
        /// An array of audio samples for the transcription request.
        /// </summary>
        public float[] samples;

        public int maxTime;

        public NewSegmentTranscribed callback;
    }
}