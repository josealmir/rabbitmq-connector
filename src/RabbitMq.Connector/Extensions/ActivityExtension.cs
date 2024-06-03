
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using Serilog;
using RabbitMq.Connector.Model;
using System.Text;

namespace RabbitMq.Connector.Extensions
{
    public static class ActivityExtension
    {
        public static void InjectActivityToHeader(this Activity activity, IEventBasicPropertie @event)
        {
            ActivityContext activityContextToInject = default;
            TextMapPropagator propagator = Propagators.DefaultTextMapPropagator;
            if (activity is not null)
            {
                activityContextToInject = activity.Context;
            }
            else if (Activity.Current is not null)
            {
                activityContextToInject = Activity.Current.Context;
            }

            propagator.Inject(new PropagationContext(activityContextToInject, Baggage.Current), @event.BasicProperties, InjectContextInHeader);
            
            void InjectContextInHeader(IBasicProperties props, string key, string value) 
            {
                try
                {
                    props.Headers ??= new Dictionary<string, object>();
                    props.Headers[key] = value;
                }
                catch (Exception ex)
                {                    
                    Log.Error(ex, "Failed to inject trace context.");
                }
            }
        }

        public static ActivityContext ExtractActivityContext(this IEventBasicPropertie @event)
        {
            var props = @event.BasicProperties;
            static IEnumerable<string> ExtractActivityToHeader(IBasicProperties props, string key)
            {                
                try
                {
                    if (props.Headers.TryGetValue(key, out var value))
                    {
                        var bytes = value as byte[];
                        return new[] { Encoding.UTF8.GetString(bytes) };
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to extract trace context.");
                }
                return Enumerable.Empty<string>();
            }
            
            var _porpagator = new TraceContextPropagator().Extract(default, props, ExtractActivityToHeader);
            Baggage.Current = _porpagator.Baggage;
            return _porpagator.ActivityContext;
        }
    }
}