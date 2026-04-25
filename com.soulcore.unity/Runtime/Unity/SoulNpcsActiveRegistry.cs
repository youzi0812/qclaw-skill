using System.Collections.Generic;
using UnityEngine;

namespace SoulCore
{
    public static class SoulNpcsActiveRegistry
    {
        private static readonly List<SoulNPC> Active = new();

        public static int Count => Active.Count;

        public static bool TryRegister(SoulNPC n)
        {
            if (n == null) return false;
            if (!SoulCoreEdition.IsFree) return true;
            if (Active.Count >= SoulCoreEdition.MaxActiveNpcSlots) return false;
            if (!Active.Contains(n)) Active.Add(n);
            return true;
        }

        public static void Unregister(SoulNPC n)
        {
            if (n == null || !SoulCoreEdition.IsFree) return;
            Active.Remove(n);
        }
    }
}
