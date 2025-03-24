using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFightGame
{
    internal class Team
    {
        public Queue<IUnit> QueueFighters { get; private set; }
        public string TeamName { get; private set; }
        public float Money { get; private set; }

        public Team(string teamName, float money)
        {
            TeamName = teamName;
            QueueFighters = new Queue<IUnit>();
            Money = money;
        }

        public void AddFighter(IUnit fighter)
        {
            fighter.Team = this;
            this.Money -= fighter.Cost;
            QueueFighters.Enqueue(fighter);
            Console.WriteLine($"Add {fighter.Name} to {this.TeamName}");
        }

        public bool HasFighters()
        {
            return QueueFighters.Count > 0;
        }

        public IUnit GetNextFighter()
        {
            if (!HasFighters())
            {
                Console.WriteLine($"Команда {TeamName} больше не имеет бойцов!");
                return null;
            }
            return QueueFighters.Peek();
        }

        public void RemoveFighter()
        {
            if (HasFighters())
            {
                IUnit removedFighter = QueueFighters.Dequeue();
                Console.WriteLine($"{removedFighter.Name} покинул команду {TeamName}.");
            }
        }

    }
}
