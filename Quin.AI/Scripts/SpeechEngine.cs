using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LeastSquares.Neural;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LeastSquares.Undertone
{
    public delegate void NewSegmentTranscribed(SpeechSegment NewSegment);
    public class SpeechEngine : MonoBehaviour
    {
        class TranscriptionState
        {
            public int last;
            public NewSegmentTranscribed callback;
            public string text;
            public float maxTime;
        }
        
        public const int SampleFrequency = 16000;
        
        private static readonly Dictionary<int, TranscriptionState> _state = new Dictionary<int, TranscriptionState>();
        private static int _id;
        
        public string SelectedModel = "whisper-tiny.en";
        public string SelectedLanguage = "en";
        public bool TranslateToEnglish;
        public int NumOfBeams = 3;
        public bool Verbose;
        public long LastTranscriptionTime;
        
        private NeuralModel _model;
        private NeuralEngine _engine;
        private Queue<TranscriptionRequest> _transcriptionQueue;
        private bool _isTranscribing;
        private bool _shouldFree;
        private string _language;
        private string _initialPrompt;
        private readonly object _lock = new object();
        public bool Loaded { get; private set; }

        // Loads the speech model
        IEnumerator LoadModel()
        {
            // Initialize the transcription queue
            _transcriptionQueue = new Queue<TranscriptionRequest>();
            if (string.IsNullOrEmpty(SelectedModel))
            {
                Debug.LogError("Model is null. Please select or download a model.");
                yield break;
            }
            
            if (Loaded)
            {
                LogIfVerbose("Model already loaded");
                yield break;
            }

            Debug.Log(
                $"Loading model with lang: {SelectedLanguage}, model: {SelectedModel}, translate: {TranslateToEnglish}");
            
            lock (_lock)
            {
                _engine = new NeuralEngine();
                yield return ModelManager.Load(_engine, SelectedModel, model =>
                {
                    _model = model;
                });
                
                if (_model == null)
                {
                    Debug.LogError("Failed to load speech model. Please check the selected model is downloaded.");
                    yield break;
                }
                SetLanguage();
            }

            
            Loaded = true;
            
            if (_model != null)
                Debug.Log("Loaded model correctly");
        }
        
        private static void OnSegmentCallback(int user_data, SpeechSegment segment)
        {
            var id = (int)user_data;
            var transcriptionState = _state[id];
            transcriptionState.text += segment.text;
            transcriptionState.callback?.Invoke(segment);
        }

        private void SetLanguage()
        {
            var isSimplifiedChinese = SelectedLanguage == "zh-Hans";
            var isTraditionalChinese = SelectedLanguage == "zh-Hant";
            _language = isSimplifiedChinese || isTraditionalChinese ? "zh" : SelectedLanguage;
            if (isTraditionalChinese)
            {
                _initialPrompt = "以下是普通話的句子。";
            }
            else if(isSimplifiedChinese)
            {
                _initialPrompt = "以下是普通话的句子。";
            }
        }


        // Called when the script instance is being loaded
        void Awake()
        {
            StartCoroutine(LoadModel());
            StartCoroutine(TranscriptionCoroutine());
        }

        // A coroutine for transcribing audio
        private IEnumerator TranscriptionCoroutine()
        {
            while (!_shouldFree)
            {
                if (_transcriptionQueue.Count > 0 && !_isTranscribing)
                {
                    _isTranscribing = true;
                    LogIfVerbose($"Elements in queue: {_transcriptionQueue.Count}");
                    var request = _transcriptionQueue.Dequeue();

                    async void Task() => await TranscribeAndReset(request);

                    System.Threading.Tasks.Task.Run(Task);
                }
                
                yield return null;
            }
        }

        public async Task<SpeechSegment[]> TranscribeClip(AudioClip clip, int offset = 0, int length = -1, NewSegmentTranscribed callback = null)
        {
            Debug.Assert(clip.frequency == SampleFrequency, "Whisper needs all samples to be in 16kHz");
            if (length == 0)
                return Array.Empty<SpeechSegment>();
            var sampleLength = length == -1 ? clip.samples : length;
            var buffer = new float[sampleLength * clip.channels];
            if(!clip.GetData(buffer, offset))
                Debug.LogWarning("Failed to retrieve data");
            return await TranscribeSamples(buffer, callback);
        }
        
        // Transcribes an array of audio samples
        public async Task<SpeechSegment[]> TranscribeSamples(float[] samples, NewSegmentTranscribed callback = null)
        {
            if (samples.Length == 0)
                return Array.Empty<SpeechSegment>();

            var tcs = new TaskCompletionSource<SpeechSegment[]>();
            _transcriptionQueue.Enqueue(new TranscriptionRequest
            {
                tcs = tcs,
                samples = samples,
                callback = callback,
                // We assume its mono
                maxTime = (samples.Length / SampleFrequency) * 1000
            });
            return await tcs.Task;
        }

        // Transcribes audio data
        private async Task<SpeechSegment[]> TranscribeAndReset(TranscriptionRequest request)
        {
            SpeechSegment[] result = Array.Empty<SpeechSegment>();
            try
            {
                result = await TranscribeData(request);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to transcribe data: {e}");
            }

            if (_shouldFree)
            {
                Dispose();
            }
            _isTranscribing = false;
            return result;
        }

        // Transcribes audio data
        private Task<SpeechSegment[]> TranscribeData(TranscriptionRequest request)
        {
            var samples = request.samples;
            var id = _id++;
            _state[id] = new TranscriptionState
            {
                maxTime = request.maxTime, 
                callback = request.callback
            };
            var tcs = request.tcs;
  
            if (!Loaded)
            {
                Debug.LogWarning("Tried to translate before loading the speech model.");
                tcs.SetResult(Array.Empty<SpeechSegment>());
                return tcs.Task;
            }

            LogIfVerbose("Started transcription");
            var sw = new Stopwatch();
            sw.Start();
            

            lock (_lock)
            {
                using var data = GenerateInput(_model, samples);
                using var output = _engine.Inference(_model, data);
                if (output == null)
                {
                    Debug.LogError("Failed to transcribe audio file");
                    tcs.SetResult(Array.Empty<SpeechSegment>());
                    return tcs.Task;
                }

                var transcription = output.Tensors[0].Values[0].GetString();
                if (_initialPrompt != null)
                    transcription = transcription.Substring(_initialPrompt.Length);
                var result = new SpeechSegment[]
                {
                    new SpeechSegment()
                    {
                        text = transcription
                    }
                };
                sw.Stop();
                LastTranscriptionTime = sw.ElapsedMilliseconds;
                LogIfVerbose("Transcribed audio file: " + sw.ElapsedMilliseconds + "ms");
                OnSegmentCallback(id, result[0]);
                tcs.SetResult(result.ToArray());
            }


            return tcs.Task;
        }

        private NeuralData GenerateInput(NeuralModel model, float[] samples)
        {
            var wavBytes = AudioUtils.FloatArrayToWavBytes(samples);
            using var data = FixedPointerToHeapAllocatedMem.Create(wavBytes, (uint)(wavBytes.Length));
            using var lang = new FixedString(_language ?? "auto");
            using var task = new FixedString(TranslateToEnglish ? "translate" : "transcribe");
            using var prompt = new FixedString(_initialPrompt ?? "");
            var serialized = NeuralNative.neural_get_whisper_data(
                model.Data,
                data.Address,
                data.SizeInBytes,
                lang.Address, 
                task.Address,
                prompt.Address,
                false,
                200,
                NumOfBeams
            );
            return NeuralData.FromSerialized(serialized);
        }
        
        private void LogIfVerbose(string text)
        {
            if(Verbose)
                Debug.Log(text);
        }

        // Called when the game object is destroyed
        void OnDestroy()
        {
            if (!_isTranscribing)
                Dispose();
            _shouldFree = true;
        }

        // Frees the speech model
        private void Dispose()
        {
            lock (_lock)
            {
                if (_engine == null) return;
                _model.Dispose();
                _engine.Dispose();
                LogIfVerbose("Free'ing speech model");
            }
        }
    }
}