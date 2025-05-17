using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;



namespace QueueFightGame
{
    public class GameManager
    {
        public Team RedTeam { get; private set; }
        public Team BlueTeam { get; private set; }
        public Team CurrentAttacker { get; private set; }
        public Team CurrentDefender { get; private set; }
        public GameState CurrentState { get; private set; }
        public int Round { get; private set; }

        private readonly CommandManager _commandManager;
        public CommandManager CommandManager => _commandManager;
        public ILogger _logger;
        public ILogger Logger => _logger;

        private readonly Random _random = new Random();

        // Events for UI updates
        public event EventHandler<GameStateChangedEventArgs> GameStateChanged;
        public event EventHandler<LogEventArgs> LogGenerated;
        public event EventHandler<GameOverEventArgs> GameOver;

        // DTO для сериализации
        public class GameStateDto
        {
            public int Round { get; set; }
            public List<UnitDto> RedUnits { get; set; }
            public List<UnitDto> BlueUnits { get; set; }
            public List<string> LogHistory { get; set; }
            // при желании — стеки undo/redo
        }

        public class UnitDto
        {
            public string TypeName { get; set; }
            public float Health { get; set; }
            public int Id { get; set; }
            // и всё, что нужно для восстановления
        }

        // В GameManager:
        public void SaveState(string path)
        {
            var dto = new GameStateDto
            {
                Round = Round,
                RedUnits = RedTeam.Fighters.Select(u => new UnitDto
                {
                    TypeName = u.GetType().Name,
                    Health = u.Health,
                    Id = u.ID
                }).ToList(),
                BlueUnits = BlueTeam.Fighters.Select(u => new UnitDto
                {
                    TypeName = u.GetType().Name,
                    Health = u.Health,
                    Id = u.ID
                }).ToList(),
                LogHistory = _logger.GetLogHistory()
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(dto));


        }

        public void LoadState(string path)
        {
            var dto = JsonConvert.DeserializeObject<GameStateDto>(File.ReadAllText(path));

            // 1) Создать команды (если их ещё нет) и задать бюджет (его тоже можно дописать в DTO)
            if (RedTeam == null) RedTeam = new Team("Красные", 0);
            if (BlueTeam == null) BlueTeam = new Team("Синие", 0);

            // 2) Очистить старые списки бойцов
            RedTeam.Fighters.Clear();
            BlueTeam.Fighters.Clear();

            foreach (var u in dto.RedUnits)
            {
                var unit = UnitFactory.CreateUnit(u.TypeName);
                unit.Health = u.Health;

                // добавляем напрямую, не тратя бюджет
                RedTeam.Fighters.Add(unit);
                unit.Team = RedTeam;
            }
            foreach (var u in dto.BlueUnits)
            {
                var unit = UnitFactory.CreateUnit(u.TypeName);
                unit.Health = u.Health;

                BlueTeam.Fighters.Add(unit);
                unit.Team = BlueTeam;
            }


            // 4) Восстановить раунд
            Round = dto.Round;

            CurrentState = GameState.WaitingForPlayer;   // после загрузки ждём действия игрока

            // 5) Восстановить очередь ходов (например, сохранять и загружать CurrentAttacker в DTO)
            //    Пока просто делаем: тот, кто ходил последним, ходит следующим
            CurrentAttacker = (dto.Round % 2 == 1) ? RedTeam : BlueTeam;
            CurrentDefender = CurrentAttacker == RedTeam ? BlueTeam : RedTeam;

            // 6) Восстановить лог
            _logger.ClearLog();
            foreach (var line in dto.LogHistory)
                _logger.Log(line);

            // 7) Очистить историю Undo/Redo
            _commandManager.ClearHistory();

            // 8) Уведомить UI
            OnGameStateChanged();
        }



        public GameManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandManager = new CommandManager(_logger); // Pass logger to CommandManager too
            CurrentState = GameState.NotStarted;
        }

        public void StartGame(Team redTeam, Team blueTeam)
        {
            RedTeam = redTeam ?? throw new ArgumentNullException(nameof(redTeam));
            BlueTeam = blueTeam ?? throw new ArgumentNullException(nameof(blueTeam));

            RedTeam.ResetUnitsForNewBattle();
            BlueTeam.ResetUnitsForNewBattle();
            _logger.ClearLog();
            // НЕ пересоздаем CommandManager, а очищаем его историю
            _commandManager.ClearHistory();

            CurrentAttacker = _random.Next(2) == 0 ? RedTeam : BlueTeam;
            CurrentDefender = CurrentAttacker == RedTeam ? BlueTeam : RedTeam;

            Round = 1;
            // ВАЖНО: Устанавливаем состояние ОЖИДАНИЯ, а не TurnInProgress сразу
            CurrentState = GameState.WaitingForPlayer;
            Log($"--- Игра началась! ---");
            Log($"Команда {RedTeam.TeamName} состав: {string.Join(", ", RedTeam.Fighters.Select(f => f.Name))}");
            Log($"Команда {BlueTeam.TeamName} состав: {string.Join(", ", BlueTeam.Fighters.Select(f => f.Name))}");
            Log($"Первый ход за командой: {CurrentAttacker.TeamName}. Нажмите 'Следующий Ход'."); // Подсказка игроку

            OnGameStateChanged(); // Отправляем начальное состояние UI
        }

        public void RequestNextTurn()
        {
            if (CurrentState != GameState.TurnInProgress && CurrentState != GameState.WaitingForPlayer) // Allow starting turn
            {
                Log("Невозможно начать ход, игра не идет или завершена.");
                return;
            }

            CurrentState = GameState.TurnInProgress; // Lock state during processing

            // Update the command manager with current round number
            _commandManager.SetCurrentRound(Round);

            Log($"\n--- Раунд {Round} ---");
            Log($"Ходит команда: {CurrentAttacker.TeamName}");

            // --- Phase 1: Special Abilities ---
            Log("--- Фаза способностей ---");
            ProcessSpecialAbilities(CurrentAttacker); // Attacker's specials first? Or both simultaneously? Let's do attacker then defender.
            ProcessSpecialAbilities(CurrentDefender);
            // Reset special usage flags for next turn cycle
            ResetSpecialAbilityFlags(CurrentAttacker);
            ResetSpecialAbilityFlags(CurrentDefender);
            // Check for deaths after specials
            CheckForDeaths();
            if (CheckWinCondition()) return; // Check if specials caused game over

            // --- Phase 2: Main Attack ---
            Log($"--- Фаза атаки ({CurrentAttacker.TeamName}) ---");
            IUnit attackerUnit = CurrentAttacker.GetNextFighter();
            IUnit defenderUnit = CurrentDefender.GetNextFighter();

            if (attackerUnit != null && defenderUnit != null)
            {
                var attackCommand = new AttackCommand(attackerUnit, defenderUnit, CurrentAttacker, CurrentDefender, _logger, _commandManager);
                _commandManager.ExecuteCommand(attackCommand);
            }
            else
            {
                Log("Невозможно атаковать: отсутствует один из бойцов на передовой.");
            }

            // --- Phase 3: Cleanup & State Change ---
            CheckForDeaths(); // Remove units killed by the main attack
            if (CheckWinCondition()) return; // Check for game over after attack

            // Switch turns
            (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
            Round++; // Increment round after a full cycle (both teams acted or tried to)

            CurrentState = GameState.WaitingForPlayer; // Ready for next input
            Log($"Ход завершен. Ожидание следующего хода (Раунд {Round}).");
            OnGameStateChanged();
        }

        private void ProcessSpecialAbilities(Team team)
        {
            Log($"-- Способности команды {team.TeamName} --");
            var livingFighters = team.GetLivingFighters(); // Process only living units

            foreach (var unit in livingFighters)
            {
                if (unit is ISpecialActionUnit specialUnit && !specialUnit.HasUsedSpecial)
                {
                    try
                    {
                        // PerformSpecialAction handles its own probability check now
                        specialUnit.PerformSpecialAction(team, team == RedTeam ? BlueTeam : RedTeam, _logger, _commandManager);
                    }
                    catch (Exception ex)
                    {
                        Log($"ОШИБКА способности у {unit.Name}: {ex.Message}");
                    }
                }
            }
        }

        private void ResetSpecialAbilityFlags(Team team)
        {
            foreach (var unit in team.Fighters.OfType<ISpecialActionUnit>())
            {
                unit.HasUsedSpecial = false;
            }
        }

        private void CheckForDeaths()
        {
            // Save references to dead fighters before removing them
            var deadRedFighters = RedTeam.Fighters.Where(f => f.Health <= 0).ToList();
            var deadBlueFighters = BlueTeam.Fighters.Where(f => f.Health <= 0).ToList();
            
            // Record dead fighters with their positions for possible resurrection during undo
            foreach (var fighter in deadRedFighters)
            {
                int position = RedTeam.Fighters.IndexOf(fighter);
                _commandManager.RecordDeadFighter(fighter, RedTeam, position);
                Log($"Боец {fighter.Name} из команды {RedTeam.TeamName} погибает!");
            }
            
            foreach (var fighter in deadBlueFighters)
            {
                int position = BlueTeam.Fighters.IndexOf(fighter);
                _commandManager.RecordDeadFighter(fighter, BlueTeam, position);
                Log($"Боец {fighter.Name} из команды {BlueTeam.TeamName} погибает!");
            }
            
            // Now remove them from teams
            RedTeam.RemoveDeadFighters(_logger);
            BlueTeam.RemoveDeadFighters(_logger);
        }

        private bool CheckWinCondition()
        {
            bool redLost = !RedTeam.HasFighters();
            bool blueLost = !BlueTeam.HasFighters();

            if (redLost && blueLost)
            {
                CurrentState = GameState.GameOver;
                Log("\n--- Игра окончена: НИЧЬЯ! ---");
                OnGameOver(null); // Draw
                return true;
            }
            if (redLost)
            {
                CurrentState = GameState.GameOver;
                Log($"\n--- Игра окончена: Победила команда {BlueTeam.TeamName}! ---");
                OnGameOver(BlueTeam); // Blue wins
                return true;
            }
            if (blueLost)
            {
                CurrentState = GameState.GameOver;
                Log($"\n--- Игра окончена: Победила команда {RedTeam.TeamName}! ---");
                OnGameOver(RedTeam); // Red wins
                return true;
            }
            return false;
        }

        // New method to allow undoing to a specific round
        public void RequestUndoToRound(int targetRound)
        {
            if (CurrentState == GameState.GameOver)
            {
                Log("Нельзя отменить ход: игра завершена.");
                return;
            }
            if (CurrentState == GameState.TurnInProgress)
            {
                Log("Нельзя отменить ход во время его обработки.");
                return;
            }
            
            if (targetRound >= Round)
            {
                Log($"Невозможно отменить до раунда {targetRound} - текущий раунд: {Round}.");
                return;
            }

            if (_commandManager.CanUndoToRound(targetRound))
            {
                int originalRound = Round;
                
                // Get all fighters that died in rounds after the target round
                var deadFighters = _commandManager.GetDeadFightersInRange(targetRound, Round);
                
                // Undo to the target round
                int actionsUndone = _commandManager.UndoToRound(targetRound);
                
                if (actionsUndone > 0)
                {
                    // Set the round to the target round
                    Round = targetRound;
                    
                    // Swap attacker/defender based on round parity
                    bool shouldSwapFromCurrent = (originalRound - targetRound) % 2 == 1;
                    if (shouldSwapFromCurrent)
                    {
                        (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
                    }
                    
                    // Restore dead fighters to their teams
                    foreach (var deadFighter in deadFighters)
                    {
                        // Restore some health to the fighter
                        deadFighter.Unit.Health = Math.Max(1, deadFighter.Unit.MaxHealth * 0.1f);
                        
                        // Add the fighter back to its team at the correct position
                        deadFighter.Team.AddFighterAt(
                            Math.Min(deadFighter.Position, deadFighter.Team.Fighters.Count), 
                            deadFighter.Unit);
                        
                        Log($"Боец {deadFighter.Unit.Name} воскрешен и возвращен в команду {deadFighter.Team.TeamName}.");
                    }
                    
                    // Clear records for all undone rounds
                    for (int r = targetRound + 1; r <= originalRound; r++)
                    {
                        _commandManager.ClearDeadFightersForRound(r);
                    }
                    
                    Log($"Игра отменена до раунда {targetRound}.");
                }
                
                CurrentState = GameState.WaitingForPlayer; // Allow next action
                OnGameStateChanged(); // Notify UI to refresh display
            }
            else
            {
                Log($"Нельзя отменить до раунда {targetRound}.");
            }
        }

        public void RequestUndoTurn()
        {
            if (CurrentState == GameState.GameOver)
            {
                Log("Нельзя отменить ход: игра завершена.");
                return;
            }
            if (CurrentState == GameState.TurnInProgress)
            {
                Log("Нельзя отменить ход во время его обработки.");
                return;
            }

            if (_commandManager.CanUndo)
            {
                int prevRound = Round - 1;
                int actionsUndone = _commandManager.UndoLastRound(Round);

                if (actionsUndone > 0 && Round > 1)
                {
                    Round--;
                    // Меняем атакующую/защищающуюся команду обратно
                    (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
                }

                CurrentState = GameState.WaitingForPlayer;
                Log("Раунд полностью отменен.");
                OnGameStateChanged();
            }
            else
            {
                Log("Нет действий для отмены.");
            }
        }

        // --- Event Invokers ---
        public virtual void OnGameStateChanged()
        {
            GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(RedTeam, BlueTeam, CurrentState, GetLogHistory()));
        }

        protected virtual void OnGameOver(Team winner)
        {
            GameOver?.Invoke(this, new GameOverEventArgs(winner));
            OnGameStateChanged(); // Send final state
        }

        // Helper to raise log event (and maybe log internally too)
        private void Log(string message)
        {
            _logger.Log(message); // Log to internal logger (MemoryLogger)
            LogGenerated?.Invoke(this, new LogEventArgs(message)); // Notify subscribers
        }

        public List<string> GetLogHistory()
        {
            return _logger.GetLogHistory();
        }
    }

    // --- Enums and Event Args ---
    public enum GameState
    {
        NotStarted,
        WaitingForPlayer, // Waiting for "Next Turn" or "Undo"
        TurnInProgress,   // Processing turn logic
        GameOver
    }

    public class GameStateChangedEventArgs : EventArgs
    {
        public Team RedTeamSnapshot { get; } // Consider sending copies/snapshots if needed
        public Team BlueTeamSnapshot { get; }
        public GameState CurrentState { get; }
        public List<string> LogMessages { get; }

        public GameStateChangedEventArgs(Team red, Team blue, GameState state, List<string> logs)
        {
            RedTeamSnapshot = red; // For now, pass reference. UI should be careful.
            BlueTeamSnapshot = blue;
            CurrentState = state;
            LogMessages = new List<string>(logs); // Copy of logs
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogEventArgs(string message) { Message = message; }
    }

    public class GameOverEventArgs : EventArgs
    {
        public Team WinningTeam { get; } // null for a draw
        public GameOverEventArgs(Team winner) { WinningTeam = winner; }
    }


}