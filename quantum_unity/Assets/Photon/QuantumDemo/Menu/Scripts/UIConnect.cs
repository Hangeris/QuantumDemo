using System;
using Photon.Realtime;
using UnityEngine;
using UI = UnityEngine.UI;

namespace Quantum.Demo
{
    public class UIConnect : UIScreen<UIConnect>
    {
        [Header("References")]
        [SerializeField] private PhotonRegions selectableRegions;
        [SerializeField] private PhotonAppVersions selectableAppVersion;
     
        [Header("UI References")]
        [SerializeField] private UI.Dropdown regionDropdown;
        [SerializeField] private UI.Dropdown appVersionDropdown;
        [SerializeField] private UI.InputField usernameInputField;
        [SerializeField] private UI.Button reconnectButton;

        public static int LastSelectedAppVersion
        {
            get => PlayerPrefs.GetInt("Quantum.Demo.UIConnect.LastSelectedAppVersion");
            set => PlayerPrefs.SetInt("Quantum.Demo.UIConnect.LastSelectedAppVersion", value);
        }
        private static string LastSelectedRegion
        {
            get => PlayerPrefs.GetString("Quantum.Demo.UIConnect.LastSelectedRegion", PhotonServerSettings.Instance.AppSettings.FixedRegion);
            set => PlayerPrefs.SetString("Quantum.Demo.UIConnect.LastSelectedRegion", value);
        }
        private static string LastUsername
        {
            get => PlayerPrefs.GetString("Quantum.Demo.UIConnect.LastUsername", Guid.NewGuid().ToString());
            set => PlayerPrefs.SetString("Quantum.Demo.UIConnect.LastUsername", value);
        }

        protected new void Awake()
        {
            base.Awake();

            usernameInputField.text = LastUsername;

            var appSettings = PhotonServerSettings.Instance.AppSettings;

            regionDropdown.AddOptions(PhotonRegions.CreateDefaultDropdownOptions(out int selectedOption, LastSelectedRegion, appSettings, selectableRegions));
            regionDropdown.value = selectedOption;
            regionDropdown.transform.parent.gameObject.SetActive(string.IsNullOrEmpty(appSettings.Server));

            appVersionDropdown.AddOptions(PhotonAppVersions.CreateDefaultDropdownOptions(appSettings, selectableAppVersion));
            appVersionDropdown.value = LastSelectedAppVersion;
        }

        public override void OnShowScreen(bool first)
        {
            base.OnShowScreen(first);

            reconnectButton.interactable = ReconnectInformation.Instance.IsValid;
        }

        public void OnAppVersionHelpButtonClicked()
        {
            UIDialog.Show("AppVersion", "The AppVersion (string) separates clients connected to the cloud into different groups. This is important to maintain simultaneous different live version and the matchmaking.\n\nChoosing 'Private' in the demo menu f.e. will only allow players to find each other when they are using the exact same build.");
        }

        public void OnConnectClicked()
        {
            if (string.IsNullOrEmpty(usernameInputField.text.Trim()))
            {
                UIDialog.Show("Error", "User name not set.");
                return;
            }

            var appSettings = PhotonServerSettings.CloneAppSettings(PhotonServerSettings.Instance.AppSettings);

            LastUsername = usernameInputField.text;
            Debug.Log($"Using user name '{usernameInputField.text}'");

            UIMain.Client = new QuantumLoadBalancingClient(PhotonServerSettings.Instance.AppSettings.Protocol);

            if (!TryOverwriteRegion(appSettings))
                return;

            // Append selected app version
            appSettings.AppVersion += PhotonAppVersions.AppendAppVersion((PhotonAppVersions.Type)appVersionDropdown.value, selectableAppVersion);
            LastSelectedAppVersion = appVersionDropdown.value;
            Debug.Log($"Using app version '{appSettings.AppVersion}'");

            if (UIMain.Client.ConnectUsingSettings(appSettings, usernameInputField.text))
            {
                HideScreen();
                UIConnecting.ShowScreen();
            }
            else
            {
                Debug.LogError($"Failed to connect with app settings: '{appSettings.ToStringFull()}'");
            }
        }

        public void OnReconnectClicked()
        {
            if (UIMain.Client != null)
            {
                if (HandleReconnectWithClient()) 
                    return;
            }

            if (UIMain.Client == null && ReconnectInformation.Instance.IsValid)
            {
                if (HandleReconnectWithoutClient()) 
                    return;
            }

            Debug.LogError($"Cannot reconnect");
            ReconnectInformation.Reset();
            reconnectButton.interactable = false;
        }

        private static bool HandleReconnectWithoutClient()
        {
            UIMain.Client = new QuantumLoadBalancingClient(PhotonServerSettings.Instance.AppSettings.Protocol)
            {
                UserId = ReconnectInformation.Instance.UserId
            };

            var appSettings = PhotonServerSettings.CloneAppSettings(PhotonServerSettings.Instance.AppSettings);
            appSettings.FixedRegion = ReconnectInformation.Instance.Region;
            appSettings.AppVersion = ReconnectInformation.Instance.AppVersion;

            if (!UIMain.Client.ConnectUsingSettings(appSettings, LastUsername)) 
                return false;
            
            Debug.Log($"Reconnecting to nameserver using reconnect info {ReconnectInformation.Instance}");
            HideScreen();
            UIReconnecting.ShowScreen();
            return true;
        }
        private static bool HandleReconnectWithClient()
        {
            if (PhotonServerSettings.Instance.CanRejoin)
            {
                if (UIMain.Client.ReconnectAndRejoin())
                {
                    Debug.Log($"Reconnecting and rejoining");
                    HideScreen();
                    UIReconnecting.ShowScreen();
                    return true;
                }
            }
            else
            {
                // Reconnect to master server and join back into the room
                if (UIMain.Client.ReconnectToMaster())
                {
                    Debug.Log($"Reconnecting to master server");
                    HideScreen();
                    UIReconnecting.ShowScreen();
                    return true;
                }
            }

            return false;
        }
        
        private bool TryOverwriteRegion(AppSettings appSettings)
        {
            if (string.IsNullOrEmpty(appSettings.Server) == false)
            {
                appSettings.FixedRegion = string.Empty;
                return true;
            }
     
            // Connections to nameserver require an app id
            if (string.IsNullOrEmpty(appSettings.AppIdRealtime.Trim()))
            {
                UIDialog.Show("Error", "AppId not set.\n\nSearch or create PhotonServerSettings and configure an AppId.");
                return false;
            }

            if (regionDropdown.value == 0)
            {
                appSettings.FixedRegion = string.Empty;
                LastSelectedRegion = "best";
            }
            else if (selectableRegions != null && regionDropdown.value <= selectableRegions.Regions.Count)
            {
                appSettings.FixedRegion = selectableRegions.Regions[regionDropdown.value - 1].Token;
                LastSelectedRegion = appSettings.FixedRegion;
            }

            Debug.Log($"Using region '{LastSelectedRegion}'");
            return true;
        }
    }
}