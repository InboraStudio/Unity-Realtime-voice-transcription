using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LeastSquares.Neural;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LeastSquares.Undertone
{
    public class ModelManager
    {
        public static string GetModelDirectory(string modelName = null)
        {
            return Application.streamingAssetsPath + "/Undertone/" + (modelName != null ? $"{modelName}.bytes" : string.Empty);
        }
        
        /// <summary>
        /// Loads a model from the Resources folder and returns a fixed pointer to the heap-allocated memory.
        /// </summary>
        /// <param name="modelName">The name of the model to be loaded.</param>
        /// <returns>A fixed pointer to the heap-allocated memory containing the model data.</returns>
        public static FixedMemoryBlock LoadModelFromResources(string modelName)
        {
            Debug.Log($"Loading model '{modelName}' from resources");
            var textAsset = Resources.Load<TextAsset>($"{modelName}");
            var files = new List<TextAsset>();
            if (textAsset != null)
            {
                files.Add(textAsset);
            }
            else
            {
                
                var partitionIndex = 0;
                var partitionedFile = Resources.Load<TextAsset>($"{modelName}.{partitionIndex}");
                while (partitionedFile != null)
                {
                    files.Add(partitionedFile);
                    partitionIndex++;
                    partitionedFile = Resources.Load<TextAsset>($"{modelName}.{partitionIndex}");
                }

                if (files.Count == 0)
                {
                    Debug.LogError($"Failed to find model '{modelName}' in the Resources folder.");
                    return null;
                }
            }
            
            var totalLength = files.Sum(f => (long) f.bytes.Length);
            var memoryBlock = FixedMemoryBlock.Create(totalLength);
            var offset = 0;
            var baseAddr = memoryBlock.Address.ToInt64();
            foreach (var fileBytes in files.Select(pf => pf.bytes))
            {
                var chunkSize = 4096 * 2;
                var subOffset = 0;
                while(subOffset < fileBytes.Length)
                {
                    var size = Math.Min(chunkSize, fileBytes.Length - subOffset);
                    Marshal.Copy(fileBytes, subOffset, new IntPtr(baseAddr + offset + subOffset), size);
                    subOffset += size;
                }
                offset += fileBytes.Length;
            }

            return memoryBlock;
        }
        
        public static IEnumerator ReadFileWebRequestCoroutine(string path, Action<byte[]> callback)
        {
            using (var request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error while loading model '{path}'.");
                    if (!string.IsNullOrEmpty(request.error))
                        Debug.LogError(request.error);

                    callback(null);
                }
                else
                {
                    callback(request.downloadHandler.data);
                }
            }
        }

        private static IEnumerator LoadModelFromStreamingAssetsEmbedded(string selectedModel, Action<FixedMemoryBlock> callback)
        {
            yield return ReadFileWebRequestCoroutine(GetModelDirectory(selectedModel), bytes =>
            {
                if (bytes == null)
                {
                    Debug.LogError($"Failed to load model '{selectedModel}' from streaming assets for embedded device.");
                    callback(null);
                    return;
                }
                var modelPtr = FixedMemoryBlock.Create(bytes.Length);
                Marshal.Copy(bytes, 0, modelPtr.Address, bytes.Length);
                callback(modelPtr);
            });
        }

        public static IEnumerator Load(NeuralEngine engine, string selectedModel, Action<NeuralModel> callback)
        {
            NeuralModel context;
            var path = GetModelDirectory(selectedModel);
            var isEmbedded = false;
            Debug.Log($"Is embedded: {isEmbedded}");
            Debug.Log($"Looking for file {path}");
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            isEmbedded = true;
#endif
            if (isEmbedded || !File.Exists(path))
            {
                Debug.Log("Loading model from memory...");
                FixedMemoryBlock model = null;
                if (isEmbedded)
                {
                    yield return LoadModelFromStreamingAssetsEmbedded(selectedModel, m => model = m);
                }

                if (model == null) 
                    model = LoadModelFromResources(selectedModel);

                if (model == null)
                {
                    callback(null);
                    yield break;
                }

                context = NeuralModel.FromMemory(engine, model.Address, (uint)model.SizeInBytes);
                model.Dispose();
            }
            else
            {
                Debug.Log("Loading model from file...");
                context = NeuralModel.FromFile(engine, path);
            }

            var isMultilingual = !selectedModel.EndsWith(".en");
            var vocab = Resources.Load<TextAsset>($"vocab{(isMultilingual ? "" : ".en")}");
            var vocabBytes = vocab.bytes;
            using var vocabPtr = FixedPointerToHeapAllocatedMem.Create(vocabBytes, (uint)vocabBytes.Length);
            NeuralNative.neural_load_whisper_tokenizer_from_memory(context.Data, vocabPtr.Address, vocabBytes.Length);

            callback(context);
        }
    }
}