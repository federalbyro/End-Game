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
            if (redTeam.HasFighters())
            {
                return blueTeam;
            }
            return redTeam;
        }

        private Team RandomStartAttack()
        {
            Random randomStartAttack = new Random();
            bool redTeamStarts = randomStartAttack.Next(2) == 0;
            if (redTeamStarts)
            {
                return redTeam;
            }
            return blueTeam;
        }

        public void Battle()
        {
            Team attackingTeam = RandomStartAttack();
            Team defendingTeam = (attackingTeam == redTeam) ? blueTeam : redTeam;

            while (redTeam.HasFighters() && blueTeam.HasFighters())
            {
                IUnit attacker = attackingTeam.GetNextFighter();
                IUnit defender = defendingTeam.GetNextFighter();

                Console.WriteLine($"\n{attacker.Name} атакует {defender.Name}!");
                attacker.Attack(defender);

                // Проверяем, выжил ли защитник
                if (defender.Health <= 0)
                {
                    Console.WriteLine($"{defender.Name} пал в бою!");
                    defendingTeam.RemoveFighter();
                }

                // Меняем атакующую команду
                (attackingTeam, defendingTeam) = (defendingTeam, attackingTeam);
            }

            Console.WriteLine(redTeam.HasFighters() ? "Красная команда победила!" : "Синяя команда победила!");
        }



    }
}
