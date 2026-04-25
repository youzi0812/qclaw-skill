namespace SoulCore
{
    /// <summary>
    /// 开放式行为驱动器的任务约束判定接口。
    /// 项目方可在任意组件实现本接口，并挂到 OpenBehaviorDriver 的“自定义任务判定组件”字段。
    /// </summary>
    public interface IOpenBehaviorTaskConstraintProvider
    {
        /// <summary>是否视为“任务NPC”。</summary>
        bool IsTaskNpc(SoulNPC npc);

        /// <summary>是否处于“紧急任务”状态（用于忙碌静默）。</summary>
        bool IsUrgentTaskNpc(SoulNPC npc);
    }
}
