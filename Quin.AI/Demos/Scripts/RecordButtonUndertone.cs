using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeastSquares.Undertone
{
    /// <summary>
    /// This class represents the record button in the UI and handles the recording and transcribing of audio.
    /// </summary>
    public class RecordButtonUndertone : MonoBehaviour
    {
        public PushToTranscribe _transcriber;
        public RotateBackgroundUndertone _rotate;
        private bool _isRecording;

        public TMP_Text transcriptionText;

        /// <summary>
        /// Adds a listener to the button to call the OnClicked function when clicked.
        /// </summary>
        private void Start()
        {
            var recordButton = GetComponent<Button>();
            recordButton.onClick.AddListener(OnClicked);
        }

        /// <summary>
        /// Handles the logic for when the record button is clicked.
        /// </summary>
        private async void OnClicked()
        {
            var buttonText = GetComponentInChildren<TMP_Text>();
            // If the speech recognition engine is not loaded, do nothing.
            if (!_transcriber.Engine.Loaded) return;

            if (!_isRecording)
            {
                // Start recording and change the button text to "Stop".
                buttonText.text = "Stop".ToUpperInvariant();
                _transcriber.StartRecording();
                _isRecording = true;
                _rotate.speed = 10;
            }
            else
            {
                // Stop recording and transcribe the audio.
                buttonText.text = "Transcribing...".ToUpperInvariant();
                GetComponent<Button>().interactable = false;
                _rotate.speed = 0;
                string transcription = await _transcriber.StopRecording();
                transcriptionText.text = transcription;
                buttonText.text = "Record".ToUpperInvariant();
                GetComponent<Button>().interactable = true;
                _isRecording = false;
            }
        }
    }
}