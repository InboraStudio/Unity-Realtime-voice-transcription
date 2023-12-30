using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LeastSquares.Undertone
{
	///<summary>
	///Delegate for when text is transcribed
	///</summary>
	public delegate void TextTranscribed(string text);
	
	///<summary>
	///Class for real-time transcription of audio input
	///</summary>
	public class RealtimeTranscriber : MonoBehaviour
	{
		public SpeechEngine Engine;
		public event TextTranscribed OnTextTranscribed;
		[SerializeField] public float InitialStepSizeInSeconds = 1.5f;
		[SerializeField] public bool AutoAdjustStep = true;
		[SerializeField] public float MaxWindowLengthInSecs = 12;
		[SerializeField] public bool WriteTimestamps = true;
		[SerializeField] public float VADThreshold = 0.004f; // Voice Activity Detection threshold
		[SerializeField] public int VADWindows = 3; // Number of windows to process after VAD is triggered

		private int _windowLengthInSecs;
		private int _keepLength;
		private int _windowLength;
		private int _stepSizeInSamples;
		private int _initialStep;
		
		private bool _isListening;
		private AudioClip _recordedClip;
		private int _lastSamplePosition;
		private float[] _samplesBuffer;
		private int _runningOffset;
		private string _alreadyTranscribedText;
		private readonly List<float> _prevSamples = new List<float>();

		private void Start()
		{
			_initialStep = (int)(SpeechEngine.SampleFrequency * InitialStepSizeInSeconds);
			_stepSizeInSamples = _initialStep;
			RecalculateWindowSize();
			_keepLength = (int)(0.5f * SpeechEngine.SampleFrequency);
			StartCoroutine(TranscribeCoroutine());
		}

		public bool VADTriggered (float[] samples)
		{
			float sum = 0;
			for (int i = 0; i < samples.Length; i++)
			{
				sum += Mathf.Abs(samples[i]);
			}
			float average = sum / samples.Length;
			if (average > VADThreshold) Debug.Log("VAvg:"+average.ToString());
			return average > VADThreshold;
		}

		///<summary>
		///Coroutine for transcription of audio input
		///</summary>
		private IEnumerator TranscribeCoroutine()
		{
			int process_count = 0;
			while (true)
			{
				if (!_isListening)
				{
					yield return null;
					continue;
				}

				var currentPosition = Microphone.GetPosition(null);
				if (currentPosition > _stepSizeInSamples + _lastSamplePosition || currentPosition < _lastSamplePosition && currentPosition + _recordedClip.samples - _lastSamplePosition > _stepSizeInSamples)
				{
					_recordedClip.GetData(_samplesBuffer, _lastSamplePosition);
					var samples = _prevSamples.Concat(_samplesBuffer).ToArray();
					bool flush = samples.Length >= _windowLength;

					// if we hear something, we process the next few seconds
					if (VADTriggered(samples)) process_count = VADWindows;
					else process_count--;

					if (process_count > 0)
					{
						async void Task() => await TranscribeAudioClipAsync(samples, flush);

						System.Threading.Tasks.Task.Run(Task);
					}
					else
					{
						process_count=0;
					}


					
					_lastSamplePosition = (_lastSamplePosition + _samplesBuffer.Length) % _recordedClip.samples;
					_prevSamples.AddRange(_samplesBuffer);
					// Flush
					if (flush)
					{
						_prevSamples.RemoveRange(0, _prevSamples.Count - _keepLength);
					}
					
					if (_prevSamples.Count > _windowLength)
					{
						var offset = _prevSamples.Count - _windowLength;
						_prevSamples.RemoveRange(0, offset);
					}
				}

				yield return null;
			}
		}

		///<summary>
		///Starts recording audio input
		///</summary>
		public void StartListening()
		{
			if (_isListening) return;
			
			Debug.Log("Recording started");
			_isListening = true;
			_lastSamplePosition = 0;
			_alreadyTranscribedText = string.Empty;
			_prevSamples.Clear();
			_recordedClip = Microphone.Start(null, true, _windowLengthInSecs * 2, SpeechEngine.SampleFrequency);
		}

		///<summary>
		///Stops recording audio input
		///</summary>
		public void StopListening()
		{
			Debug.Log("Recording stopped");
			_isListening = false;
			Microphone.End(null);
		}

		///<summary>
		///Transcribes audio clip asynchronously
		///</summary>
		///<param name="samples">Array of audio samples</param>
		///<param name="flush">Whether to flush the transcription</param>
		private async Task TranscribeAudioClipAsync(float[] samples, bool flush)
		{
			var segments = await Engine.TranscribeSamples(samples);
			var maxEndTime = 0;
			var text = string.Join("\n", segments.Select(S =>
			{
				var span = TimeSpan.FromMilliseconds(S.t0 + _runningOffset);
				var time = $"{span.Minutes:D2}:{span.Seconds:D2}";
				maxEndTime = (int)Math.Max(maxEndTime, S.t1);
				var timestamp = WriteTimestamps ? $"[{time}] " : string.Empty;
				return $"{timestamp}{S.text}";
			}).ToArray());
			
			OnTextTranscribed?.Invoke(_alreadyTranscribedText + text);
			
			if (flush)
			{
				_alreadyTranscribedText += $"{text}\n";
				_runningOffset += maxEndTime;
			} ;
			
			if (AutoAdjustStep && Engine.LastTranscriptionTime / 1000f > (_stepSizeInSamples / (float)SpeechEngine.SampleFrequency))
			{
				_stepSizeInSamples = (int) (((Engine.LastTranscriptionTime + 100) / 1000f) * SpeechEngine.SampleFrequency);
				RecalculateWindowSize();
				Debug.Log($"Step size adjusted to {_stepSizeInSamples} samples");
			}
		}

		private void RecalculateWindowSize()
		{
			_samplesBuffer = new float[_stepSizeInSamples];
			_windowLengthInSecs = (int)(MaxWindowLengthInSecs - InitialStepSizeInSeconds);
			_windowLength = (int)(_windowLengthInSecs * SpeechEngine.SampleFrequency);
		}

		///<summary>
		///Stops recording audio input when the object is destroyed
		///</summary>
		private void OnDestroy()
		{
			if(_isListening)
				StopListening();
		}
	}
}