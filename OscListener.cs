using OscCore;
using OscCore.LowLevel;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class OscListener : IDisposable {
    private UdpClient client;
    private ConcurrentQueue<OscMessage> messages;

    public OscListener(int port) {
        var endpoint = new IPEndPoint(IPAddress.Any, 19190);
        client = new(endpoint);
        messages = new();

        var callback = new AsyncCallback(ReceiveCallback);
        client.BeginReceive(callback, null);
    }

    public bool TryGetMessage(out OscMessage message) {
        return messages.TryDequeue(out message);
    }

    public void Dispose() {
        client.Dispose();
    }

    private void ReceiveCallback(IAsyncResult result) {
        byte[] data;
        IPEndPoint ip = null;

        try {
            data = client.EndReceive(result, ref ip);
        } catch (ObjectDisposedException) {
            return;
        }

        var callback = new AsyncCallback(ReceiveCallback);
        client.BeginReceive(callback, null);

        try {
            var packet = OscPacket.Read(data, 0, data.Length);
            ReceivePacket(packet);
        } catch (OscException) {
            // TODO: Log
            return;
        }
    }

    private void ReceivePacket(OscPacket packet) {
        switch(packet) {
            case OscBundle bundle:
                for(var i = 0; i < bundle.Count; i++) {
                    ReceivePacket(bundle[i]);
                }

                break;

            case OscMessage message:
                messages.Enqueue(message);
                break;
        }
    }
}
