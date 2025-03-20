using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFightGame
{
    internal class WeakFighter : BaseUnit, ICanBeHealed
    {
        public WeakFighter() : base("WeakFighter", 100f, 0.7f, 25) { }
    }

    internal class StrongFighter : BaseUnit
    {
        public StrongFighter() : base("StrongFighter", 140f, 0.5f, 40) { }
    }

    internal class Healer : BaseUnit, ISpecialActionHealer
    {
        public int Range { get; private set; }
        public int Power { get; private set; }
        public Healer() : base("Healer", 30f, 0.9f, 5)
        {
            Range = 3;
            Power = 5;
        }

        public void DoHeal(ICanBeHealed target)
        {
            Console.WriteLine($"Healer doheal {target.Name}");
        }
    }

    internal class Archer : BaseUnit, ISpecialActionArcher, ICanBeHealed
    {
        public int Range { get; set; }
        public int Power { get; set; }
        public Archer(string name) : base(name, 20f, 0.8f, 5)
        {
            Range = 3;
            Power = 15;
        }
        public void DoSpecialAttack(IUnit target, Team ownTeam)
        {
            int archerIndex = ownTeam.QueueFighters.ToList().FindIndex(unit => unit == this);

            if (archerIndex >= Range)
            {
                Console.WriteLine($"{Name} не может стрелять, его обзор закрыт!");
                return;
            }

            Random random = new Random();
            bool isHit = random.Next(100) < 70;

            if (isHit)
            {
                float newDamage = Power * target.Protection;
                Console.WriteLine($"{Name} стреляет в {target.Name} и попадает, нанося {newDamage} урона!");
                target.Health -= newDamage;
            }
            else
            {
                Console.WriteLine($"{Name} стреляет в {target.Name}, но промахивается!");
            }
        }

    }
}
