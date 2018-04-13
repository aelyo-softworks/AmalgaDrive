using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using AmalgaDrive.Utilities;

namespace AmalgaDrive.Model
{
    public abstract class DriveObject : ChangeTrackingDictionaryObject
    {
        // we want to expose validity publicly
        public bool IsValid => !DictionaryObjectHasErrors;

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // recompute valid for any change
            OnPropertyChanged(this, nameof(IsValid));
            base.OnPropertyChanged(sender, e);
        }

        protected override void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            OnPropertyChanged(this, nameof(IsValid));
            base.OnErrorsChanged(sender, e);
        }

        protected virtual void DispatchOnPropertyChanged(DispatcherObject obj, object sender, DispatcherPriority priority = DispatcherPriority.Normal, [CallerMemberName] string name = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var dispatcher = obj?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            dispatcher.Invoke(() => OnPropertyChanged(sender, new PropertyChangedEventArgs(name)), priority);
        }

        protected virtual void OnPropertyChanged(DispatcherObject obj, object sender, [CallerMemberName] string name = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var dispatcher = obj?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => OnPropertyChanged(sender, new PropertyChangedEventArgs(name)));
            }
            else
            {
                OnPropertyChanged(sender, new PropertyChangedEventArgs(name));
            }
        }
    }
}
