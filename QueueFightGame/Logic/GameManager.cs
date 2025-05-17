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
        private readonly ILogger _logger;
        public ILogger Logger => _logger;
        private readonly Random _random = new Random();
        public event EventHandler<GameStateChangedEventArgs> GameStateChanged;
        public event EventHandler<LogEventArgs> LogGenerated;
        public event EventHandler<GameOverEventArgs> GameOver;

        public class GameStateDto
        {
            public int Round { get; set; }
            public List<UnitDto> RedUnits { get; set; }
            public List<UnitDto> BlueUnits { get; set; }
            public List<string> LogHistory { get; set; }
        }
        public class UnitDto
        {
            public string TypeName { get; set; }
            public float Health { get; set; }
            public int Id { get; set; }
        }
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
            if (RedTeam == null) RedTeam = new Team("Красные", 0);
            if (BlueTeam == null) BlueTeam = new Team("Синие", 0);
            RedTeam.Fighters.Clear();
            BlueTeam.Fighters.Clear();
            foreach (var u in dto.RedUnits)
            {
                var unit = UnitFactory.CreateUnit(u.TypeName);
                unit.Health = u.Health;
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
            Round = dto.Round;
            CurrentState = GameState.WaitingForPlayer;
            CurrentAttacker = (dto.Round % 2 == 1) ? RedTeam : BlueTeam;
            CurrentDefender = CurrentAttacker == RedTeam ? BlueTeam : RedTeam;
            _logger.ClearLog();
            foreach (var line in dto.LogHistory)
                _logger.Log(line);
            _commandManager.ClearHistory();
            OnGameStateChanged();
        }
        public GameManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandManager = new CommandManager(_logger);
            CurrentState = GameState.NotStarted;
        }
        public void StartGame(Team redTeam, Team blueTeam)
        {
            RedTeam = redTeam ?? throw new ArgumentNullException(nameof(redTeam));
            BlueTeam = blueTeam ?? throw new ArgumentNullException(nameof(blueTeam));
            RedTeam.ResetUnitsForNewBattle();
            BlueTeam.ResetUnitsForNewBattle();
            _logger.ClearLog();
            _commandManager.ClearHistory();
            CurrentAttacker = _random.Next(2) == 0 ? RedTeam : BlueTeam;
            CurrentDefender = CurrentAttacker == RedTeam ? BlueTeam : RedTeam;
            Round = 1;
            CurrentState = GameState.WaitingForPlayer;
            Log("--- Игра началась! ---");
            Log($"Команда {RedTeam.TeamName} состав: {string.Join(", ", RedTeam.Fighters.Select(f => f.Name))}");
            Log($"Команда {BlueTeam.TeamName} состав: {string.Join(", ", BlueTeam.Fighters.Select(f => f.Name))}");
            Log($"Первый ход за командой: {CurrentAttacker.TeamName}. Нажмите 'Следующий Ход'.");
            OnGameStateChanged();
        }
        public void RequestNextTurn()
        {
            if (CurrentState != GameState.TurnInProgress && CurrentState != GameState.WaitingForPlayer) return;
            CurrentState = GameState.TurnInProgress;
            _commandManager.SetCurrentRound(Round);
            Log($"\n--- Раунд {Round} ---");
            Log($"Ходит команда: {CurrentAttacker.TeamName}");
            Log("--- Фаза способностей ---");
            ProcessSpecialAbilities(CurrentAttacker);
            ProcessSpecialAbilities(CurrentDefender);
            ResetSpecialAbilityFlags(CurrentAttacker);
            ResetSpecialAbilityFlags(CurrentDefender);
            CheckForDeaths();
            if (CheckWinCondition()) return;
            Log($"--- Фаза атаки ({CurrentAttacker.TeamName}) ---");
            IUnit attackerUnit = CurrentAttacker.GetNextFighter();
            IUnit defenderUnit = CurrentDefender.GetNextFighter();
            if (attackerUnit != null && defenderUnit != null)
            {
                var attackCommand = new AttackCommand(attackerUnit, defenderUnit, CurrentAttacker, CurrentDefender, _logger, _commandManager);
                _commandManager.ExecuteCommand(attackCommand);
            }
            CheckForDeaths();
            if (CheckWinCondition()) return;
            (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
            Round++;
            CurrentState = GameState.WaitingForPlayer;
            Log($"Ход завершен. Ожидание следующего хода (Раунд {Round}).");
            OnGameStateChanged();
        }
        private void ProcessSpecialAbilities(Team team)
        {
            Log($"-- Способности команды {team.TeamName} --");
            foreach (var unit in team.GetLivingFighters())
            {
                if (unit is ISpecialActionUnit specialUnit && !specialUnit.HasUsedSpecial)
                {
                    try { specialUnit.PerformSpecialAction(team, team == RedTeam ? BlueTeam : RedTeam, _logger, _commandManager); }
                    catch (Exception ex) { Log($"ОШИБКА способности у {unit.Name}: {ex.Message}"); }
                }
            }
        }
        private void ResetSpecialAbilityFlags(Team team)
        {
            foreach (var unit in team.Fighters.OfType<ISpecialActionUnit>())
                unit.HasUsedSpecial = false;
        }
        private void CheckForDeaths()
        {
            var deadRedFighters = RedTeam.Fighters.Where(f => f.Health <= 0).ToList();
            var deadBlueFighters = BlueTeam.Fighters.Where(f => f.Health <= 0).ToList();
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
                OnGameOver(null);
                return true;
            }
            if (redLost)
            {
                CurrentState = GameState.GameOver;
                Log($"\n--- Игра окончена: Победила команда {BlueTeam.TeamName}! ---");
                OnGameOver(BlueTeam);
                return true;
            }
            if (blueLost)
            {
                CurrentState = GameState.GameOver;
                Log($"\n--- Игра окончена: Победила команда {RedTeam.TeamName}! ---");
                OnGameOver(RedTeam);
                return true;
            }
            return false;
        }
        public void RequestUndoToRound(int targetRound)
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.TurnInProgress) return;
            if (targetRound >= Round) return;
            if (_commandManager.CanUndoToRound(targetRound))
            {
                int originalRound = Round;
                var deadFighters = _commandManager.GetDeadFightersInRange(targetRound, Round);
                int actionsUndone = _commandManager.UndoToRound(targetRound);
                if (actionsUndone > 0)
                {
                    Round = targetRound;
                    bool shouldSwapFromCurrent = (originalRound - targetRound) % 2 == 1;
                    if (shouldSwapFromCurrent)
                        (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
                    foreach (var deadFighter in deadFighters)
                    {
                        deadFighter.Unit.Health = Math.Max(1, deadFighter.Unit.MaxHealth * 0.1f);
                        deadFighter.Team.AddFighterAt(
                            Math.Min(deadFighter.Position, deadFighter.Team.Fighters.Count),
                            deadFighter.Unit);
                        Log($"Боец {deadFighter.Unit.Name} воскрешен и возвращен в команду {deadFighter.Team.TeamName}.");
                    }
                    for (int r = targetRound + 1; r <= originalRound; r++)
                        _commandManager.ClearDeadFightersForRound(r);
                    Log($"Игра отменена до раунда {targetRound}.");
                }
                CurrentState = GameState.WaitingForPlayer;
                OnGameStateChanged();
            }
        }
        public void RequestUndoTurn()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.TurnInProgress) return;
            if (_commandManager.CanUndo)
            {
                int prevRound = Round - 1;
                int actionsUndone = _commandManager.UndoLastRound(Round);
                if (actionsUndone > 0 && Round > 1)
                {
                    Round--;
                    (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
                }
                CurrentState = GameState.WaitingForPlayer;
                Log("Раунд полностью отменен.");
                OnGameStateChanged();
            }
        }
        public void RequestRedoTurn()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.TurnInProgress) return;
            if (_commandManager.CanRedo)
            {
                int actionsRedone = _commandManager.RedoLastRound(Round);
                if (actionsRedone > 0)
                {
                    Round++;
                    (CurrentAttacker, CurrentDefender) = (CurrentDefender, CurrentAttacker);
                }
                CurrentState = GameState.WaitingForPlayer;
                Log("Раунд возвращён (Redo).");
                OnGameStateChanged();
            }
        }
        public virtual void OnGameStateChanged()
        {
            GameStateChanged?.Invoke(this, new GameStateChangedEventArgs(RedTeam, BlueTeam, CurrentState, GetLogHistory()));
        }
        protected virtual void OnGameOver(Team winner)
        {
            GameOver?.Invoke(this, new GameOverEventArgs(winner));
            OnGameStateChanged();
        }
        private void Log(string message)
        {
            _logger.Log(message);
            LogGenerated?.Invoke(this, new LogEventArgs(message));
        }
        public List<string> GetLogHistory() => _logger.GetLogHistory();
    }

    public enum GameState
    {
        NotStarted,
        WaitingForPlayer,
        TurnInProgress,
        GameOver
    }
    public class GameStateChangedEventArgs : EventArgs
    {
        public Team RedTeamSnapshot { get; }
        public Team BlueTeamSnapshot { get; }
        public GameState CurrentState { get; }
        public List<string> LogMessages { get; }
        public GameStateChangedEventArgs(Team red, Team blue, GameState state, List<string> logs)
        {
            RedTeamSnapshot = red;
            BlueTeamSnapshot = blue;
            CurrentState = state;
            LogMessages = new List<string>(logs);
        }
    }
    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogEventArgs(string message) { Message = message; }
    }
    public class GameOverEventArgs : EventArgs
    {
        public Team WinningTeam { get; }
        public GameOverEventArgs(Team winner) { WinningTeam = winner; }
    }
}
