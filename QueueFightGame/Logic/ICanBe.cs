namespace QueueFightGame
{
    public interface ICanBeHealed
    {
        string Name { get; }
        float Health { get; set; }
        float MaxHealth { get; }
    }

    public interface ICanBeCloned : IUnit
    {
        ICanBeCloned Clone();
    }

    public interface ICanBeBuff
    {
        BuffType BuffType { get; }
        float DamageMultiplier { get; }
        float GetModifiedProtection(IUnit attacker);
        void ApplyBuffEffect(StrongFighter fighter);
        void RemoveBuffEffect(StrongFighter fighter);
    }

    public enum BuffType
    {
        None,
        Spear,
        Horse,
        Shield,
        Helmet
    }
}