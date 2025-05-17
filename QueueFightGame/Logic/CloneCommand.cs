using System;

namespace QueueFightGame
{
    public class CloneCommand : IGameCommand
    {
        private readonly Mage _mage;
        private readonly ICanBeCloned _original;
        private readonly Team _team;
        private readonly int _insertPosition;
        private readonly ILogger _logger;

        private IUnit _createdClone;

        public CloneCommand(Mage mage, ICanBeCloned original, Team team, int insertPosition, ILogger logger)
        {
            _mage = mage;
            _original = original;
            _team = team;
            _insertPosition = insertPosition;
            _logger = logger;
        }

        public void Execute()
        {
            _createdClone = _original.Clone() as IUnit;

            if (_createdClone != null)
            {
                int actualPosition = Math.Max(0, Math.Min(_insertPosition, _team.Fighters.Count));
                _team.AddFighterAt(actualPosition, _createdClone);
                _logger.Log($"{_mage.Name}|({_mage.ID}) ({_team.TeamName}) успешно клонировал {_original.Name}, создав {_createdClone.Name}!");
            }
            else
            {
                _logger.Log($"{_mage.Name} ({_team.TeamName}) не смог создать клон {_original.Name} (ошибка клонирования).");
            }
        }

        public void Undo()
        {
            if (_createdClone != null && _team.Fighters.Contains(_createdClone))
            {
                _team.RemoveFighter(_createdClone, _logger, false);
            }
        }
    }
}