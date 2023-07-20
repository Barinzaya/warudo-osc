using OscCore;
using System;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Graphs;

[NodeType(Id = "com.barinzaya.oscinput.nodes.oscmessage", Title = "On OSC Message", Category = "External Integration")]
public class OscInputNode : Node {
    public new OscInputPlugin Plugin => base.Plugin as OscInputPlugin;

    [DataInput] public string Address = "/value";
    [DataInput] public OscInputType[] ArgumentTypes = new OscInputType[0];

    [FlowOutput] public Continuation Exit;

    private object[] values;

    protected override void OnCreate() {
        base.OnCreate();

        ConfigureOutputs();
        Watch(nameof(ArgumentTypes), () => ConfigureOutputs());

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

    public void ConfigureOutputs() {
        var count = ArgumentTypes.Length;
        if (count != values?.Length) {
            values = new object[count];
        }

        DataOutputPortCollection.Clear();

        for (var i = 0; i < count; i++) {
            var name = $"Arg{i+1}";
            object value;

            switch (ArgumentTypes[i]) {
                case OscInputType.Boolean: value = false; break;
                case OscInputType.Float: value = 0f; break;
                case OscInputType.Int: value = 0; break;
                case OscInputType.String: value = ""; break;

                default: throw new ArgumentException("Unhandled input type", $"ArgumentTypes[{i}]");
            }

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

