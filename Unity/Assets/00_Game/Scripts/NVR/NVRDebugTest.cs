using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BeardedManStudios.Network;

[CustomEditor(typeof(NVRDebugTest))]
public class NVRDebugTestEditor
    : Editor
{
    private bool _status_gui;
    private bool _connection_gui;

    private NetWorker net;

    public override void OnInspectorGUI()
    {
        // networking doesn't work in editor mode properly
        if (!Application.isPlaying)
            return;

        var obj = target as NVRDebugTest;

        base.OnInspectorGUI();

        if (net == null)
        {
            if (GUILayout.Button("Host"))
                net = obj.StartHost();
            if (GUILayout.Button("Client"))
                net = obj.StartClient();
        }
        else
        {
            if (GUILayout.Button("Stop"))
            {
                net.Disconnect();
                net = null;
                return;
            }

            // status
            GUILayout.Label(net.ToString());

            GUILayout.Label(string.Format("Server: {0}", net.IsServer));
            GUILayout.Label(string.Format("Connected: {0}", net.Connected));
            GUILayout.Label(string.Format("Disconnected: {0}", net.Disconnected));
            GUILayout.Label(string.Format("Server: {0}", net.IsServer));
            GUILayout.Label(string.Format("Host: {0}:{1}", net.Host, net.Port));
            GUILayout.Label(string.Format("Connections: {0}/{1}", net.Connections, net.MaxConnections));
            GUILayout.Label(string.Format("Bandwidth: {0} in, {1} out", net.BandwidthIn, net.BandwidthOut));

            GUILayout.Label("Self:");

            OnInspectorGUINetworkingPlayer(net.Me);

            foreach (var player in net.Players)
                OnInspectorGUINetworkingPlayer(player);
        }
    }

    private void OnInspectorGUINetworkingPlayer(NetworkingPlayer player)
    {
        if (player == null)
        {
            GUILayout.Label("null player");
            return;
        }

        GUILayout.Label(string.Format("Player: {0}", player.Name));
        GUILayout.Label(string.Format("> object: {0}", player.PlayerObject));
        GUILayout.Label(string.Format("> position: {0}", player.Position));
        GUILayout.Label(string.Format("> address: {0}", player.Ip));
        GUILayout.Label(string.Format("> message group: {0}", player.MessageGroup));
        GUILayout.Label(string.Format("> last ping: {0}", player.LastPing.ToLocalTime()));
    }
}

public class NVRDebugTest
    : MonoBehaviour
{
    public bool Host = false;
    public string IP = "127.0.0.1";
    public int Port = 15937;
    public bool WinRT;
    public string OverrideIP;
    public bool AllowWebPlayerConnection;
    public bool RelayToAll;
    public bool UseNAT;

    public List<NetWorker> Workers = new List<NetWorker>();

    private string OverrideIPParam { get { return string.IsNullOrEmpty(OverrideIP) ? null : OverrideIP; } }

    public void Start()
    {
    }

    public NetWorker StartHost()
    {
        Debug.Log("Hosting...");

        NetWorker nw = Networking.Host((ushort)Port, Networking.TransportationProtocolType.UDP, 8, WinRT, OverrideIPParam, AllowWebPlayerConnection, RelayToAll, UseNAT);

        nw.connected += () => Debug.Log("connected");
        nw.disconnected += () => Debug.Log("disconnected");

        Workers.Add(nw);

        return nw;
    }

    public NetWorker StartClient()
    {
        Debug.Log("Client...");

        NetWorker nw = Networking.Connect(IP, (ushort)Port, Networking.TransportationProtocolType.UDP, false, UseNAT);

        nw.connected += () => Debug.Log("connected");
        nw.disconnected += () => Debug.Log("disconnected");

        Workers.Add(nw);

        return nw;
    }
}
