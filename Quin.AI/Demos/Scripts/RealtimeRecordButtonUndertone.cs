using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeastSquares.Undertone
{
    public class RealtimeRecordButtonUndertone : MonoBehaviour
    {
        // Reference to the RealtimeTranscriber script
        public RealtimeTranscriber _transcriber;

        // Boolean to keep track of whether the button is recording or not
        private bool _isRecording;

        // Reference to the RotateBackground script
        public RotateBackgroundUndertone _rotate;

        // Reference to the text object that displays the transcription
        public TMP_Text transcriptionText;

        // String to store the transcription text
        private string _text;

        // Function that runs when the script is started
        private void Start()
        {
            // Get a reference to the button component and add a listener for when it is clicked
            var recordButton = GetComponent<Button>();
            recordButton.onClick.AddListener(OnClicked);

            // Add a listener for when text is transcribed
            _transcriber.OnTextTranscribed += text =>
            {
                _text = text;
            };
        }

        // Function that runs every frame
        private void Update()
        {
            // Update the transcription text object with the current transcription text
            transcriptionText.text = _text;
        }

        // Function that runs when the button is clicked
        private void OnClicked()
        {
            // Get a reference to the text object that displays the button text
            var buttonText = GetComponentInChildren<TMP_Text>();

            // If the RealtimeTranscriber script is not loaded, do nothing
            if (!_transcriber.Engine.Loaded) return;

            // If the button is not recording, start recording
            if (!_isRecording)
            {
                buttonText.text = "Stop".ToUpperInvariant();
                _transcriber.StartListening();
                _isRecording = true;
                _rotate.speed = 10;
            }
            // If the button is recording, stop recording and start transcribing
            else
            {
                buttonText.text = "Transcribing...".ToUpperInvariant();
                GetComponent<Button>().interactable = false;
                _transcriber.StopListening();
                buttonText.text = "Listen".ToUpperInvariant();
                GetComponent<Button>().interactable = true;
                _isRecording = false;
                _rotate.speed = 0;
            }
        }
    }
}