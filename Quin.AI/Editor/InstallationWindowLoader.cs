using UnityEditor;

namespace LeastSquares.Undertone
{
    [InitializeOnLoad]
    public class InstallationWindowLoader
    {
        private const string ProjectOpenedKey = "ProjectOpened";

        static InstallationWindowLoader()
        {
            EditorApplication.delayCall += ShowCustomMenuWindow;
        }

        private static void ShowCustomMenuWindow()
        {
            var value = EditorPrefs.GetBool(ProjectOpenedKey);
            if (!value)
            {
                UndertoneGettingStartedWindow.ShowWindow();
                EditorPrefs.SetBool(ProjectOpenedKey, true);
            }
        }
    }
}