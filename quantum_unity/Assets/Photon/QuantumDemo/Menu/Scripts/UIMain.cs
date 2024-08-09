using UnityEngine;

namespace Quantum.Demo
{
    public class UIMain : MonoBehaviour
    {
        public enum PhotonEventCode : byte
        {
            StartGame = 110
        }
        
        public static QuantumLoadBalancingClient Client { get; set; }

        private void Update()
        {
            Client?.Service();
        }

        private void OnDestroy()
        {
            if (Client != null && Client.IsConnected == true)
            {
                Client.Disconnect();
            }
        }
    }
}