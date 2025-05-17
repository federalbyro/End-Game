using System;
using System.Linq;

namespace QueueFightGame
{
    public class SquireBuffCommand : IGameCommand
    {
        private readonly WeakFighter _squire;
        private readonly StrongFighter _knight;
        private readonly Team _team;
        private readonly ILogger _logger;

        // State for Undo
        private BuffType _appliedBuffType = BuffType.None;
        private ICanBeBuff _previousKnightBuff = null;

        public SquireBuffCommand(WeakFighter squire, StrongFighter knight, Team team, ILogger logger)
        {
            _squire = squire;
            _knight = knight;
            _team = team;
            _logger = logger;
        }

        public void Execute()
        {
            if (_appliedBuffType != BuffType.None)
            {
                _squire.MarkBuffApplied(_knight);

                ApplySpecificBuff(_appliedBuffType);
            }
            else
            {
                var possibleBuffs = new[] { BuffType.Spear, BuffType.Horse, BuffType.Shield, BuffType.Helmet };
                _appliedBuffType = possibleBuffs[new Random().Next(possibleBuffs.Length)];

                var existingBuffType = _knight.CurrentBuffType;
                if (existingBuffType != BuffType.None)
                {
                    _logger.Log($"Предупреждение: {_knight.Name} уже имел бафф {existingBuffType}. Он будет заменен.");
                }


                if (ApplySpecificBuff(_appliedBuffType))
                {
                    _squire.MarkBuffApplied(_knight);
                }
                else
                {
                    _appliedBuffType = BuffType.None;
                }
            }
        }


        private bool ApplySpecificBuff(BuffType buffType)
        {
            ICanBeBuff buffInstance = null;
            switch (buffType)
            {
                case BuffType.Spear: buffInstance = new SpearBuffDecorator(_knight); break;
                case BuffType.Horse: buffInstance = new HorseBuffDecorator(_knight); break;
                case BuffType.Shield: buffInstance = new ShieldBuffDecorator(_knight); break;
                case BuffType.Helmet: buffInstance = new HelmetBuffDecorator(_knight); break;
                default: return false;
            }

            _knight.ApplyBuff(buffInstance, _logger);
            _knight.SetSquire(_squire);
            return true;
        }


        public void Undo()
        {
            if (_appliedBuffType != BuffType.None)
            {
                _squire.UnmarkBuffApplied();
                _knight.RemoveBuff(_logger);

                _appliedBuffType = BuffType.None;
            }
        }
    }
}