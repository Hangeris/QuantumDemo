using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UI = UnityEngine.UI;

namespace Quantum.Demo
{
    using Player = Photon.Realtime.Player;

    public class UIRoom : UIScreen<UIRoom>, IInRoomCallbacks, IOnEventCallback, IConnectionCallbacks, IMatchmakingCallbacks
    {
        private class MapInfo
        {
            public string Scene;
            public AssetGuid Guid;

            public static List<MapInfo> CreateTable()
            {
                var maps = UnityEngine.Resources.LoadAll<MapAsset>(QuantumEditorSettings.Instance.DatabasePathInResources);
                var list = maps.Select(x => new MapInfo { Guid = x.Settings.Guid, Scene = x.Settings.Scene }).ToList();
                list.Sort((a, b) => string.Compare(a.Scene, b.Scene, StringComparison.Ordinal));
                return list;
            }
        }
        
        [Header("References")]
        [SerializeField] private UI.Button startButton;
        [SerializeField] private UI.Text roomNameText;
        [SerializeField] private UI.Text regionText;
        [SerializeField] private GameObject waitingMessageGo;
        [SerializeField] private UI.Dropdown mapSelectDropdown;
        [SerializeField] private UI.Text clientCountText;
        [SerializeField] private RectTransform playerGridRectTransform;
        [SerializeField] private UIRoomPlayer uiRoomPlayer;
        [SerializeField] private ClientIdProvider.Type idProvider = ClientIdProvider.Type.NewGuid;
        [SerializeField] private RuntimeConfigContainer runtimeConfigContainer;

        [Header("Settings")]
        [SerializeField] private bool spectate = false;
        
        private List<MapInfo> mapInfo;
        private List<UIRoomPlayer> players = new ();
        
        public bool IsRejoining { get; set; }
        public static string LastMapSelected
        {
            get => PlayerPrefs.GetString("Quantum.Demo.UIRoom.LastMapSelected", "0");
            set => PlayerPrefs.SetString("Quantum.Demo.UIRoom.LastMapSelected", value);
        }
        
        private void Start()
        {
            uiRoomPlayer.Hide();
        }

#region Unity UI Callbacks

        public void OnDisconnectClicked()
        {
            UIMain.Client.Disconnect();
        }

        public void OnStartClicked()
        {
            if (UIMain.Client == null || !UIMain.Client.InRoom || !UIMain.Client.LocalPlayer.IsMasterClient || !UIMain.Client.CurrentRoom.IsOpen) 
                return;
            
            if (!UIMain.Client.OpRaiseEvent((byte)UIMain.PhotonEventCode.StartGame, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable))
            {
                Debug.LogError($"Failed to send start game event");
            }
        }

        public void OnMapSelectionChanged(int value)
        {
            if (UIMain.Client == null || !UIMain.Client.InRoom || !UIMain.Client.LocalPlayer.IsMasterClient) 
                return;
            
            var selectedScene = mapSelectDropdown.options[value].text;
            var selectedGuid = mapInfo.FirstOrDefault(m => m.Scene == selectedScene)?.Guid;
            var ht = new ExitGames.Client.Photon.Hashtable { { "MAP-GUID", selectedGuid.Value.Value } };
            UIMain.Client.CurrentRoom.SetCustomProperties(ht);
            LastMapSelected = selectedGuid.Value.Value.ToString();
        }
        
        public void OnHideRoomOnStartChanged(bool value)
        {
            if (UIMain.Client == null || !UIMain.Client.InRoom || !UIMain.Client.LocalPlayer.IsMasterClient) 
                return;
            
            var ht = new ExitGames.Client.Photon.Hashtable { { "HIDE-ROOM", value } };
            UIMain.Client.CurrentRoom.SetCustomProperties(ht);
        }

#endregion // Unity UI Callbacks

#region UIScreen

        public override void OnShowScreen(bool first)
        {
            UIMain.Client?.AddCallbackTarget(this);

            object mapGuidValue = null;

            if (UIMain.Client != null &&
                UIMain.Client.CurrentRoom.CustomProperties.TryGetValue("MAP-GUID", out mapGuidValue) &&
                UIMain.Client.CurrentRoom.CustomProperties.TryGetValue("STARTED", out var started))
            {
                // The game is already running as indicated by the room property. Run the start game procedure.
                Debug.Log("Game already running");
                var mapGuid = (AssetGuid)(long)mapGuidValue;
                StartQuantumGame(mapGuid);
                HideScreen();
                UIGame.ShowScreen();
            }
            else
            {
                var mapGuid = (AssetGuid)(long)mapGuidValue;
                mapInfo = MapInfo.CreateTable();
                Assert.Always(mapInfo.Count > 0);

                mapSelectDropdown.ClearOptions();
                mapSelectDropdown.AddOptions(mapInfo.Select(m => m.Scene).ToList());
                mapSelectDropdown.value = 0;

                var index = mapInfo.FindIndex(m => m.Guid == mapGuid);
                mapSelectDropdown.value = index >= 0 ? index : 0;

                UpdateUI();
            }
        }

        public override void OnHideScreen(bool first)
        {
            UIMain.Client?.RemoveCallbackTarget(this);
            IsRejoining = false;
        }

#endregion // UIScreen

#region UIRoom

        private static String FormatPlayerName(Player player)
        {
            var playerName = player.IsLocal ? $"<color=white>{player.NickName}</color>" : player.NickName;
            if (player.IsMasterClient)
            {
                playerName += " (Master Client)";
            }

            return playerName;
        }

        private void UpdateUI()
        {
            if (UIMain.Client == null || UIMain.Client.InRoom == false)
            {
                UIMain.Client?.Disconnect();
                return;
            }

            var isMasterClient = UIMain.Client.LocalPlayer.IsMasterClient;
            waitingMessageGo.Toggle(isMasterClient == false);
            startButton.Toggle(isMasterClient);
            mapSelectDropdown.interactable = isMasterClient;
            
            roomNameText.text = UIMain.Client.CurrentRoom.Name;
            regionText.text = UIMain.Client.CloudRegion.ToUpper();
            
            UpdateSelectedMap();
            
            clientCountText.text = UIMain.Client.CurrentRoom.PlayerCount.ToString();

            UpdatePlayerUI();
        }

        private void UpdatePlayerUI()
        {
            while (players.Count < UIMain.Client.CurrentRoom.MaxPlayers)
            {
                var instance = Instantiate(uiRoomPlayer);
                instance.transform.SetParent(playerGridRectTransform, false);
                instance.transform.SetAsLastSibling();

                players.Add(instance);
            }

            var i = 0;
            foreach (var player in UIMain.Client.CurrentRoom.Players)
            {
                players[i].Name.text = FormatPlayerName(player.Value);
                players[i].Show();
                i++;
            }

            for (; i < players.Count; ++i)
            {
                players[i].Hide();
            }
        }

        private void UpdateSelectedMap()
        {
            if (!UIMain.Client.CurrentRoom.CustomProperties.TryGetValue("MAP-GUID", out var mapGuid)) 
                return;
            
            var selectedScene = mapInfo.FirstOrDefault(m => m.Guid == (long)mapGuid)?.Scene;
            var mapSelectIndex = mapSelectDropdown.options.FindIndex(0, mapSelectDropdown.options.Count, optionData => optionData.text == selectedScene);
            if (mapSelectDropdown.value == mapSelectIndex) 
                return;
            
            var dropdownValueField = (typeof(UI.Dropdown)).GetField("m_Value", BindingFlags.NonPublic | BindingFlags.Instance);
            dropdownValueField.SetValue(mapSelectDropdown, mapSelectIndex);
            mapSelectDropdown.RefreshShownValue();
        }

        #endregion // UIRoom

#region IInRoomCallbacks

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdateUI();
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdateUI();
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            UpdateUI();
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            UpdateUI();
        }

#endregion // IInRoomCallbacks

#region IOnEventCallback

        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case (byte)UIMain.PhotonEventCode.StartGame:

                    UIMain.Client.CurrentRoom.CustomProperties.TryGetValue("MAP-GUID", out object mapGuidValue);
                    if (mapGuidValue == null)
                    {
                        UIDialog.Show("Error", "Failed to read the map guid during start", () => UIMain.Client?.Disconnect());
                        return;
                    }

                    if (UIMain.Client.LocalPlayer.IsMasterClient)
                    {
                        // Save the started state in room properties for late joiners (TODO: set this from the plugin)
                        var ht = new ExitGames.Client.Photon.Hashtable { { "STARTED", true } };
                        UIMain.Client.CurrentRoom.SetCustomProperties(ht);

                        if (UIMain.Client.CurrentRoom.CustomProperties.TryGetValue("HIDE-ROOM", out var hideRoom) && (bool)hideRoom)
                        {
                            UIMain.Client.CurrentRoom.IsVisible = false;
                        }
                    }

                    StartQuantumGame((AssetGuid)(long)mapGuidValue);

                    HideScreen();
                    UIGame.ShowScreen();

                    break;
            }
        }

        private void StartQuantumGame(AssetGuid mapGuid)
        {
            if (QuantumRunner.Default != null)
            {
                // There already is a runner, maybe because of duplicated calls, button events or race-conditions sending start and not deregistering from event callbacks in time.
                Debug.LogWarning($"Another QuantumRunner '{QuantumRunner.Default.name}' has prevented starting the game");
                return;
            }

            var config = runtimeConfigContainer != null ? RuntimeConfig.FromByteArray(RuntimeConfig.ToByteArray(runtimeConfigContainer.Config)) : new RuntimeConfig();
            config.Map.Id = mapGuid;

            var param = new QuantumRunner.StartParameters
            {
                RuntimeConfig = config,
                DeterministicConfig = DeterministicSessionConfigAsset.Instance.Config,
                ReplayProvider = null,
                GameMode = spectate ? Photon.Deterministic.DeterministicGameMode.Spectating : Photon.Deterministic.DeterministicGameMode.Multiplayer,
                FrameData = IsRejoining ? UIGame.Instance?.FrameSnapshot : null,
                InitialFrame = IsRejoining ? (UIGame.Instance?.FrameSnapshotNumber).Value : 0,
                PlayerCount = UIMain.Client.CurrentRoom.MaxPlayers,
                LocalPlayerCount = spectate ? 0 : 1,
                RecordingFlags = RecordingFlags.None,
                NetworkClient = UIMain.Client,
                StartGameTimeoutInSeconds = 10.0f
            };

            Debug.Log($"Starting QuantumRunner with map guid '{mapGuid}' and requesting {param.LocalPlayerCount} player(s).");

            // Joining with the same client id will result in the same quantum player slot which is important for reconnecting.
            var clientId = ClientIdProvider.CreateClientId(idProvider, UIMain.Client);
            QuantumRunner.StartGame(clientId, param);

            ReconnectInformation.Refresh(UIMain.Client, TimeSpan.FromMinutes(1));
        }

#endregion // IOnEventCallback

#region IConnectionCallbacks

        public void OnConnected() { }
        public void OnConnectedToMaster() { }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected: {cause}");

            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                UIDialog.Show("Disconnected", cause.ToString(), () =>
                {
                    HideScreen();
                    UIConnect.ShowScreen();
                });
                return;
            }

            HideScreen();
            UIConnect.ShowScreen();
        }
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }

#endregion // IConnectionCallbacks

#region IMatchmakingCallbacks
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnCreatedRoom() { }
        public void OnCreateRoomFailed(short returnCode, string message) { }
        public void OnJoinedRoom() { }
        public void OnJoinRoomFailed(short returnCode, string message) { }
        public void OnJoinRandomFailed(short returnCode, string message) { }
        public void OnLeftRoom() { }

#endregion // IMatchmakingCallbacks

    }
}