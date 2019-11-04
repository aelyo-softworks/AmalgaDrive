using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Tester
{
    public class Logger : ILogger
    {
        public void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null)
        {
            var threadName = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
            var text = DateTime.Now + " [" + threadName + "][" + level + "]" + methodName + ": " + value;

            switch (level)
            {
                case TraceLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(text);
                    Console.ResetColor();
                    break;

                case TraceLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(text);
                    Console.ResetColor();
                    break;

                default:
                    Console.WriteLine(text);
                    break;
            }
        }
    }
}
