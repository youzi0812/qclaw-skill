using UnityEditor;
using UnityEngine;

namespace SoulCore.Editor
{
    public class SoulPersonalityProfileWindow : EditorWindow
    {
        private SoulPersonalityProfile _profile;
        private SoulNPCConfig _ownerConfig;
        private SerializedObject _so;
        private Vector2 _scroll;
        private bool _isTemporaryProfile;

        public static void Open(SoulPersonalityProfile profile, SoulNPCConfig ownerConfig = null)
        {
            var w = GetWindow<SoulPersonalityProfileWindow>("人格细化面板");
            w.minSize = new Vector2(420f, 540f);
            w._ownerConfig = ownerConfig;
            if (profile != null)
            {
                w._profile = profile;
                w._isTemporaryProfile = false;
                w._so = new SerializedObject(profile);
            }
            else
            {
                w._profile = CreateInstance<SoulPersonalityProfile>();
                w._profile.name = "临时人格配置";
                if (ownerConfig != null && ownerConfig.inlinePersonalityEnabled)
                {
                    ownerConfig.FillProfileFromInline(w._profile);
                }
                w._isTemporaryProfile = true;
                w._so = new SerializedObject(w._profile);
            }
            w.Show();
            w.Focus();
        }

        public static SoulPersonalityProfile CreateProfileAssetInteractive()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "新建人格个性化资产",
                "SoulPersonalityProfile",
                "asset",
                "选择保存位置");
            if (string.IsNullOrEmpty(path)) return null;
            var asset = CreateInstance<SoulPersonalityProfile>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
            return asset;
        }

        private void OnGUI()
        {
            if (_profile == null)
            {
                EditorGUILayout.HelpBox("人格配置为空，请重新打开窗口。", MessageType.Warning);
                return;
            }

            if (_so == null || _so.targetObject != _profile) _so = new SerializedObject(_profile);
            _so.Update();

            var title = _isTemporaryProfile ? "正在编辑：临时人格（未绑定）" : $"正在编辑：{_profile.name}";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (_isTemporaryProfile)
            {
                EditorGUILayout.HelpBox("当前为临时人格。可直接调细节，完成后点“保存并绑定到当前配置”。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用预设并绑定：热心型")) ApplyPresetAndBind(true);
            if (GUILayout.Button("应用预设并绑定：谨慎型")) ApplyPresetAndBind(false);
            if (!_isTemporaryProfile && GUILayout.Button("选中资产")) Selection.activeObject = _profile;
            EditorGUILayout.EndHorizontal();

            if (_isTemporaryProfile)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("绑定个性化"))
                {
                    SaveAndBindTemporaryProfile();
                }
                if (GUILayout.Button("仅保存为资产"))
                {
                    SaveTemporaryProfileAsset(bindToOwner: false);
                }
                EditorGUILayout.EndHorizontal();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawGroup("基础风格", "warmth", "compassion", "trust", "optimism");
            DrawGroup("行为倾向", "courage", "curiosity", "selfishness", "fearTendency");
            DrawGroup("表达风格", "patience", "rationality", "creativity", "persistence");
            EditorGUILayout.EndScrollView();

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_profile);
        }

        private void SaveAndBindTemporaryProfile()
        {
            var created = SaveTemporaryProfileAssetAutoBind();
            if (created == null) return;
            _profile = created;
            _isTemporaryProfile = false;
            _so = new SerializedObject(_profile);
            Repaint();
        }

        private SoulPersonalityProfile SaveTemporaryProfileAssetAutoBind()
        {
            if (_ownerConfig == null)
            {
                EditorUtility.DisplayDialog("提示", "当前窗口未关联 SoulNPCConfig，无法自动绑定。", "确定");
                return null;
            }
            _ownerConfig.ApplyInlinePersonalityFromProfile(_profile);
            _ownerConfig.personalityProfile = null;
            EditorUtility.SetDirty(_ownerConfig);
            AssetDatabase.SaveAssets();
            Selection.activeObject = _ownerConfig;
            return _profile;
        }

        private SoulPersonalityProfile SaveTemporaryProfileAsset(bool bindToOwner)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "保存人格个性化资产",
                "SoulPersonalityProfile",
                "asset",
                "选择保存位置");
            if (string.IsNullOrEmpty(path)) return null;

            var asset = CreateInstance<SoulPersonalityProfile>();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_profile), asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;

            if (bindToOwner && _ownerConfig != null)
            {
                _ownerConfig.personalityProfile = asset;
                EditorUtility.SetDirty(_ownerConfig);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }

        private void DrawGroup(string title, params string[] props)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            for (var i = 0; i < props.Length; i++)
            {
                var p = _so.FindProperty(props[i]);
                if (p == null) continue;
                EditorGUILayout.Slider(p, 0f, 1f, LocalLabel(props[i]));
            }
        }

        private static GUIContent LocalLabel(string prop)
        {
            return prop switch
            {
                "warmth" => new GUIContent("温暖度"),
                "compassion" => new GUIContent("同理心"),
                "trust" => new GUIContent("信任倾向"),
                "optimism" => new GUIContent("乐观度"),
                "courage" => new GUIContent("勇敢度"),
                "curiosity" => new GUIContent("好奇度"),
                "selfishness" => new GUIContent("自保/自利倾向"),
                "fearTendency" => new GUIContent("恐惧倾向"),
                "patience" => new GUIContent("耐心"),
                "rationality" => new GUIContent("理性"),
                "creativity" => new GUIContent("创造性"),
                "persistence" => new GUIContent("坚持度"),
                _ => new GUIContent(prop)
            };
        }

        private void ApplyPreset(bool kind)
        {
            if (_profile == null) return;
            _so.Update();
            if (kind)
            {
                Set("warmth", 0.85f); Set("compassion", 0.88f); Set("trust", 0.72f); Set("optimism", 0.7f);
                Set("courage", 0.56f); Set("curiosity", 0.6f); Set("selfishness", 0.2f); Set("fearTendency", 0.24f);
                Set("patience", 0.7f); Set("rationality", 0.55f); Set("creativity", 0.52f); Set("persistence", 0.64f);
            }
            else
            {
                Set("warmth", 0.45f); Set("compassion", 0.5f); Set("trust", 0.4f); Set("optimism", 0.42f);
                Set("courage", 0.4f); Set("curiosity", 0.36f); Set("selfishness", 0.62f); Set("fearTendency", 0.68f);
                Set("patience", 0.62f); Set("rationality", 0.76f); Set("creativity", 0.32f); Set("persistence", 0.58f);
            }
            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_profile);
        }

        private void ApplyPresetAndBind(bool kind)
        {
            ApplyPreset(kind);
            if (_isTemporaryProfile)
            {
                SaveAndBindTemporaryProfile();
                return;
            }

            if (_ownerConfig != null)
            {
                _ownerConfig.personalityProfile = _profile;
                _ownerConfig.inlinePersonalityEnabled = false;
                EditorUtility.SetDirty(_ownerConfig);
                AssetDatabase.SaveAssets();
                Selection.activeObject = _ownerConfig;
            }
        }

        private void Set(string prop, float value)
        {
            var p = _so.FindProperty(prop);
            if (p != null) p.floatValue = value;
        }
    }
}
