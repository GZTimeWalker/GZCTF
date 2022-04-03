using NLog;
using NLog.Config;
using NLog.Targets;

namespace CTFServer.Extensions
{
    [Target("SignalR")]
    public class SignalRTarget : Target
    {
        public SignalRTarget() => Instance = this;

        public delegate void OnLog(LogEventInfo e);

        public event OnLog? LogEventHandler;

        public static SignalRTarget? Instance { get; private set; }

        protected override void Write(LogEventInfo logEvent)
            => LogEventHandler?.Invoke(logEvent);
    }
}

namespace NLog.LayoutRenderers.Wrappers
{
    [LayoutRenderer("shortname")]
    [ThreadAgnostic]
    public sealed class LoggerPrefixRendererWrapper : WrapperLayoutRendererBase
    {
        protected override string Transform(string text)
            =>  text.Contains('.') ? text[(text.LastIndexOf('.') + 1)..] : text;
    }
}