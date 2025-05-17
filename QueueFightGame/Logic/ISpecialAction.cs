using System.Collections.Generic;

namespace QueueFightGame
{
    public interface ISpecialActionUnit : IUnit
    {
        bool HasUsedSpecial { get; set; }
        int SpecialActionChance { get; }
        void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager);
    }

    public interface ISpecialActionHealer : ISpecialActionUnit
    {
        int HealRange { get; }
        int HealPower { get; }
    }

    public interface ISpecialActionArcher : ISpecialActionUnit
    {
        int AttackRange { get; }
        int AttackPower { get; }
    }

    public interface ISpecialActionMage : ISpecialActionUnit
    {
        int CloneRange { get; }
    }

    public interface ISpecialActionWeakFighter : ISpecialActionUnit
    {
        int BuffRange { get; }
        bool HasAppliedBuff { get; }
    }
}