using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CCBot
{
    public class Dependencies
    {
        public static Settings Settings { get; set; }
        internal InteractivityExtension Interactivity { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        internal StartTimes StartTimes { get; set; }
        internal CancellationTokenSource Cts { get; set; }
    }
}