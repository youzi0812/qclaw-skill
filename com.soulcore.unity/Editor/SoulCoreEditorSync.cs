using SoulCore;
using UnityEditor;

namespace SoulCore.Editor
{
    [InitializeOnLoad]
    public static class SoulCoreEditorSync
    {
        static SoulCoreEditorSync()
        {
            EditorApplication.playModeStateChanged += _onPlayMode;
            SoulCoreEditorI18n.ApplyEditorLangToPlayerPrefs();
        }

        private static void _onPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                SoulCoreEditorI18n.ApplyEditorLangToPlayerPrefs();
        }
    }
}
