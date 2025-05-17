using System;
using System.Linq;
using System.Collections.Generic;

namespace QueueFightGame
{
    public class WeakFighter : BaseUnit, ICanBeHealed, ICanBeCloned, ISpecialActionWeakFighter
    {
        private bool _hasAppliedBuff = false;
        private StrongFighter _knightToBuff = null;

        public int BuffRange { get; private set; }
        public bool HasAppliedBuff => _hasAppliedBuff;
        public override int SpecialActionChance => 100;

        public WeakFighter() : base(nameof(WeakFighter))
        {
            BuffRange = UnitConfig.Stats[nameof(WeakFighter)].BuffRange ?? 1;
        }

        private WeakFighter(WeakFighter original) : base(original)
        {
            this.BuffRange = original.BuffRange;
            this._hasAppliedBuff = original._hasAppliedBuff;
            this._hasAppliedBuff = false;
        }

        public ICanBeCloned Clone()
        {
            return new WeakFighter(this);
        }

        public override void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager)
        {
            TryApplyBuff(ownTeam, logger, commandManager);
            HasUsedSpecial = true;
        }

        private void TryApplyBuff(Team ownTeam, ILogger logger, CommandManager commandManager)
        {
            if (_hasAppliedBuff || this.Health <= 0) return;

            int myIndex = ownTeam.Fighters.IndexOf(this);
            if (myIndex < 0) return;

            _knightToBuff = ownTeam.Fighters
                .OfType<StrongFighter>()
                .Where(k => k.Health > 0 && Math.Abs(ownTeam.Fighters.IndexOf(k) - myIndex) <= BuffRange)
                .OrderBy(k => Math.Abs(ownTeam.Fighters.IndexOf(k) - myIndex))
                .FirstOrDefault();

            if (_knightToBuff != null)
            {
                var buffCommand = new SquireBuffCommand(this, _knightToBuff, ownTeam, logger);
                commandManager.ExecuteCommand(buffCommand);
            }
        }

        public void MarkBuffApplied(StrongFighter knight)
        {
            _knightToBuff = knight;
            _hasAppliedBuff = true;
        }

        public void UnmarkBuffApplied()
        {
            _hasAppliedBuff = false;
        }
    }

    public class StrongFighter : BaseUnit, ICanBeHealed
    {
        private ICanBeBuff _currentBuff = null;
        private WeakFighter _squire = null;

        public StrongFighter() : base(nameof(StrongFighter)) { }

        public void SetSquire(WeakFighter squire)
        {
            _squire = squire;
        }

        public WeakFighter GetSquire() => _squire;

        public void ApplyBuff(ICanBeBuff buff, ILogger logger)
        {
            if (_currentBuff != null && _currentBuff.BuffType != BuffType.None)
            {
                logger.Log($"{Name}|({ID}) уже имеет бафф {_currentBuff.BuffType}. Новый бафф {buff.BuffType} не применен.");
                return;
            }
            _currentBuff = buff;
            _currentBuff?.ApplyBuffEffect(this);
            logger.Log($"{Name} получает бафф {buff.BuffType} от {_squire?.Name ?? "кого-то"}.");
        }

        public void RemoveBuff(ILogger logger)
        {
            if (_currentBuff != null && _currentBuff.BuffType != BuffType.None)
            {
                logger.Log($"{Name}|({ID}) теряет бафф {_currentBuff.BuffType}.");
                _currentBuff?.RemoveBuffEffect(this);
                _currentBuff = null;
                _squire = null;
            }
        }

        public BuffType CurrentBuffType => _currentBuff?.BuffType ?? BuffType.None;

        public override void Attack(IUnit target, ILogger logger)
        {
            float damageMultiplier = _currentBuff?.DamageMultiplier ?? 1.0f;
            float targetProtection = target.Protection;

            float baseDamage = this.Damage * damageMultiplier;
            float damageDealt = Math.Max(1, baseDamage * (1.0f - targetProtection));

            target.Health -= damageDealt;
            logger.Log($"{this.Name}|({this.ID}) ({this.Team.TeamName}){(CurrentBuffType != BuffType.None ? $" [{CurrentBuffType}]" : "")} атакует {target.Name} ({target.Team.TeamName}) и наносит {damageDealt:F1} урона. Осталось здоровья у {target.Name}: {target.Health:F1}");

            if (_currentBuff != null && (CurrentBuffType == BuffType.Spear || CurrentBuffType == BuffType.Horse))
            {
                RemoveBuff(logger);
            }
        }
    }

    public class Healer : BaseUnit, ICanBeHealed, ICanBeCloned, ISpecialActionHealer
    {
        public int HealRange { get; private set; }
        public int HealPower { get; private set; }
        public override int SpecialActionChance => 40;

        public Healer() : base(nameof(Healer))
        {
            HealRange = UnitConfig.Stats[nameof(Healer)].Range ?? 1;
            HealPower = UnitConfig.Stats[nameof(Healer)].Power ?? 15;
        }

        private Healer(Healer original) : base(original)
        {
            this.HealRange = original.HealRange;
            this.HealPower = original.HealPower;
        }

        public ICanBeCloned Clone()
        {
            return new Healer(this);
        }

        public override void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager)
        {
            if (HasUsedSpecial || Health <= 0) return;

            if (new Random().Next(100) < SpecialActionChance)
            {
                TryHeal(ownTeam, logger, commandManager);
            }
            else
            {
                logger.Log($"{Name}|({ID}) ({Team.TeamName}) пропускает лечение в этот ход.");
            }
            HasUsedSpecial = true;
        }

        private void TryHeal(Team ownTeam, ILogger logger, CommandManager commandManager)
        {
            int myIndex = ownTeam.Fighters.IndexOf(this);
            if (myIndex < 0) return;

            ICanBeHealed target = ownTeam.Fighters
                .Where(u => u != this && u is ICanBeHealed ch && ch.Health < ch.MaxHealth && Math.Abs(ownTeam.Fighters.IndexOf(u) - myIndex) <= HealRange)
                .Cast<ICanBeHealed>()
                .OrderBy(u => u.Health)
                .FirstOrDefault();

            if (target != null)
            {
                var healCommand = new HealCommand(this, target, HealPower, ownTeam, logger);
                commandManager.ExecuteCommand(healCommand);
            }
            else
            {
                logger.Log($"{Name}|({ID}) ({Team.TeamName}) не нашел раненых союзников в радиусе {HealRange}.");
            }
        }
    }

    public class Archer : BaseUnit, ICanBeHealed, ICanBeCloned, ISpecialActionArcher
    {
        public int AttackRange { get; private set; }
        public int AttackPower { get; private set; }
        public override int SpecialActionChance => 75;

        public Archer() : base(nameof(Archer))
        {
            AttackRange = UnitConfig.Stats[nameof(Archer)].Range ?? 3;
            AttackPower = UnitConfig.Stats[nameof(Archer)].Power ?? 15;
        }

        private Archer(Archer original) : base(original)
        {
            this.AttackRange = original.AttackRange;
            this.AttackPower = original.AttackPower;
        }

        public ICanBeCloned Clone()
        {
            return new Archer(this);
        }

        public override void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager)
        {
            if (HasUsedSpecial || Health <= 0) return;

            if (new Random().Next(100) < SpecialActionChance)
            {
                TrySpecialAttack(enemyTeam, logger, commandManager);
            }
            else
            {
                logger.Log($"{Name} ({Team.TeamName}) пропускает выстрел в этот ход.");
            }
            HasUsedSpecial = true;
        }

        private void TrySpecialAttack(Team enemyTeam, ILogger logger, CommandManager commandManager)
        {
            if (!enemyTeam.HasFighters()) return;

            List<IUnit> possibleTargets = enemyTeam.Fighters.Where(u => u.Health > 0).ToList();

            if (possibleTargets.Any())
            {
                IUnit target = possibleTargets[new Random().Next(possibleTargets.Count)];

                var archerAttackCommand = new ArcherAttackCommand(this, target, AttackPower, enemyTeam, logger);
                commandManager.ExecuteCommand(archerAttackCommand);
            }
            else
            {
                logger.Log($"{Name} ({Team.TeamName}) не нашел целей для выстрела.");
            }
        }
    }

    public class Mage : BaseUnit, ICanBeHealed, ISpecialActionMage
    {
        public int CloneRange { get; private set; }
        public override int SpecialActionChance => 10;

        public Mage() : base(nameof(Mage))
        {
            CloneRange = UnitConfig.Stats[nameof(Mage)].CloneRange ?? 1;
        }

        public override void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager)
        {
            if (HasUsedSpecial || Health <= 0) return;

            if (new Random().Next(100) < SpecialActionChance)
            {
                TryClone(ownTeam, logger, commandManager);
            }
            else { }
            HasUsedSpecial = true;
        }

        private void TryClone(Team ownTeam, ILogger logger, CommandManager commandManager)
        {
            int myIndex = ownTeam.Fighters.IndexOf(this);
            if (myIndex < 0) return;

            var possibleTargets = ownTeam.Fighters
                .Where((u, index) => u != this && u is ICanBeCloned cloneable && cloneable.Health > 0 && Math.Abs(index - myIndex) <= CloneRange)
                .Cast<ICanBeCloned>()
                .ToList();

            if (!possibleTargets.Any())
            {
                logger.Log($"{Name} ({Team.TeamName}) не нашел подходящих целей для клонирования рядом!");
                return;
            }

            var targetToClone = possibleTargets[new Random().Next(possibleTargets.Count)];
            int targetIndex = ownTeam.Fighters.IndexOf(targetToClone as IUnit);

            int insertPosition = myIndex + 1;
            var cloneCommand = new CloneCommand(this, targetToClone, ownTeam, insertPosition, logger);
            commandManager.ExecuteCommand(cloneCommand);
        }
    }
}
