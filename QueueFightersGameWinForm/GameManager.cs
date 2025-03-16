using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueFightGame
{
    internal class GameManager
    {

        private Team redTeam;
        private Team blueTeam;

        public GameManager()
        {
            redTeam = new Team("Red");
            blueTeam = new Team("Blue");

            CreatFighters();
            Battle();
        }

        public void CreatFighters()
        {
            WeakFighter weakFighter1 = new WeakFighter();
            WeakFighter weakFighter2 = new WeakFighter();
            StrongFighter strongFighter1 = new StrongFighter();
            StrongFighter strongFighter2 = new StrongFighter();

            redTeam.AddFighter(strongFighter2);
            redTeam.AddFighter(weakFighter1);

            blueTeam.AddFighter(weakFighter2);
            blueTeam.AddFighter(strongFighter1);
        }

        private Team CheckWinnerTeam()
        {
            if (redTeam == null)
            {
                return blueTeam;
            }
            return redTeam;
        }

        public void Battle()
        {
            while (redTeam.HasFighters() && blueTeam.HasFighters())
            {
                IUnit redFighter = redTeam.GetNextFighter();
                IUnit blueFighter = blueTeam.GetNextFighter();

                Console.WriteLine($"\n{redFighter.Name} (Health: {redFighter.Health}) vs {blueFighter.Name} (Health: {blueFighter.Health})");

                redFighter.Attack(blueFighter);
                if (blueFighter.Health <= 0)
                {
                    Console.WriteLine($"{blueFighter.Name} погиб!");
                    blueTeam.RemoveFighter();
                }

                if (!blueTeam.HasFighters()) break;

                blueFighter = blueTeam.GetNextFighter();
                blueFighter.Attack(redFighter);
                if (redFighter.Health <= 0)
                {
                    Console.WriteLine($"{redFighter.Name} погиб!");
                    redTeam.RemoveFighter();
                }
                Console.ReadKey();
            }
        }


    }
}
