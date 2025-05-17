using System;
using System.Collections.Generic;

namespace QueueFightGame
{
    public class AttackCommand : IGameCommand
    {
        private readonly IUnit _attacker;
        private readonly IUnit _defender;
        private readonly Team _attackingTeam;
        private readonly Team _defendingTeam;
        private readonly ILogger _logger;
        private readonly CommandManager _commandManager;

        private float _initialDefenderHealth;
        private float _initialAttackerHealth;
        private bool _defenderWasAlive;
        private int _defenderOriginalIndex = -1;


        public AttackCommand(IUnit attacker, IUnit defender, Team attackingTeam, Team defendingTeam, ILogger logger, CommandManager commandManager)
        {
            _attacker = attacker;
            _defender = defender;
            _attackingTeam = attackingTeam;
            _defendingTeam = defendingTeam;
            _logger = logger;
            _commandManager = commandManager;

            _initialDefenderHealth = _defender.Health;
            _initialAttackerHealth = _attacker.Health;
            _defenderWasAlive = _defender.Health > 0;
            if (_defenderWasAlive)
            {
                _defenderOriginalIndex = _defendingTeam.Fighters.IndexOf(_defender);
            }
        }

        public void Execute()
        {
            _defender.Health = _initialDefenderHealth;
            _attacker.Health = _initialAttackerHealth;
            if (_defenderWasAlive && !_defendingTeam.Fighters.Contains(_defender))
            {
                if (_defenderOriginalIndex != -1)
                    _defendingTeam.AddFighterAt(_defenderOriginalIndex, _defender);
                else
                    _defendingTeam.AddFighterAt(_defendingTeam.Fighters.Count, _defender);
            }

            _attacker.Attack(_defender, _logger);

            if (_defender.Health <= 0 && _defenderWasAlive) { }

            if (_attacker.Health <= 0) { }
        }

        public void Undo()
        {
            _defender.Health = _initialDefenderHealth;
            _attacker.Health = _initialAttackerHealth;

            if (_defenderWasAlive && _defender.Health > 0 && !_defendingTeam.Fighters.Contains(_defender))
            {
                if (_defenderOriginalIndex != -1)
                    _defendingTeam.AddFighterAt(_defenderOriginalIndex, _defender);
                else
                    _defendingTeam.AddFighterAt(0, _defender);

                _logger.Log($"{_defender.Name}|({_defender.ID}) возвращен в бой (Undo).");
            }
        }
    }
}