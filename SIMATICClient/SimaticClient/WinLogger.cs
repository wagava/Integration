using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SimaticClientService
{
    class WinLogger : IDisposable
    {
        EventLog log;
        string ESource;


        public WinLogger(string LoggerName)
        {
            log = new EventLog();
            ESource = CreateEventSource(LoggerName);
            log.Source = ESource;
        }


        public void Write(int level, string msg)
        {
            if (level == 1)
                log.WriteEntry(msg, EventLogEntryType.Error, 1000);
            else if (level == 2)
                log.WriteEntry(msg, EventLogEntryType.Warning, 2000);
            else if (level == 3)
                log.WriteEntry(msg, EventLogEntryType.Information, 3000);
        }


        private string CreateEventSource(string currentAppName)
        {
            string eventSource = currentAppName;
            bool sourceExists;
            try
            {
                // поиск источника выдает исключение безопасности ТОЛЬКО в том случае, если оно не существует!
                sourceExists = EventLog.SourceExists(eventSource);
                if (!sourceExists)
                {   // отсутствие исключений до сих пор означает, что пользователь имеет права администратора
                    EventLog.CreateEventSource(eventSource, "Application");
                }
            }
            catch //(SecurityException)
            {
                eventSource = "Application";
            }

            return eventSource;
        }


        public void Dispose()
        {

        }
    }
}
