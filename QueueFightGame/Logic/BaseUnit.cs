using System;

namespace QueueFightGame
{
    public abstract class BaseUnit : IUnit
    {
        private static int _nextId = 1;

        public string Name { get; protected set; }
        public int ID { get; private set; }
        public float Health { get; set; }
        public float MaxHealth { get; protected set; }
        public float Protection { get; protected set; }
        public float Damage { get; protected set; }
        public float Cost { get; private set; }
        public string Description { get; private set; }
        public string IconPath { get; private set; }
        public Team Team { get; set; }
        public override string ToString() => $"{Name}#{ID}";
        protected BaseUnit(string typeName)
        {
            if (!UnitConfig.Stats.TryGetValue(typeName, out var data))
            {
                throw new ArgumentException($"Configuration not found for unit type: {typeName}");
            }

            ID = _nextId++;
            Name = data.DisplayName;
            MaxHealth = data.Health;
            Health = data.Health;
            Protection = data.Protection;
            Damage = data.Damage;
            Cost = data.Cost;
            Description = data.Description;
            IconPath = data.IconPath;
            Team = null;
        }
        protected BaseUnit(BaseUnit original, string cloneSuffix = "_clone")
        {
            ID = _nextId++;
            Name = original.Name + cloneSuffix;
            MaxHealth = original.MaxHealth;
            Health = original.Health * 0.75f;
            Health = original.MaxHealth;
            Protection = original.Protection;
            Damage = original.Damage;
            Cost = original.Cost;
            Description = original.Description;
            IconPath = original.IconPath;
            Team = original.Team;
        }


        public virtual void Attack(IUnit target, ILogger logger)
        {
            float damageDealt = Math.Max(1, this.Damage * (1.0f - target.Protection));
            target.Health -= damageDealt;
            logger.Log($"{this.Name}|({this.ID}) ({this.Team.TeamName}) атакует {target.Name} ({target.Team.TeamName}) и наносит {damageDealt:F1} урона. Осталось здоровья у {target.Name}: {target.Health:F1}");
        }

        public virtual bool HasUsedSpecial { get; set; } = false;
        public virtual int SpecialActionChance => 0;

        public virtual void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager) { }
    }

    // --- Wall and Adapter ---
    public abstract class BaseWall : IWall
    {
        public string Name { get; }
        public float Health { get; set; }
        public float MaxHealth { get; }
        public float Protection { get; set; }

        protected BaseWall(string name, float health, float protection)
        {
            Name = name;
            MaxHealth = health;
            Health = health;
            Protection = protection;
        }
    }

    public class StoneWall : BaseWall
    {
        public StoneWall() : base("Каменная стена", 200, 0.3f) { }
    }

    public class WallAdapter : BaseUnit, ICanBeHealed
    {
        private readonly BaseWall _wall;

        public WallAdapter() : base(nameof(WallAdapter)) { }

        public override void Attack(IUnit target, ILogger logger)
        {
            logger.Log($"{this.Name}|({this.ID}) ({this.Team.TeamName}) не может атаковать.");
        }

        public override void PerformSpecialAction(Team ownTeam, Team enemyTeam, ILogger logger, CommandManager commandManager) { }
    }


}