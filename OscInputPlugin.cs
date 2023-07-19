using SharpOSC; // would rather use OscCore, but there is a zero-tolerance ban on System.IO and OscCore uses System.IO.MemoryStream
using System.Collections.Concurrent;
using System.Collections.Generic;
using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

[PluginType(Id = "com.barinzaya.oscinput", Name = "OSC Input", Version = "0.2.0", Author = "Barinzaya", Description = "Adds an On OSC Message node.",
    NodeTypes = new[] { typeof(OscInputNode) })]
public class OscInputPlugin : Plugin {
    public const int OSC_SERVER_PORT = 19190;

    private UDPListener oscListener;
    private ConcurrentQueue<OscMessage> oscMessages = new();

    public delegate void OscMessageHandler(OscMessage message);
    private Dictionary<string, HashSet<OscMessageHandler>> handlers = new();

    protected override void OnCreate() {
        base.OnCreate();
        oscListener = new(OSC_SERVER_PORT, OnOscPacket);
    }

    protected override void OnDestroy() {
        oscListener.Dispose();
        base.OnDestroy();
    }

    public override void OnPreUpdate() {
        base.OnPreUpdate();

        OscMessage message;
        while(oscMessages.TryDequeue(out message)) {
            HashSet<OscMessageHandler> addressHandlers;
            if (handlers.TryGetValue(message.Address, out addressHandlers)) {
                foreach (var handler in addressHandlers) {
                    handler(message);
                }
            }
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

    public bool AddHandler(string address, OscMessageHandler action) {
        HashSet<OscMessageHandler> addressHandlers;
        if (!handlers.TryGetValue(address, out addressHandlers)) {
            addressHandlers = new();
            handlers[address] = addressHandlers;
        }

        return addressHandlers.Add(action);
    }

    public bool RemoveHandler(string address, OscMessageHandler action) {
        HashSet<OscMessageHandler> addressHandlers;
        if (!handlers.TryGetValue(address, out addressHandlers)) {
            return false;
        }

        var result = addressHandlers.Remove(action);

        if (result && addressHandlers.Count == 0) {
            handlers.Remove(address);
        }

        return result;
    }
}
