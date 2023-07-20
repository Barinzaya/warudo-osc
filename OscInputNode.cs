using OscCore;
using System;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Graphs;

[NodeType(Id = "com.barinzaya.oscinput.nodes.oscmessage", Title = "On OSC Message", Category = "External Integration")]
public class OscInputNode : Node {
    public new OscInputPlugin Plugin => base.Plugin as OscInputPlugin;

    [DataInput] public string Address = "/value";

    [Label("Arguments")]
    [DataInput] public OscInputType[] ArgumentTypes = new OscInputType[0];

    [FlowOutput] public Continuation Exit;

    private object[] values;

    protected override void OnCreate() {
        base.OnCreate();

        ConfigureOutputs(null, ArgumentTypes);
        Watch<OscInputType[]>(nameof(ArgumentTypes), (oldArgs, newArgs) => ConfigureOutputs(oldArgs, newArgs));

        Plugin?.AddHandler(Address, OnReceivedOscMessage);
        Watch<string>(nameof(Address), (oldAddress, newAddress) => {
            Plugin?.RemoveHandler(oldAddress, OnReceivedOscMessage);
            Plugin?.AddHandler(newAddress, OnReceivedOscMessage);
        });
    }

    protected override void OnDestroy() {
        Plugin?.RemoveHandler(Address, OnReceivedOscMessage);
        base.OnDestroy();
    }

    protected void OnReceivedOscMessage(OscMessage message) {
        var numArgs = Math.Min(message.Count, values.Length);

        for (var i = 0; i < numArgs; i++) {
            var value = message[i];

            values[i] = ArgumentTypes[i] switch {
                OscInputType.Boolean => value switch {
                    bool x => x,
                    double x => x != 0,
                    float x => x != 0,
                    int x => x != 0,
                    long x => x != 0,
                    _ => value
                },

                OscInputType.Float => value switch {
                    bool x => x ? 1f : 0f,
                    double x => (float)x,
                    float x => x,
                    int x => (float)x,
                    long x => (float)x,
                    _ => value
                },

                OscInputType.Int => value switch {
                    bool x => x ? 1 : 0,
                    double x => (int)Math.Round(x),
                    float x => (int)Math.Round(x),
                    int x => x,
                    long x => (int)x,
                    _ => value
                },

                OscInputType.String => value switch {
                    string x => x,
                    _ => value
                },

                _ => throw new ArgumentException("Unsupported argument type", $"ArgumentTypes[{i}]")
            };
        }

        Broadcast();
        Graph?.InvokeFlow(this, "Exit");
    }

    private void ConfigureOutputs(OscInputType[] oldArgs, OscInputType[] newArgs) {
        var oldCount = oldArgs?.Length ?? 0;
        var newCount = newArgs?.Length ?? 0;

        if (newCount != oldCount) {
            Array.Resize(ref values, newCount);
        }

        for (var i = newCount; i < oldCount; i++) {
            DataOutputPortCollection.RemovePort($"Arg{i+1}");
        }

        for (var i = 0; i < newCount; i++) {
            if (i < oldCount && oldArgs[i] == newArgs[i]) {
                continue;
            }

            var name = $"Arg{i+1}";
            DataOutputPortCollection.RemovePort(name);

            object value = ArgumentTypes[i] switch {
                OscInputType.Boolean => false,
                OscInputType.Float => 0f,
                OscInputType.Int => 0,
                OscInputType.String => "",
                _ => throw new ArgumentException("Unhandled input type", $"ArgumentTypes[{i}]")
            };
            values[i] = value;

            var j = i;
            DataOutputPortCollection.AddPort(new DataOutputPort(name, value.GetType(), () => values[j], new DataOutputProperties {
                label = name,
                order = i,
            }));
        }

        Broadcast();
    }
}

public enum OscInputType {
    Boolean,
    Float,
    Int,
    String,
}
