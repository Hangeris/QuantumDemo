using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;

namespace Quantum.Demo
{
    public class UIConnecting : UIScreen<UIConnecting>, IConnectionCallbacks, IMatchmakingCallbacks
    {
        [SerializeField] private  RuntimeConfigContainer runtimeConfigContainer;
        
        private EnterRoomParams enterRoomParams;

        public override void OnShowScreen(bool first)
        {
            UIMain.Client?.AddCallbackTarget(this);
        }
        public override void OnHideScreen(bool first)
        {
            UIMain.Client?.RemoveCallbackTarget(this);
        }
        
        public void OnDisconnectClicked()
        {
            UIMain.Client.Disconnect();
        }

        public void OnConnected() { }

        public void OnConnectedToMaster()
        {
            if (string.IsNullOrEmpty(UIMain.Client.CloudRegion) == false)
            {
                Debug.Log($"Connected to master server in region '{UIMain.Client.CloudRegion}'");
            }
            else
            {
                Debug.Log($"Connected to master server '{UIMain.Client.MasterServerAddress}'");
            }

            Debug.Log($"UserId: {UIMain.Client.UserId}");

            var defaultMapGuid = FindDefaultMapGuid();
            var joinRandomParams = new OpJoinRandomRoomParams();
            GenerateEnterRoomParams(defaultMapGuid);

            StartRandomMatchmaking(joinRandomParams);
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected: {cause}");

            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                UIDialog.Show("Connection Failed", cause.ToString(), () =>
                {
                    HideScreen();
                    UIConnect.ShowScreen();
                });
            }
            else
            {
                HideScreen();
                UIConnect.ShowScreen();
            }
        }

        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnCreatedRoom() { }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            UIDialog.Show("Error", $"Create room failed [{returnCode}]: '{message}'", () => UIMain.Client?.Disconnect());
        }

        public void OnJoinedRoom()
        {
            Debug.Log($"Entered room '{UIMain.Client.CurrentRoom.Name}' as actor '{UIMain.Client.LocalPlayer.ActorNumber}'");
            HideScreen();
            UIRoom.ShowScreen();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            UIDialog.Show("Error", $"Joining room failed [{returnCode}]: '{message}'", () => UIMain.Client?.Disconnect());
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            if (returnCode == ErrorCode.NoRandomMatchFound)
            {
                if (!UIMain.Client.OpCreateRoom(enterRoomParams))
                {
                    UIDialog.Show("Error", "Failed to send join or create room operation", () => UIMain.Client?.Disconnect());
                }
            }
            else
            {
                UIDialog.Show("Error", $"Join random failed [{returnCode}]: '{message}'", () => UIMain.Client?.Disconnect());
            }
        }

        public void OnLeftRoom()
        {
            UIDialog.Show("Error", "Left the room unexpectedly", () => UIMain.Client?.Disconnect());
        }
        
        private void StartRandomMatchmaking(OpJoinRandomRoomParams joinRandomParams)
        {
            Debug.Log("Starting random matchmaking");

            if (UIMain.Client.OpJoinRandomOrCreateRoom(joinRandomParams, enterRoomParams)) 
                return;
            
            UIMain.Client.Disconnect();
            Debug.LogError($"Failed to send join random operation");
        }

        private void GenerateEnterRoomParams(long defaultMapGuid)
        {
            enterRoomParams = new EnterRoomParams
            {
                RoomOptions = new RoomOptions
                {
                    IsVisible = true,
                    MaxPlayers = Input.MAX_COUNT,
                    Plugins = new[] { "QuantumPlugin" },
                    CustomRoomProperties = new Hashtable
                    {
                        { "HIDE-ROOM", false },
                        { "MAP-GUID", defaultMapGuid },
                    },
                    PlayerTtl = PhotonServerSettings.Instance.PlayerTtlInSeconds * 1000,
                    EmptyRoomTtl = PhotonServerSettings.Instance.EmptyRoomTtlInSeconds * 1000
                }
            };
        }

        private long FindDefaultMapGuid()
        {
            var defaultMapGuid = 0L;
            if (runtimeConfigContainer != null && runtimeConfigContainer.Config.Map.Id.IsValid)
            {
                defaultMapGuid = runtimeConfigContainer.Config.Map.Id.Value;
            }
            else
            {
                long.TryParse(UIRoom.LastMapSelected, out defaultMapGuid);

                var allMapsInResources = UnityEngine.Resources.LoadAll<MapAsset>(QuantumEditorSettings.Instance.DatabasePathInResources);
                if (allMapsInResources.All(m => m.AssetObject.Guid != defaultMapGuid))
                {
                    defaultMapGuid = 0;
                }
            }

            if (defaultMapGuid == 0)
            {
                var allMapsInResources = UnityEngine.Resources.LoadAll<MapAsset>(QuantumEditorSettings.Instance.DatabasePathInResources);
                Assert.Always(allMapsInResources.Length > 0);
                defaultMapGuid = allMapsInResources[0].AssetObject.Guid.Value;
            }

            return defaultMapGuid;
        }
    }
}