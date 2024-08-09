using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;

namespace Quantum.Demo
{
    public class UIReconnecting : UIScreen<UIReconnecting>, IConnectionCallbacks, IMatchmakingCallbacks
    {
        private int rejoinIterations;

        public override void OnShowScreen(bool first)
        {
            rejoinIterations = 0;
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
            JoinOrRejoin(ReconnectInformation.Instance.Room, PhotonServerSettings.Instance.CanRejoin);
        }
        
        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected: {cause}");

            UIMain.Client = null;
            ReconnectInformation.Reset();

            switch (cause)
            {
                case DisconnectCause.DisconnectByClientLogic:
                    HideScreen();
                    UIConnect.ShowScreen();
                    break;
                
                default:
                    UIDialog.Show("Reconnecting Failed", cause.ToString(), () =>
                    {
                        HideScreen();
                        UIConnect.ShowScreen();
                    });
                    break;
            }
        }

        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }
        
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnCreatedRoom() { }
        public void OnCreateRoomFailed(short returnCode, string message) { }

        public void OnJoinedRoom()
        {
            Debug.Log($"Joined or rejoined room '{UIMain.Client.CurrentRoom.Name}' successfully as actor '{UIMain.Client.LocalPlayer.ActorNumber}'");
            HideScreen();
            UIRoom.Instance.IsRejoining = true;
            UIRoom.ShowScreen();
        }

        public async void OnJoinRoomFailed(short returnCode, string message)
        {
            switch (returnCode)
            {
                case ErrorCode.JoinFailedFoundActiveJoiner:
                    if (rejoinIterations++ < 10)
                    {
                        Debug.Log($"Rejoining failed, player is still marked active in the room. Trying again ({rejoinIterations}/10)");
                        await System.Threading.Tasks.Task.Delay(1000);
                        JoinOrRejoin(ReconnectInformation.Instance.Room, PhotonServerSettings.Instance.CanRejoin);
                        return;
                    }

                    break;

                case ErrorCode.JoinFailedWithRejoinerNotFound:
                    JoinOrRejoin(ReconnectInformation.Instance.Room);
                    return;
            }

            Debug.LogError($"Joining or rejoining room failed with error '{returnCode}': {message}");
            UIDialog.Show("Joining Room Failed", message, () => UIMain.Client.Disconnect());
        }
        public void OnJoinRandomFailed(short returnCode, string message) { }
        public void OnLeftRoom() { }
        
        private void JoinOrRejoin(string roomName, bool rejoin = false)
        {
            if (rejoin)
            {
                Debug.Log($"Trying to rejoin room '{roomName}");
                if (UIMain.Client.OpRejoinRoom(roomName)) 
                    return;
                
                Debug.LogError("Failed to send rejoin room operation");
                UIMain.Client.Disconnect();
                return;
            }
            
            Debug.Log($"Trying to join room '{roomName}'");
            if (UIMain.Client.OpJoinRoom(new EnterRoomParams { RoomName = roomName })) 
                return;
            
            Debug.LogError("Failed to send join room operation");
            UIMain.Client.Disconnect();

        }

    }
}