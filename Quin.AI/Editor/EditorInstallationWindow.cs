using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LeastSquares.Undertone
{

    /// <summary>
    /// A window that provides a guide to get started with Undertone.
    /// </summary>
    public class UndertoneGettingStartedWindow : EditorWindow
    {
        private static bool showOnStart = true;
        private const string showOnStartPrefKey = "UndertoneShowOnStart";
        private Vector2 scrollPosition;

        /// <summary>
        /// Adds a menu item to open the Undertone Getting Started window.
        /// </summary>
        [MenuItem("Window/Undertone/Getting Started")]
        public static void ShowWindow()
        {
            showOnStart = EditorPrefs.GetBool(showOnStartPrefKey, true);
            if (showOnStart)
                GetWindow<UndertoneGettingStartedWindow>("Undertone Getting Started");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical("Box");
            {
                RenderWelcomeLabel();
                RenderSpeechEngineSection();
                RenderModelTypesSection();
                RenderDemoScenesSection();
                RenderDocsAndLinks();
                RenderStartupToggle();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Renders the welcome label at the top of the window.
        /// </summary>
        private void RenderWelcomeLabel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Getting Started with Undertone", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Renders the section for adding a speech engine and downloading a model.
        /// </summary>
        private void RenderSpeechEngineSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Step 1: Add a speech engine and download a model", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "To use Undertone, you need to add a speech engine into your game and download a model. The included 'tiny.en' model can be used to start. The speech engine interface provides buttons to download and select models.",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Renders the section for understanding different model types.
        /// </summary>
        private void RenderModelTypesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Step 2: Understand different model types", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "There are two types of models: multilingual and English-only models. Multilingual models support multiple languages but are generally larger in size. English-only models are smaller and more focused but only support English. Evaluate the pros and cons of each type and choose the one that best fits your project requirements.",
                    MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Renders the section for checking out the demo scenes.
        /// </summary>
        private void RenderDemoScenesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Step 3: Check out the demo scenes", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("The following demo scenes are available for you to try:",
                    MessageType.Info);
                for (int i = 0; i < demoSceneNames.Length; i++)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(demoSceneNames[i].Replace("Assets/Undertone/", ""), GUILayout.Height(25), GUILayout.ExpandWidth(true)))
                    {
                        EditorSceneManager.OpenScene(demoSceneNames[i]);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Renders the section for checking out the documentation and links.
        /// </summary>
        private void RenderDocsAndLinks()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Step 4: Check out the documentation & more", EditorStyles.boldLabel);
                if (GUILayout.Button("Open Documentation"))
                    Application.OpenURL("https://leastsquares.io/docs/unity/undertone");
                if (GUILayout.Button("Join Discord Community"))
                    Application.OpenURL("https://discord.gg/DZpBsTYNPD");
                if (GUILayout.Button("Check Other Assets"))
                    Application.OpenURL("https://assetstore.unity.com/publishers/39777");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Renders the toggle for showing the window on startup.
        /// </summary>
        private void RenderStartupToggle()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Show this window on startup", GUILayout.Width(400));
                showOnStart = EditorGUILayout.Toggle(showOnStart);
                EditorPrefs.SetBool(showOnStartPrefKey, showOnStart);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private static readonly string[] demoSceneNames =
        {
            "Assets/Undertone/Demos/Push to record.unity",
            "Assets/Undertone/Demos/Realtime transcription.unity",
        };
    }
}