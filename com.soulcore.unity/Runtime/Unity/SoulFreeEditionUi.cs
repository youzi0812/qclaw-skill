using UnityEngine;

namespace SoulCore
{
    public enum SoulFreeEditionNotifyKind
    {
        NpcCountExceeded = 0,
        SnapshotNotAvailable = 1
    }

    /// <summary>免费版：触达限制时弹一次简易提示（无预制体，OnGUI；避免 spam）。</summary>
    [DefaultExecutionOrder(1000)]
    public class SoulFreeEditionUi : MonoBehaviour
    {
        private const string PrefsNpc = "SoulCore.FreeDlg.Npc";
        private const string PrefsSave = "SoulCore.FreeDlg.Save";

        private static SoulFreeEditionUi _instance;
        public static void Notify(SoulFreeEditionNotifyKind kind)
        {
            if (!SoulCoreEdition.IsFree) return;
            if (_instance == null)
            {
                var go = new GameObject("SoulFreeEditionUi");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SoulFreeEditionUi>();
            }
            _instance._queue(kind);
        }

        private SoulFreeEditionNotifyKind? _current;
        private float _until;
        private string _line1;
        private string _line2;

        private void _queue(SoulFreeEditionNotifyKind kind)
        {
            if (kind == SoulFreeEditionNotifyKind.NpcCountExceeded)
            {
                if (PlayerPrefs.GetInt(PrefsNpc, 0) != 0) return;
                PlayerPrefs.SetInt(PrefsNpc, 1);
                PlayerPrefs.Save();
                _line1 = "魂核 免费版：同时激活的 NPC 已达上限（10 个）。";
                _line2 = "标准版将解除限制。请从商店升级或从场景中移除此对象上的 SoulNPC。";
            }
            else
            {
                if (PlayerPrefs.GetInt(PrefsSave, 0) != 0) return;
                PlayerPrefs.SetInt(PrefsSave, 1);
                PlayerPrefs.Save();
                _line1 = "魂核 免费版：不包含「快照存档」能力。";
                _line2 = "请使用标准版，或自研存档逻辑（仅存自家游戏数据）。";
            }
            _current = kind;
            _until = Time.unscaledTime + 9f;
        }

        private void OnGUI()
        {
            if (_current == null) return;
            if (Time.unscaledTime > _until) { _current = null; return; }
            const int w = 500, h = 150;
            var x = (Screen.width - w) * 0.5f;
            var y = (Screen.height - h) * 0.5f;
            var r = new Rect(x, y, w, h);
            GUI.depth = -1000;
            GUI.Box(r, "魂核 · 免费版说明");
            GUILayout.BeginArea(new Rect(x + 12f, y + 28f, w - 24f, h - 40f));
            GUILayout.Label(_line1, GUI.skin.GetStyle("Label"));
            GUILayout.Space(4);
            GUILayout.Label(_line2, GUI.skin.GetStyle("Label"));
            GUILayout.Space(10);
            if (GUILayout.Button("知道了", GUILayout.Height(32)))
                _current = null;
            GUILayout.EndArea();
        }
    }
}
