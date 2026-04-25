using System;
using System.Collections.Generic;

namespace SoulCore.Advanced
{
    [Serializable]
    public enum AdvancedIntentKind
    {
        None = 0,
        Socialize = 1,
        Rest = 2,
        Work = 3,
        Solitude = 4
    }

    [Serializable]
    public struct AdvancedIntent
    {
        public AdvancedIntentKind Kind;
        public float Score;
        public string Reason;
    }

    public sealed class AdvancedNpcState
    {
        public SoulNPC Npc;
        /// <summary>键名见 <see cref="AdvancedDriveKeys"/>。</summary>
        public Dictionary<string, float> Drives = new();
        /// <summary>键名见 <see cref="AdvancedSignalKeys"/>。</summary>
        public Dictionary<string, float> Signals = new();
        public AdvancedIntent Intent;
    }

    public interface IAdvancedModule
    {
        string ModuleId { get; }
        void Tick(AdvancedNpcState state, float dt);
    }
}
