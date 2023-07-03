using System.Collections.Generic;
using System.Diagnostics;

namespace ServiceA.Diagnostics;

public class ParentContextPropagator : DistributedContextPropagator
{
    public override IReadOnlyCollection<string> Fields => CreateDefaultPropagator().Fields;

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
    {
        return CreateDefaultPropagator().ExtractBaggage(carrier, getter);
    }

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        CreateDefaultPropagator().ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
    }

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        CreateDefaultPropagator().Inject(activity?.Parent, carrier, setter);
    }
}
