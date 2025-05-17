using System;

namespace QueueFightGame
{
    public class CloneCommand : IGameCommand
    {
        private readonly IUnit _actor;
        private readonly ICanBeCloned _target;
        private readonly Team _team;
        private readonly int _position;
        private readonly ILogger _logger;

        private IUnit _clone;

        public CloneCommand(
            IUnit actor,
            ICanBeCloned target,
            Team team,
            int position,
            ILogger logger)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _team = team ?? throw new ArgumentNullException(nameof(team));
            _position = position < 0 ? 0 : position;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute()
        {
            _clone = CreateClone();
            if (_clone == null)
            {
                LogFailure();
                return;
            }

            int index = CalculateIndex();
            AddClone(index);
            LogSuccess(index);
        }

        public void Undo()
        {
            if (_clone == null) return;

            RemoveClone();
            LogUndo();
        }

        private IUnit CreateClone()
        {
            return _target.Clone() as IUnit;
        }

        private int CalculateIndex()
        {
            int max = _team.Fighters.Count;
            return _position > max ? max : _position;
        }

        private void AddClone(int index)
        {
            _team.AddFighterAt(index, _clone);
        }

        private void RemoveClone()
        {
            _team.RemoveFighter(_clone, _logger, false);
        }

        private void LogSuccess(int index)
        {
            _logger.Log(
                $"{_actor.Name}|({_actor.ID}) ({_team.TeamName}) клонировал {_target.Name} " +
                $"и вставил клон {_clone.Name} на позицию {index}."
            );
        }

        private void LogFailure()
        {
            _logger.Log(
                $"{_actor.Name}|({_actor.ID}) ({_team.TeamName}) не смог клонировать {_target.Name}."
            );
        }

        private void LogUndo()
        {
            _logger.Log(
                $"Клон {_clone.Name}|({_clone.ID}) удалён из {_team.TeamName} (undo)."
            );
        }
    }
}
