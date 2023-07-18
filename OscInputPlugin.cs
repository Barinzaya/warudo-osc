using SharpOSC; // would rather use OscCore, but there is a zero-tolerance ban on System.IO and OscCore uses System.IO.MemoryStream
using System.Collections.Concurrent;
using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

[PluginType(Id = "com.barinzaya.oscinput", Name = "OSC Input", Version = "0.1.0", Author = "Barinzaya", Description = "Adds an OSC Input node, that allows data to be received via OSC.",
    NodeTypes = new[] { typeof(OscInputNode) })]
public class OscInputPlugin : Plugin {
    public const int OSC_SERVER_PORT = 19190;

    public delegate void OscMessageHandler(OscMessage message);
    public event OscMessageHandler ReceivedOscMessage;

    private UDPListener oscListener;
    private ConcurrentQueue<OscMessage> oscMessages = new ConcurrentQueue<OscMessage>();

    protected override void OnCreate() {
        base.OnCreate();
        oscListener = new UDPListener(OSC_SERVER_PORT, OnOscPacket);
    }

    protected override void OnDestroy() {
        oscListener.Dispose();
        base.OnDestroy();
    }

    public override void OnPreUpdate() {
        base.OnPreUpdate();

        OscMessage message;
        while(oscMessages.TryDequeue(out message)) {
            ReceivedOscMessage?.Invoke(message);
        }
    }

    private void OnOscPacket(OscPacket packet) {
        if (packet is OscBundle bundle) {
            foreach(var m in bundle.Messages) {
                oscMessages.Enqueue(m);
            }
        }

        if (packet is OscMessage message) {
            oscMessages.Enqueue(message);
        }
    }
}
