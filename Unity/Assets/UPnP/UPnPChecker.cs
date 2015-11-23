using UnityEngine;
using System.Threading;

namespace NATUPnP
{
    public class UPnPChecker
        : MonoBehaviour
    {
        public bool Status = false;
        public string ExternalIP = "unknown";

        private Thread _scan_thread;
        private UPnP.NAT _nat;

        void OnApplicationQuit()
        {
            _nat.Disconnect();
        }

        void Scan()
        {
            _nat = new UPnP.NAT();

            Status = false;
            ExternalIP = "scanning...";

            Debug.Log("scanning");

            Thread.Sleep(100);

            try
            {
                Status = _nat.Discover();
                ExternalIP = _nat.GetExternalIP().ToString();
                //ForwardPort = UPnP.NAT.ForwardPort
            }
            catch
            {
                Status = false;
                ExternalIP = "invalid";
            }

            Debug.Log("scanning complete");
        }

        void Start()
        {
            Debug.Log("spawning");
            _scan_thread = new Thread(() => Scan());
            _scan_thread.Start();
            Debug.Log("spawning complete");

            Thread.Sleep(100);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Break();
            }
        }
    }
}
