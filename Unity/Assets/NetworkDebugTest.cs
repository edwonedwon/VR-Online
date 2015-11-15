using UnityEngine;
using BeardedManStudios.Network;

public class NetworkDebugTest
	: MonoBehaviour
{
    public string IP = "108.47.46.240";
    public int Port = 15937;
    public bool Host = false;
    public bool NAT = false;

    public void Start()
    {
        if (Host)
        {
            Debug.Log("Hosting...");

            NetWorker nw = Networking.Host((ushort)Port, Networking.TransportationProtocolType.UDP, 8, false, null, false, true, NAT);

            nw.connected += () => Debug.Log("connected");
            nw.disconnected += () => Debug.Log("disconnected");
        }
        else
        {
            Debug.Log("Client...");

            NetWorker nw = Networking.Connect(IP, (ushort)Port, Networking.TransportationProtocolType.UDP, false, NAT);

            nw.connected += () => Debug.Log("connected");
            nw.disconnected += () => Debug.Log("disconnected");
        }
    }
}
