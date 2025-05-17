using System;
using System.Collections.Generic;
using System.Linq;

namespace QueueFightGame
{
    public interface IGameCommand
    {
        void Execute();
        void Undo();
        // string GetLogMessage(); // Optional: command provides its own log string
    }
    
    public class CommandManager
    {
        private readonly Stack<IGameCommand> _undoStack = new Stack<IGameCommand>();
        private readonly Stack<IGameCommand> _redoStack = new Stack<IGameCommand>();
        private const int MaxUndoLevels = 20;
        private readonly ILogger _logger;
        
        // Track commands by round
        private Dictionary<int, List<IGameCommand>> _commandsByRound = new Dictionary<int, List<IGameCommand>>();
        private int _currentRound = 1;
        
        // Track dead fighters by round
        private Dictionary<int, List<DeadFighterRecord>> _deadFightersByRound = new Dictionary<int, List<DeadFighterRecord>>();
        
        // Track the sequence of rounds to allow undoing to specific rounds
        private List<int> _roundHistory = new List<int>();
        
        // Class to track dead fighter information
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

        // Set current round number
        public void SetCurrentRound(int round)
        {
            _currentRound = round;
            
            // Add the round to history if not already present
            if (!_roundHistory.Contains(round))
                _roundHistory.Add(round);
        }

        // Record a dead fighter
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

        // Новый метод для очистки истории
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
            
            // Track which round this command belongs to
            if (!_commandsByRound.ContainsKey(_currentRound))
                _commandsByRound[_currentRound] = new List<IGameCommand>();
            
            _commandsByRound[_currentRound].Add(command);

            // Limit undo stack size
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
        
        // Method to check if we can undo to a specific round
        public bool CanUndoToRound(int round)
        {
            return round < _currentRound && _roundHistory.Contains(round);
        }
        
        // Get a list of available rounds for undo purposes
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
        
        // Get dead fighters from a specific round
        public List<DeadFighterRecord> GetDeadFightersFromRound(int round)
        {
            if (_deadFightersByRound.ContainsKey(round))
                return _deadFightersByRound[round];
            return new List<DeadFighterRecord>();
        }
        
        // Get all dead fighters from rounds between target and current
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
        
        // Remove dead fighter records for a round
        public void ClearDeadFightersForRound(int round)
        {
            if (_deadFightersByRound.ContainsKey(round))
                _deadFightersByRound.Remove(round);
        }
        
        // Method to undo to a specific round
        public int UndoToRound(int targetRound)
        {
            // FIX: Use _currentRound as the actual current round, not just _roundHistory's max
            int currentRound = _currentRound;

            if (targetRound >= currentRound || !_roundHistory.Contains(targetRound))
            {
                _logger.Log($"Нет действий для отмены до раунда {targetRound}.");
                return 0;
            }

            int actionsUndone = 0;

            // Restore all dead fighters from rounds being undone BEFORE undoing commands
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

            // Undo each round starting from the most recent
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

            // Remove all rounds > targetRound from _roundHistory
            _roundHistory.RemoveAll(r => r > targetRound);

            // Update _currentRound to the new round (so further undo works correctly)
            _currentRound = targetRound;

            return actionsUndone;
        }
        
        // Helper method to undo commands for a specific round
        private int UndoRoundCommands(List<IGameCommand> commandsToUndo)
        {
            if (commandsToUndo == null || commandsToUndo.Count == 0)
                return 0;
                
            int undoCount = 0;
            var tempStack = new Stack<IGameCommand>();
            
            // Process all commands in the undo stack
            while (_undoStack.Count > 0)
            {
                var cmd = _undoStack.Pop();
                
                if (commandsToUndo.Contains(cmd))
                {
                    // This command belongs to the round we're undoing
                    cmd.Undo();
                    _redoStack.Push(cmd);
                    undoCount++;
                }
                else
                {
                    // This command is from a different round, keep it
                    tempStack.Push(cmd);
                }
            }
            
            // Restore commands from other rounds
            while (tempStack.Count > 0)
            {
                _undoStack.Push(tempStack.Pop());
            }
            
            return undoCount;
        }
        
        // Метод для отката только последнего раунда
        public int UndoLastRound(int currentRound)
        {
            // Найти последний раунд в истории
            int lastRound = _roundHistory.Count > 0 ? _roundHistory.Max() : 0;
            if (lastRound == 0 || !_commandsByRound.ContainsKey(lastRound))
            {
                _logger.Log("Нет предыдущих раундов для отмены.");
                return 0;
            }

            // Восстановить погибших бойцов этого раунда (если есть)
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

            // Откатить все команды этого раунда (в обратном порядке)
            var commands = _commandsByRound[lastRound];
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                commands[i].Undo();
                _redoStack.Push(commands[i]);
                _undoStack.Pop(); // Удалить из undo-стека
            }

            _logger.Log($"Раунд {lastRound} отменён ({commands.Count} действий).");

            // Удалить этот раунд из истории
            _commandsByRound.Remove(lastRound);
            _deadFightersByRound.Remove(lastRound);
            _roundHistory.Remove(lastRound);

            // Обновить текущий раунд
            _currentRound = _roundHistory.Count > 0 ? _roundHistory.Max() : 1;

            return commands.Count;
        }
        
        // Метод для возврата только последнего отменённого раунда
        public int RedoLastRound(int currentRound)
        {
            // Следующий раунд после текущего
            int nextRound = _roundHistory.Count > 0 ? _roundHistory.Max() + 1 : 1;
            // Но если уже есть команды для nextRound, значит redo невозможен
            if (!_commandsByRound.ContainsKey(nextRound) && _redoStack.Count > 0)
            {
                // Собираем команды для этого раунда из redo-стека (в обратном порядке)
                var commandsToRedo = new List<IGameCommand>();
                int count = 0;
                // Считаем сколько команд было в последнем undone раунде
                foreach (var kv in _commandsByRound)
                {
                    if (kv.Key == currentRound)
                    {
                        count = kv.Value.Count;
                        break;
                    }
                }
                // Если не нашли, пробуем по количеству в redo-стеке (fallback)
                if (count == 0)
                    count = _redoStack.Count;

                for (int i = 0; i < count && _redoStack.Count > 0; i++)
                {
                    commandsToRedo.Add(_redoStack.Pop());
                }
                commandsToRedo.Reverse(); // Чтобы порядок был как при исполнении

                foreach (var cmd in commandsToRedo)
                {
                    cmd.Execute();
                    _undoStack.Push(cmd);
                }

                // Добавляем команды в _commandsByRound и _roundHistory
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