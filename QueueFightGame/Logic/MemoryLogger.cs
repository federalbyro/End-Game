using System.Collections.Generic;
using System.Linq;

namespace QueueFightGame
{
    public class MemoryLogger : ILogger
    {
        private readonly List<string> _logMessages = new List<string>();

        public void Log(string message)
        {
            _logMessages.Add(message);
        }

        public List<string> GetLogHistory()
        {
            return _logMessages.ToList();
        }

        public void ClearLog()
        {
            _logMessages.Clear();
        }
    }
}