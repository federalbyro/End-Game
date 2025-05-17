namespace QueueFightGame
{
    // Keep existing IUnit interface
    public interface IUnit
    {
        string Name { get; }
        int ID { get; }
        float Health { get; set; }
        float MaxHealth { get; }
        float Protection { get; }
        float Damage { get; }
        float Cost { get; }
        string Description { get; }
        string IconPath { get; }
        Team Team { get; set; }

        void Attack(IUnit target, ILogger logger);
    }

    public interface IWall
    {
        string Name { get; }
        float Health { get; set; }
        float MaxHealth { get; }
        float Protection { get; }
    }
}