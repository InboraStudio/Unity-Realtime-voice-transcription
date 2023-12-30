using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LeastSquares.Undertone
{
    /**
     * This script is an editor script for the SpeechEngine class. It allows the user to manage the available models for the SpeechEngine and download new models from the internet.
     * It also provides a GUI for selecting the model and language to use for speech recognition and translation.
     */
    [CustomEditor(typeof(SpeechEngine))]
    public class SpeechEngineInspector : Editor
    {
        private static long GB = 1024 * 1024 * 1024;
        private List<string> availableModels = new List<string>
        {
            "whisper-tiny.en", "whisper-base.en", "whisper-small.en", "whisper-medium.en",
            "whisper-tiny", "whisper-base", "whisper-small", "whisper-medium",
        };

        private List<string> speeds = new List<string>()
        {
            "fastest", "normal", "slow", "slowest",
            "fastest", "normal", "slow", "slowest"
        };

        private const string DefaultModel = "whisper-tiny.en";
        private const string DefaultTinyModelPath = "/Undertone/Resources/" + DefaultModel + ".bytes";

        private List<string> installedModels;
        private string modelsPath;
        private WebClient webClient;
        private int _currentPercentage;
        private bool isDownloading;
        private SerializedProperty selectedModel;
        private SerializedProperty selectedLanguage;
        private SerializedProperty translateToEnglish;

        /**
         * This method is called when the editor is enabled. It initializes the serialized properties and sets the path to the models directory.
         */
        private void OnEnable()
        {
            selectedModel = serializedObject.FindProperty("SelectedModel");
            selectedLanguage = serializedObject.FindProperty("SelectedLanguage");
            translateToEnglish = serializedObject.FindProperty("TranslateToEnglish");
            modelsPath = ModelManager.GetModelDirectory();
            UpdateInstalledModels();
        }
        
        /**
         * This method updates the list of installed models by searching for all .bytes files in the models directory and adding them to the installedModels list.
         */
        private void UpdateInstalledModels()
        {
            if (!Directory.Exists(modelsPath))
                Directory.CreateDirectory(modelsPath);
            var modelFiles = Directory.GetFiles(modelsPath, "*.bytes");

            installedModels = new List<string>();
 
            foreach (var modelFile in modelFiles)
            {
                var modelName = Path.GetFileNameWithoutExtension(modelFile);
                
                if (availableModels.Contains(modelName))
                {
                    installedModels.Add(modelName);
                }
                else
                {
                    var cleanedModelNameMatch = Regex.Match(modelName, @"^(.+).\d$");
                    if (!cleanedModelNameMatch.Success ||
                        !availableModels.Contains(cleanedModelNameMatch.Groups[1].Value))
                        continue;
                    
                    var cleanedModelName = cleanedModelNameMatch.Groups[1].Value;
                    if (!installedModels.Contains(cleanedModelName))
                        installedModels.Add(cleanedModelName);
                }
            }
            // Tiny.en comes by default
            if (File.Exists($"{Application.dataPath}/{DefaultTinyModelPath}") && !installedModels.Contains(DefaultModel))
                installedModels.Add(DefaultModel);
        }

        private int WrapIndex(int index)
        {
            return Mathf.Clamp(index, 0, installedModels.Count);
        }
        
        /**
         * This method displays the GUI for selecting the model and language to use for speech recognition and translation.
         */
        private void ModelSettingsGUI(SpeechEngine speechEngine)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            if (installedModels.Count == 0)
            {
                EditorGUILayout.HelpBox("Please install a model first.", MessageType.Info);
                return;
            }
            serializedObject.Update();
            
            int selectedModelIndex = installedModels.IndexOf(selectedModel.stringValue);
            int newModelIndex = EditorGUILayout.Popup("Select Model", WrapIndex(selectedModelIndex), installedModels.ToArray());
            if (newModelIndex != selectedModelIndex)
            {
                selectedModel.stringValue = installedModels[newModelIndex];
            }

            var englishModelEnabled = selectedModel.stringValue.EndsWith(".en");
            if (englishModelEnabled)
                selectedLanguage.stringValue = "en";

            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(englishModelEnabled);

            int selectedLanguageIndex = Math.Max(0, Array.IndexOf(availableLanguages, selectedLanguage.stringValue));
            int newLanguageIndex = EditorGUILayout.Popup("Select Language", selectedLanguageIndex, languageNames);
            if (newLanguageIndex != selectedLanguageIndex)
            {
                selectedLanguage.stringValue = availableLanguages[newLanguageIndex];
            }
            EditorGUILayout.PropertyField(translateToEnglish);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);

            speechEngine.NumOfBeams = EditorGUILayout.IntSlider(new GUIContent("Quality", "The quality to use for the transcription. 1 is fastest while 5 provides the highest quality"), speechEngine.NumOfBeams, 1, 5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Verbose"), new GUIContent("Verbose", "Extended logging."));

            serializedObject.ApplyModifiedProperties();
        }

        /**
         * This method is called when the inspector GUI is drawn. It displays the GUI for selecting the model and language and also provides options for downloading and deleting models.
         */
        public override void OnInspectorGUI()
        {
            var speechEngine = (SpeechEngine)target;
            ModelSettingsGUI(speechEngine);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Whisper Models Management", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (isDownloading)
            {
                EditorGUI.ProgressBar(GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true)),
                    _currentPercentage * 0.01f, "Downloading...");
                return;
            }

            DrawEnglishModels();

            EditorGUILayout.Space();

            DrawMultilingualModels();
            
            EditorGUI.EndDisabledGroup();
        }

        /**
         * This method displays the GUI for downloading or deleting an English model.
         */
        private void DrawEnglishModels()
        {
            EditorGUILayout.LabelField("English Models", EditorStyles.boldLabel);
            foreach (string model in availableModels)
            {
                if (!IsEnglishModel(model) || IsQuantizedModel(model)) continue;
                DrawModel(model);
            }
        }

        /**
         * This method displays the GUI for downloading or deleting a multilingual model.
         */
        private void DrawMultilingualModels()
        {
            EditorGUILayout.LabelField("Multilingual Models", EditorStyles.boldLabel);
            foreach (string model in availableModels)
            {
                if (!IsMultilingualModel(model) || IsQuantizedModel(model)) continue;
                DrawModel(model);
            }
        }
        
        private void DrawQuantizedModels()
        {
            EditorGUILayout.LabelField("Quantized Models (Smaller & faster but less precise)", EditorStyles.boldLabel);
            foreach (string model in availableModels)
            {
                if (!IsQuantizedModel(model)) continue;
                DrawModel(model);
            }
        }

        private bool IsEnglishModel(string model) => model.EndsWith(".en");
        
        private bool IsMultilingualModel(string model) => !IsEnglishModel(model);
        
        private bool IsQuantizedModel(string model) => model.Contains("-q");

        /**
         * This method displays the GUI for downloading or deleting a model.
         */
        private void DrawModel(string modelName)
        {
            EditorGUILayout.BeginHorizontal();
        
            bool isInstalled = installedModels.Contains(modelName);
        
            if (isInstalled)
            {
                EditorGUILayout.LabelField(modelName, "Installed");
        
                if (GUILayout.Button("Delete", GUILayout.Width(100)))
                {
                    DeleteModel(modelName);
                    AssetDatabase.Refresh();
                    UpdateInstalledModels();
                }
            }
            else
            {
                EditorGUILayout.LabelField(modelName);
        
                if (GUILayout.Button("Download", GUILayout.Width(100)))
                {
                    string url = $"https://huggingface.co/datasets/leastsquares/undertone/resolve/main/{modelName}.onnx";
                    DownloadModelAsync(url, modelName);
                }
            }
        
            EditorGUILayout.EndHorizontal();
        }

        private void DeleteModel(string modelName)
        {
            // Check if there are chunked files using the pattern '{modelName}.*.bytes'.
            void DeleteChunked(string searchPattern)
            {
                var modelChunks = Directory.GetFiles(modelsPath, searchPattern)
                    .OrderBy(f => f)
                    .ToList();

                if (modelChunks.Count > 0)
                {
                    // If there are chunked files, delete them all.
                    foreach (var chunkFile in modelChunks)
                    {
                        File.Delete(chunkFile);
                    }
                }
            }

            DeleteChunked($"{modelName}.*.bytes");
            DeleteChunked($"{modelName}.*.bytes.meta");
            // Delete the main model file and its meta file.
            File.Delete(Path.Combine(modelsPath, modelName + ".bytes"));
            File.Delete(Path.Combine(modelsPath, modelName + ".bytes.meta"));
            // Tiny.en comes by default
            var defaultModelPath = $"{Application.dataPath}/{DefaultTinyModelPath}";
            if (modelName == DefaultModel && File.Exists(defaultModelPath))
            {
                File.Delete(defaultModelPath);
                File.Delete($"{defaultModelPath}.meta");
            }
        }

        /**
         * This method downloads a model from the internet and saves it to the models directory.
         */
        private async void DownloadModelAsync(string url, string modelName)
        {
            isDownloading = true;
            _currentPercentage = 0;

            long chunkSize = 8 * GB;
            long contentLength;
            using (UnityWebRequest request = UnityWebRequest.Head(url))
            {
                request.SendWebRequest();
                while (!request.isDone)
                {
                    await Task.Delay(50);
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error fetching content length: {request.error}");
                    return;
                }

                contentLength = Convert.ToInt64(request.GetResponseHeader("Content-Length"));
            }

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                var downloadHandler = new ChunkDownloadHandler(modelsPath, modelName, chunkSize, contentLength);
                request.downloadHandler = downloadHandler;
                request.SendWebRequest();

                while (!request.isDone)
                {
                    _currentPercentage = (int)(downloadHandler.GetDownloadProgress() * 100);
                    Repaint();
                    await Task.Delay(100);
                }

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading {modelName}: {request.error}");
                }
                else
                {
                    MergeChunksIfSingle(modelsPath, modelName);

                    AssetDatabase.Refresh();
                    UpdateInstalledModels();
                    EditorUtility.DisplayDialog("Download Complete", $"Successfully downloaded {modelName}.", "OK");
                }

                isDownloading = false;
            }
        }

        private void MergeChunksIfSingle(string folder, string modelName)
        {
            string chunk0Path = GetChunkPath(folder, modelName, 0);
            string chunk1Path = GetChunkPath(folder, modelName, 1);
            if (File.Exists(chunk0Path) && !File.Exists(chunk1Path))
            {
                string mergedFilePath = Path.Combine(folder, $"{modelName}.bytes");
                File.Move(chunk0Path, mergedFilePath);
            }
        }

        private string GetChunkPath(string folder, string modelName, int chunkIndex)
        {
            return $"{folder}{modelName}.{chunkIndex}.bytes";
        }

        private static string[] availableLanguages = Languages.AvailableLanguages;

        private static string[] languageNames = Languages.AvailableLanguagesNames;
    }
    
    public class ChunkDownloadHandler : DownloadHandlerScript
    {
        private string modelsPath;
        private string modelName;
        private int bytesReadTotal = 0;
        private long chunkSize;
        private int chunkIndex = 0;
        private string chunkPath;
        private FileStream output;
        private long totalDownloadedBytes = 0;
        private long contentLength;


        public ChunkDownloadHandler(string modelsPath, string modelName, long chunkSize, long contentLength) : base()
        {
            this.modelsPath = modelsPath;
            this.modelName = modelName;
            this.chunkSize = chunkSize;
            this.contentLength = contentLength;

            chunkPath = GetChunkPath(modelsPath, modelName, chunkIndex);
            output = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || data.Length < 1)
            {
                return false;
            }

            output.Write(data, 0, dataLength);
            bytesReadTotal += dataLength;
            totalDownloadedBytes += dataLength;
            
            if (bytesReadTotal >= chunkSize)
            {
                bytesReadTotal = 0;
                chunkIndex++;
                chunkPath = GetChunkPath(modelsPath, modelName, chunkIndex);
                output.Dispose();
                output = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
            }

            return true;
        }

        protected override void CompleteContent()
        {
            output.Dispose();
        }

        public float GetDownloadProgress()
        {
            return (float)totalDownloadedBytes / contentLength;
        }
        
        private string GetChunkPath(string folder, string modelName, int chunkIndex)
        {
            return $"{folder}{modelName}.{chunkIndex}.bytes";
        }
    }
}