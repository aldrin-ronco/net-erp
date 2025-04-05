using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NetErp.Helpers
{
    public class DebounceHelper
    {
        private readonly DispatcherTimer _timer;
        private Action _action;

        public DebounceHelper(int delayMilliseconds)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMilliseconds) };
            _timer.Tick += (s, e) => { _timer.Stop(); _action?.Invoke(); };
        }

        public void Execute(Action action)
        {
            _action = action;
            _timer.Stop();
            _timer.Start();
        }
    }
}
