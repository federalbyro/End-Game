using System;

namespace QueueFightGame
{
    public class ArcherAttackCommand : IGameCommand
    {
        private readonly Archer _archer;
        private readonly IUnit _target;
        private readonly int _power;
        private readonly Team _targetTeam;
        private readonly ILogger _logger;

        private float _initialTargetHealth;
        private float _damageDealt;

        public ArcherAttackCommand(Archer archer, IUnit target, int power, Team targetTeam, ILogger logger)
        {
            _archer = archer;
            _target = target;
            _power = power;
            _targetTeam = targetTeam;
            _logger = logger;

            _initialTargetHealth = _target.Health;
            _damageDealt = 0;
        }

        public void Execute()
        {
            _target.Health = _initialTargetHealth;
            _damageDealt = 0;

            bool isHit = new Random().Next(100) < 70;

            if (isHit)
            {
                _damageDealt = Math.Max(1, _power * (1.0f - _target.Protection));
                _target.Health -= _damageDealt;
                _logger.Log($"{_archer.Name}|({_archer.ID}) ({_archer.Team.TeamName}) стреляет в {_target.Name} ({_targetTeam.TeamName}) и ПОПАДАЕТ, нанося {_damageDealt:F1} урона. Осталось здоровья: {_target.Health:F1}/{_target.MaxHealth:F1}");

                if (_target.Health <= 0) { }
            }
            else
            {
                _logger.Log($"{_archer.Name}|({_archer.ID}) ({_archer.Team.TeamName}) стреляет в {_target.Name} ({_targetTeam.TeamName}), но ПРОМАХИВАЕТСЯ!");
                _damageDealt = 0;
            }
        }

        public void Undo()
        {
            if (_damageDealt > 0)
            {
                _target.Health += _damageDealt;
            }
        }
    }
}