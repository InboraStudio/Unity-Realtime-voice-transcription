using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LeastSquares.Undertone
{
    /// <summary>
    /// Class responsible for recording audio and transcribing it using a SpeechEngine.
    /// </summary>
    public class PushToTranscribe : MonoBehaviour
    {
        [SerializeField] public SpeechEngine Engine;
        [SerializeField] public int MaxRecordingTime = 30;
        private AudioClip _clip;

        private void Start()
        {
            StartRecording();
        }

        /// <summary>
        /// Starts recording audio from the microphone.
        /// </summary>
        public void StartRecording()
        {
            _clip = Microphone.Start(null, false, MaxRecordingTime, SpeechEngine.SampleFrequency);
        }

        /// <summary>
        /// Stops recording audio and transcribes it using a SpeechEngine.
        /// </summary>
        /// <returns>A string with the transcribed audio segments and their respective timestamps.</returns>
        public async Task<string> StopRecording()
        {
            var str = Engine.TranscribeClip(_clip, 0, Microphone.GetPosition(null));
            Microphone.End(null);
            var segments = await str;
            return string.Join("\n", segments.Select(S => S.text).ToArray());
        }
    }
}