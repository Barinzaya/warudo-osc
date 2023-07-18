using SharpOSC;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

[NodeType(Id = "com.barinzaya.oscinput.nodes.oscinput", Title = "OSC Input", Category = "External Integration")]
public class OscInputNode : Node {
    public new OscInputPlugin Plugin => base.Plugin as OscInputPlugin;

    [DataInput] public OscInputType DataType = OscInputType.Float;
    [DataInput] public string Address = "/value";
    [DataInput] public int Index = 0;

    [HiddenIf("DataType", If.NotEqual, OscInputType.Boolean)]
    [DataOutput] public bool BooleanValue() => boolValue;
    private bool boolValue;

    [HiddenIf("DataType", If.NotEqual, OscInputType.Float)]
    [DataOutput] public float FloatValue() => floatValue;
    private float floatValue;

    [HiddenIf("DataType", If.NotEqual, OscInputType.Int)]
    [DataOutput] public int IntValue() => intValue;
    private int intValue;

    [HiddenIf("DataType", If.NotEqual, OscInputType.String)]
    [DataOutput] public string StringValue() => stringValue;
    private string stringValue;

    [FlowOutput] public Continuation Exit;

    protected override void OnCreate() {
        base.OnCreate();
        Plugin.ReceivedOscMessage += OnReceivedOscMessage;
    }

    protected override void OnDestroy() {
        Plugin.ReceivedOscMessage -= OnReceivedOscMessage;
        base.OnDestroy();
    }

    protected void OnReceivedOscMessage(OscMessage message) {
        if (message.Address == Address) {
            if (Index >= 0 && Index < message.Arguments.Count) {
                var value = message.Arguments[Index];

                if (value is bool b) {
                    boolValue = b;
                }

                if (value is float f) {
                    floatValue = f;
                }

                if (value is double d) {
                    floatValue = (float)d;
                }

                if (value is int i) {
                    intValue = i;
                }

                if (value is long l) {
                    intValue = (int)l;
                }

                if (value is string s) {
                    stringValue = s;
                }

                Graph?.InvokeFlow(this, "Exit");
            }
        }
    }
}

public enum OscInputType {
    Boolean,
    Float,
    Int,
    String,
}

