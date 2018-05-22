using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive
{
    public partial class App : Application
    {
        public App()
        {
            Logger = new LoggerImpl();
        }

        private class LoggerImpl : ILogger
        {
            public void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null)
            {
                Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!(Current.MainWindow is MainWindow win))
                        return;

                    win.AppendTrace(level, value, methodName);
                });
            }
        }

        public ILogger Logger { get; }

        public void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => Logger.Log(level, value, methodName);
    }
}
