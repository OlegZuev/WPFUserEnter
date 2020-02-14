using System;
using System.Windows.Threading;

namespace Database {
    public class DispatcherTimeout : DispatcherTimer {
        private EventHandler _tick = delegate { };
        private TimeSpan _initialInterval;

        public new TimeSpan Interval {
            get => base.Interval;
            set {
                _initialInterval = value;
                base.Interval = _initialInterval;
            }
        }

        public new event EventHandler Tick {
            add {
                _tick = value;
                base.Tick += _tick;
            }
            remove => base.Tick -= value;
        }

        public void ForceStop() {
            Tick -= _tick;
            _tick = delegate { };
            Interval = _initialInterval;
        }
    }
}