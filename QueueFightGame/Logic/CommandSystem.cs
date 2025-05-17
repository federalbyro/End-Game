using System;
using System.Collections.Generic;
using System.Linq;

namespace QueueFightGame
{
    public interface IGameCommand
    {
        void Execute();
        void Undo();
    }

    public class CommandManager
    {
        private readonly Stack<IGameCommand> _undoStack = new Stack<IGameCommand>();
        private readonly Stack<IGameCommand> _redoStack = new Stack<IGameCommand>();
        private const int MaxUndoLevels = 20;
        private readonly ILogger _logger;

        private Dictionary<int, List<IGameCommand>> _commandsByRound = new Dictionary<int, List<IGameCommand>>();
        private int _currentRound = 1;

        private Dictionary<int, List<DeadFighterRecord>> _deadFightersByRound = new Dictionary<int, List<DeadFighterRecord>>();

        private List<int> _roundHistory = new List<int>();

        public class DeadFighterRecord
        {
            public IUnit Unit { get; set; }
            public Team Team { get; set; }
            public float Health { get; set; }
            public int Position { get; set; }
        }

        public CommandManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SetCurrentRound(int round)
        {
            _currentRound = round;

            if (!_roundHistory.Contains(round))
                _roundHistory.Add(round);
        }

        public void RecordDeadFighter(IUnit unit, Team team, int position)
        {
            if (!_deadFightersByRound.ContainsKey(_currentRound))
                _deadFightersByRound[_currentRound] = new List<DeadFighterRecord>();

            _deadFightersByRound[_currentRound].Add(new DeadFighterRecord
            {
                Unit = unit,
                Team = team,
                Health = unit.Health,
                Position = position
            });

            _logger.Log($"Записан погибший боец {unit.Name} из команды {team.TeamName}.");
        }

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _commandsByRound.Clear();
            _deadFightersByRound.Clear();
            _roundHistory.Clear();
            _currentRound = 1;
            _logger?.Log("История команд очищена.");
        }

        public void ExecuteCommand(IGameCommand command)
        {
            command.Execute();
            _undoStack.Push(command);

            if (!_commandsByRound.ContainsKey(_currentRound))
                _commandsByRound[_currentRound] = new List<IGameCommand>();

            _commandsByRound[_currentRound].Add(command);

            if (_undoStack.Count > MaxUndoLevels)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(0);
                _undoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(list[i]);
                }
            }
            _redoStack.Clear();
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public bool CanUndoToRound(int round)
        {
            return round < _currentRound && _roundHistory.Contains(round);
        }

        public List<int> GetAvailableUndoRounds()
        {
            return _roundHistory.Where(r => r < _currentRound).OrderByDescending(r => r).ToList();
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                _logger.Log("Действие отменено (Undo).");
            }
            else
            {
                _logger.Log("Нет действий для отмены.");
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
                _logger.Log("Действие повторено (Redo).");
            }
            else
            {
                _logger.Log("Нет действий для повтора.");
            }
        }

        public List<DeadFighterRecord> GetDeadFightersFromRound(int round)
        {
            if (_deadFightersByRound.ContainsKey(round))
                return _deadFightersByRound[round];
            return new List<DeadFighterRecord>();
        }

        public List<DeadFighterRecord> GetDeadFightersInRange(int targetRound, int currentRound)
        {
            var result = new List<DeadFighterRecord>();
            for (int round = targetRound + 1; round <= currentRound; round++)
            {
                if (_deadFightersByRound.ContainsKey(round))
                    result.AddRange(_deadFightersByRound[round]);
            }
            return result;
        }

        public void ClearDeadFightersForRound(int round)
        {
            if (_deadFightersByRound.ContainsKey(round))
                _deadFightersByRound.Remove(round);
        }

        public int UndoToRound(int targetRound)
        {
            int currentRound = _currentRound;

            if (targetRound >= currentRound || !_roundHistory.Contains(targetRound))
            {
                _logger.Log($"Нет действий для отмены до раунда {targetRound}.");
                return 0;
            }

            int actionsUndone = 0;

            for (int round = targetRound + 1; round <= currentRound; round++)
            {
                if (_deadFightersByRound.TryGetValue(round, out var deadList))
                {
                    foreach (var fighter in deadList)
                    {
                        if (!fighter.Team.Fighters.Contains(fighter.Unit))
                        {
                            fighter.Unit.Health = Math.Max(1, fighter.Unit.MaxHealth * 0.1f);
                            int insertPosition = Math.Min(fighter.Position, fighter.Team.Fighters.Count);
                            fighter.Team.AddFighterAt(insertPosition, fighter.Unit);
                            _logger.Log($"Боец {fighter.Unit.Name} воскрешен и возвращен в команду {fighter.Team.TeamName}.");
                        }
                    }
                }
            }

            List<int> roundsToUndo = _roundHistory.Where(r => r > targetRound && r <= currentRound)
                                                  .OrderByDescending(r => r)
                                                  .ToList();

            foreach (int round in roundsToUndo)
            {
                if (_commandsByRound.ContainsKey(round))
                {
                    var commandsInRound = _commandsByRound[round];
                    int roundUndoCount = UndoRoundCommands(commandsInRound);

                    actionsUndone += roundUndoCount;
                    _logger.Log($"Отменено {roundUndoCount} действий за раунд {round}.");

                    _commandsByRound.Remove(round);
                }
                _deadFightersByRound.Remove(round);
            }

            _roundHistory.RemoveAll(r => r > targetRound);

            _currentRound = targetRound;

            return actionsUndone;
        }

        private int UndoRoundCommands(List<IGameCommand> commandsToUndo)
        {
            if (commandsToUndo == null || commandsToUndo.Count == 0)
                return 0;

            int undoCount = 0;
            var tempStack = new Stack<IGameCommand>();

            while (_undoStack.Count > 0)
            {
                var cmd = _undoStack.Pop();

                if (commandsToUndo.Contains(cmd))
                {
                    cmd.Undo();
                    _redoStack.Push(cmd);
                    undoCount++;
                }
                else
                {
                    tempStack.Push(cmd);
                }
            }

            while (tempStack.Count > 0)
            {
                _undoStack.Push(tempStack.Pop());
            }

            return undoCount;
        }

        public int UndoLastRound(int currentRound)
        {
            int lastRound = _roundHistory.Count > 0 ? _roundHistory.Max() : 0;
            if (lastRound == 0 || !_commandsByRound.ContainsKey(lastRound))
            {
                _logger.Log("Нет предыдущих раундов для отмены.");
                return 0;
            }

            if (_deadFightersByRound.TryGetValue(lastRound, out var deadList))
            {
                foreach (var fighter in deadList)
                {
                    if (!fighter.Team.Fighters.Contains(fighter.Unit))
                    {
                        fighter.Unit.Health = Math.Max(1, fighter.Unit.MaxHealth * 0.1f);
                        int insertPosition = Math.Min(fighter.Position, fighter.Team.Fighters.Count);
                        fighter.Team.AddFighterAt(insertPosition, fighter.Unit);
                        _logger.Log($"Боец {fighter.Unit.Name} воскрешен и возвращен в команду {fighter.Team.TeamName}.");
                    }
                }
            }

            var commands = _commandsByRound[lastRound];
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
                _redoStack.Push(commands[i]);
                _undoStack.Pop();
            }

            _logger.Log($"Раунд {lastRound} отменён ({commands.Count} действий).");

            _commandsByRound.Remove(lastRound);
            _deadFightersByRound.Remove(lastRound);
            _roundHistory.Remove(lastRound);

            _currentRound = _roundHistory.Count > 0 ? _roundHistory.Max() : 1;

            return commands.Count;
        }

        public int RedoLastRound(int currentRound)
        {
            int nextRound = _roundHistory.Count > 0 ? _roundHistory.Max() + 1 : 1;
            if (!_commandsByRound.ContainsKey(nextRound) && _redoStack.Count > 0)
            {
                var commandsToRedo = new List<IGameCommand>();
                int count = 0;
                foreach (var kv in _commandsByRound)
                {
                    if (kv.Key == currentRound)
                    {
                        count = kv.Value.Count;
                        break;
                    }
                }
                if (count == 0)
                    count = _redoStack.Count;

                for (int i = 0; i < count && _redoStack.Count > 0; i++)
                {
                    commandsToRedo.Add(_redoStack.Pop());
                }
                commandsToRedo.Reverse();

                foreach (var cmd in commandsToRedo)
                {
                    cmd.Execute();
                    _undoStack.Push(cmd);
                }

                _commandsByRound[nextRound] = new List<IGameCommand>(commandsToRedo);
                _roundHistory.Add(nextRound);

                _logger.Log($"Раунд {nextRound} возвращён ({commandsToRedo.Count} действий).");
                return commandsToRedo.Count;
            }
            _logger.Log("Нет отменённых раундов для возврата.");
            return 0;
        }
    }
}
