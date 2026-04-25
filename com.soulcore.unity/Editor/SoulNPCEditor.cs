using System.Text;
using SoulCore;
using UnityEditor;
using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace SoulCore.Editor
{
    [CustomEditor(typeof(SoulNPC))]
    [CanEditMultipleObjects]
    public class SoulNPCEditor : UnityEditor.Editor
    {
        private const string _foldKey = "SoulCore.SoulNPCEditor.runtimeFoldout";

        private enum _ProjectTier { Free, Standard, Pro }

        private static _ProjectTier _projectTier()
        {
#if UNITY_2021_2_OR_NEWER
            var s = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (!string.IsNullOrEmpty(s))
            {
                if (s.IndexOf("SOUL_EDITION_FREE", System.StringComparison.Ordinal) >= 0) return _ProjectTier.Free;
                if (s.IndexOf("SOUL_EDITION_PRO", System.StringComparison.Ordinal) >= 0) return _ProjectTier.Pro;
            }
            return _ProjectTier.Standard;
#else
            return _ProjectTier.Standard;
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var tier = _projectTier();
            if (tier == _ProjectTier.Free)
            {
                EditorGUILayout.HelpBox(
                    "当前工程为「免费版」编译 (SOUL_EDITION_FREE)：同屏最多 10 个 SoulNPC；无快照存档。上架标准包前请用 魂核/产品版本/切换为 标准版 再导出。",
                    MessageType.Warning);
            }
            else if (tier == _ProjectTier.Pro)
            {
                EditorGUILayout.HelpBox(
                    "当前工程为「高级版 6.1.8」编译 (SOUL_EDITION_PRO)：包含标准版能力，并可启用开放式智能体 API 配置。",
                    MessageType.Info);
            }
            _drawEngineBanner();
            if (targets.Length > 1)
            {
                DrawDefaultInspector();
                EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.inspect.multi"), MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var p = serializedObject;
            EditorGUILayout.PropertyField(p.FindProperty("m_Script"), true);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.identity"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.npc.nameHint"), MessageType.Info);
            EditorGUILayout.PropertyField(p.FindProperty("_citizenId"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.citizenId")));
            EditorGUILayout.PropertyField(p.FindProperty("_displayName"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.displayName")));
            EditorGUILayout.PropertyField(p.FindProperty("_randomSeed"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.randomSeed")));
            var cfgProp = p.FindProperty("_config");
            EditorGUILayout.PropertyField(cfgProp, new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.config")));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建并绑定 SoulNPCConfig"))
            {
                var path = AssetDatabase.GenerateUniqueAssetPath("Assets/SoulNPCConfig.asset");
                var a = ScriptableObject.CreateInstance<SoulNPCConfig>();
                AssetDatabase.CreateAsset(a, path);
                AssetDatabase.SaveAssets();
                cfgProp.objectReferenceValue = a;
                serializedObject.ApplyModifiedProperties();
                Selection.activeObject = a;
                EditorGUIUtility.PingObject(a);
            }
            using (new EditorGUI.DisabledScope(cfgProp.objectReferenceValue == null))
            {
                if (GUILayout.Button("选中配置资源"))
                {
                    Selection.activeObject = cfgProp.objectReferenceValue;
                    EditorGUIUtility.PingObject(cfgProp.objectReferenceValue);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (cfgProp.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    SoulCoreEditorI18n.UseChinese
                        ? "已指定 Config 时，辅文案以该资源内「flavorOutput」为准，下方选项仅作无 Config 时预览。"
                        : "When a Config asset is assigned, use its `flavorOutput` field. The field below is disabled (preview when no config).",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.openAgent"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.npc.openAgentHint"), MessageType.Info);
            EditorGUILayout.PropertyField(p.FindProperty("_openAgentOverride"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.openAgentOverride")));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.flavorHeader"), EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(cfgProp.objectReferenceValue != null))
            {
                var flavorProp = p.FindProperty("_flavorOutput");
                var flavorLabels = new[]
                {
                    SoulCoreEditorI18n.Ed("ed.npc.flavorEnumBuiltIn"),
                    SoulCoreEditorI18n.Ed("ed.npc.flavorEnumCustom")
                };
                EditorGUI.BeginChangeCheck();
                var n = EditorGUILayout.Popup(
                    new GUIContent(
                        SoulCoreEditorI18n.Ed("ed.npc.flavorMode"),
                        SoulCoreEditorI18n.Ed("ed.npc.flavorModeTip")),
                    flavorProp.enumValueIndex,
                    flavorLabels);
                if (EditorGUI.EndChangeCheck()) flavorProp.enumValueIndex = n;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.day"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindProperty("_secondsPerGameDay"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.secondsPerDay")));
            EditorGUILayout.PropertyField(p.FindProperty("_runDailyAutoReset"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.runDaily")));
            EditorGUILayout.PropertyField(p.FindProperty("_dailyResetOnlyInRange"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.dailyRange")));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.res"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindProperty("_resourceScarcity"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.resourceScarcity")));
            EditorGUILayout.PropertyField(p.FindProperty("_useScarcitySliderAsProvider"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.useScarcityProvider")));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.perf"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindProperty("_maxDistanceMeters"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.maxDist")));
            EditorGUILayout.PropertyField(p.FindProperty("_focusTarget"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.focus")));
            EditorGUILayout.PropertyField(p.FindProperty("_maxPerceptionsPerSecond"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.maxPerSec")));
            EditorGUILayout.PropertyField(p.FindProperty("_enforceProcessingBudget"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.enforceBudget")));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.sched"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindProperty("_processScheduler"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.processScheduler")));
            EditorGUILayout.PropertyField(p.FindProperty("_autoEnsureScheduler"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.autoEnsure")));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.npc.header.debug"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p.FindProperty("_showDebug"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.showDebug")));
            EditorGUILayout.PropertyField(p.FindProperty("_debugStatus"), new GUIContent(SoulCoreEditorI18n.Ed("ed.npc.debugStatus")));

            serializedObject.ApplyModifiedProperties();
            _drawRuntime();
        }

        private void _drawEngineBanner()
        {
            var ch = SoulCoreEngineInfo.Channel;
            var ver = Application.unityVersion;
            if (ch == SoulCoreChannel.TuanjieEditor)
            {
                EditorGUILayout.HelpBox(
                    SoulCoreEditorI18n.Ed("ed.banner.tj.title") + "\n" +
                    string.Format(SoulCoreEditorI18n.Ed("ed.banner.tj.body"),
                        SoulCoreEngineInfo.TuanjieProductTested,
                        SoulCoreEngineInfo.TuanjieProductRecommended,
                        SoulCoreEngineInfo.PackageMinimumUnity) +
                    "\nEditor: " + ver,
                    MessageType.Info);
            }
            else if (ch == SoulCoreChannel.UnityEditorInternational)
            {
                EditorGUILayout.HelpBox(
                    SoulCoreEditorI18n.Ed("ed.banner.unity.title") + "\n" +
                    string.Format(SoulCoreEditorI18n.Ed("ed.banner.unity.body"), SoulCoreEngineInfo.PackageMinimumUnity) +
                    "\nEditor: " + ver,
                    MessageType.Info);
            }
            else
                EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.banner.unknown"), MessageType.Warning);
        }

        private void _drawRuntime()
        {
            EditorGUILayout.Space();
            var fold = SessionState.GetBool(_foldKey, true);
            fold = EditorGUILayout.Foldout(fold, SoulCoreEditorI18n.Ed("ed.inspect.runtimeFold"), true);
            SessionState.SetBool(_foldKey, fold);
            if (!fold) return;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(SoulCoreEditorI18n.Ed("ed.inspect.playHint"), MessageType.Info);
                return;
            }

            var npc = (SoulNPC)target;
            if (npc.IsBlockedByFreeEdition)
            {
                EditorGUILayout.HelpBox(
                    "该对象因「免费版」同屏 SoulNPC 上限 (10) 未初始化 Soul；请减少数量或升级标准版。",
                    MessageType.Warning);
                return;
            }
            if (npc.Soul == null)
            {
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.soulNull"));
                return;
            }

            var s = npc.Soul;
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.id"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.idField"), s.Id);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.name"), s.Name);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(SoulCoreEditorI18n.Ed("ed.inspect.alive"), s.IsAlive);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.stats"), EditorStyles.boldLabel);
                s.Stats.TryGetValue("experience_count", out var exp);
                s.Stats.TryGetValue("decision_count", out var dec);
                s.Stats.TryGetValue("daily_interactions", out var day);
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.exp"), exp.ToString());
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.dec"), dec.ToString());
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.day"), day.ToString());

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.emoMem"), EditorStyles.boldLabel);
                EditorGUILayout.TextField(SoulCoreEditorI18n.Ed("ed.inspect.domEmo"), s.Emotion.GetDominant());
                s.Memory.Stats.TryGetValue("total", out var mTotal);
                s.Memory.Stats.TryGetValue("forgotten", out var mForg);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.memCount"), mTotal);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.memForg"), mForg);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.pool"), s.Memory.PooledMemoryCount);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.memMax"), s.Memory.MaxMemories);
                s.Relationship.Stats.TryGetValue("total_changes", out var relC);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.relCh"), relC);
                s.Relationship.Stats.TryGetValue("threshold_triggers", out var relT);
                EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.relTh"), relT);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.last"), EditorStyles.boldLabel);
            var d = npc.LastDecision;
            if (d == null) EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.none"));
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(SoulCoreEditorI18n.Ed("ed.inspect.action"), d.Action);
                    var ex = d.Explanation;
                    if (ex != null && ex.Length > 200) ex = ex.Substring(0, 200) + "…";
                    EditorGUILayout.TextField(SoulCoreEditorI18n.Ed("ed.inspect.expl"), ex ?? "");
                    EditorGUILayout.TextField(SoulCoreEditorI18n.Ed("ed.inspect.domEmo"), d.Emotion);
                    EditorGUILayout.FloatField(SoulCoreEditorI18n.Ed("ed.inspect.emoInt"), d.EmotionIntensity);
                }
            }
            if (!string.IsNullOrEmpty(npc.LastSkipReason))
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.skip"), npc.LastSkipReason);

            var sch = SoulProcessScheduler.Instance;
            if (sch != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(SoulCoreEditorI18n.Ed("ed.inspect.sched"), EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.queue"), sch.QueuedCount);
                    EditorGUILayout.IntField(SoulCoreEditorI18n.Ed("ed.inspect.perFrame"), sch.MaxPerceptionsPerFrame);
                }
            }

            if (GUILayout.Button(SoulCoreEditorI18n.Ed("ed.inspect.copy")))
            {
                var sb = new StringBuilder();
                sb.AppendLine(s.Explain());
                if (d != null)
                {
                    sb.AppendLine("---");
                    sb.AppendLine(d.Action);
                    sb.AppendLine(d.Explanation ?? "");
                }
                EditorGUIUtility.systemCopyBuffer = sb.ToString();
            }
        }
    }
}
