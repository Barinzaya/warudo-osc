using OscCore;
using OscCore.Address;
using System;
using System.Collections.Generic;
using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

[PluginType(Id = "com.barinzaya.oscinput", Name = "OSC Input", Version = "0.2.3", Author = "Barinzaya", Description = "Adds an On OSC Message node.",
    NodeTypes = new[] { typeof(OscInputNode) })]
public class OscInputPlugin : Plugin {
    public const int OSC_SERVER_PORT = 19190;

    private OscListener listener;

    public delegate void OscMessageHandler(OscMessage message);
    private Dictionary<string, HashSet<OscMessageHandler>> handlers = new();

    protected override void OnCreate() {
        base.OnCreate();
        listener = new(OSC_SERVER_PORT);
    }

    protected override void OnDestroy() {
        listener.Dispose();
        base.OnDestroy();
    }

    public override void OnPreUpdate() {
        base.OnPreUpdate();

        while (listener.TryGetMessage(out var message)) {
            DispatchMessage(message);
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

    private void DispatchMessage(OscMessage message) {
        OscAddress address;
        try {
            address = new(message.Address);
        } catch (ArgumentException) {
            return;
        }

        if (address.IsLiteral) {
            HashSet<OscMessageHandler> addressHandlers;
            if (handlers.TryGetValue(message.Address, out addressHandlers)) {
                foreach (var handler in addressHandlers) {
                    handler(message);
                }
            }
        } else {
            foreach (var pair in handlers) {
                if (address.Match(pair.Key)) {
                    foreach (var handler in pair.Value) {
                        handler(message);
                    }
                }
            }
        }
    }
}
