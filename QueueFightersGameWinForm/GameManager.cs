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
            Archer archer1 = new Archer();
            Archer archer2 = new Archer();

            redTeam.AddFighter(strongFighter2);
            redTeam.AddFighter(weakFighter1);
            redTeam.AddFighter(archer1);

            blueTeam.AddFighter(weakFighter2);
            blueTeam.AddFighter(strongFighter1);
            blueTeam.AddFighter(archer2);
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
                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey();

                Console.WriteLine("\n--- Новый раунд ---");
                Console.WriteLine($"Ходит команда: {(attackingTeam == redTeam ? "Красная" : "Синяя")}");

                IUnit attacker = attackingTeam.GetNextFighter();
                IUnit defender = defendingTeam.GetNextFighter();

                Console.WriteLine($"\n{attacker.Name} | HP: {attacker.Health} атакует {defender.Name}| HP: {defender.Health}");
                attacker.Attack(defender);

                if (defender.Health <= 0)
                {
                    Console.WriteLine($"{defender.Name} пал в бою!");
                    defendingTeam.RemoveFighter();
                }

                (attackingTeam, defendingTeam) = (defendingTeam, attackingTeam);
            }

            Console.WriteLine(redTeam.HasFighters() ? "Красная команда победила!" : "Синяя команда победила!");
        }



    }
}
