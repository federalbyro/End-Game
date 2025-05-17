using System;

namespace QueueFightGame
{
    public abstract class BuffDecoratorBase : ICanBeBuff
    {
        protected readonly StrongFighter _fighter;
        public abstract BuffType BuffType { get; }
        public virtual float DamageMultiplier => 1.0f;

        protected BuffDecoratorBase(StrongFighter fighter)
        {
            _fighter = fighter ?? throw new ArgumentNullException(nameof(fighter));
        }

        public virtual float GetModifiedProtection(IUnit attacker)
        {
            return _fighter.Protection;
        }

        public virtual void ApplyBuffEffect(StrongFighter fighter) { }
        public virtual void RemoveBuffEffect(StrongFighter fighter) { }
    }

    // --- Specific Buffs ---

    public class SpearBuffDecorator : BuffDecoratorBase
    {
        public override BuffType BuffType => BuffType.Spear;
        public override float DamageMultiplier => 1.5f;

        public SpearBuffDecorator(StrongFighter fighter) : base(fighter) { }

        public override float GetModifiedProtection(IUnit attacker) => _fighter.Protection;
    }

    public class HorseBuffDecorator : BuffDecoratorBase
    {
        public override BuffType BuffType => BuffType.Horse;
        public override float DamageMultiplier => 1.2f;

        public HorseBuffDecorator(StrongFighter fighter) : base(fighter) { }

        public override float GetModifiedProtection(IUnit attacker) => Math.Max(0.1f, _fighter.Protection - 0.2f);
    }

    public class ShieldBuffDecorator : BuffDecoratorBase
    {
        public override BuffType BuffType => BuffType.Shield;

        public ShieldBuffDecorator(StrongFighter fighter) : base(fighter) { }

        public override float GetModifiedProtection(IUnit attacker) => Math.Max(0.1f, _fighter.Protection - 0.3f);
    }

    public class HelmetBuffDecorator : BuffDecoratorBase
    {
        public override BuffType BuffType => BuffType.Helmet;

        public HelmetBuffDecorator(StrongFighter fighter) : base(fighter) { }

        public override float GetModifiedProtection(IUnit attacker)
        {
            if (attacker is Archer)
            {
                return Math.Max(0.1f, _fighter.Protection - 0.4f);
            }
            return _fighter.Protection;
        }
    }
}