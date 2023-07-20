using OscCore;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

public class OscListener : IDisposable {
    private UdpClient client;
    private ConcurrentQueue<OscPacket> packets;

    public OscListener(int port) {
        var endpoint = new IPEndPoint(IPAddress.Any, 19190);
        client = new(endpoint);
        packets = new();

        var callback = new AsyncCallback(ReceiveCallback);
        client.BeginReceive(callback, null);
    }

    public bool TryGetPacket(out OscPacket packet) {
        return packets.TryDequeue(out packet);
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
            packets.Enqueue(packet);
        } catch (OscException) {
            // TODO: Log
            return;
        }
    }
}
