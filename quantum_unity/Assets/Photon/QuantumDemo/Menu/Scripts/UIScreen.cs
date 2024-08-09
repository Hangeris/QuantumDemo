using Photon.Realtime;
using UnityEngine;

namespace Quantum.Demo
{
    public abstract class UIScreen : MonoBehaviour
    {
        public GameObject Panel;
        public bool StartEnabled;

        public virtual bool VerifyCanShow() => true;
        public virtual void OnShowScreen(bool first) { }
        public virtual void OnHideScreen(bool first) { }
        public virtual void OnScreenDestroy() { }
        public virtual void ResetScreenToStartState(bool cascade) { }
        public bool IsScreenInstanceVisible() => Panel.activeInHierarchy;
    }

    public abstract class UIScreen<T> : UIScreen where T : UIScreen
    {
        private static bool firstShow;
        private static bool firstHide;

        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (Instance)
            {
                Instance.gameObject.SetActive(false);
                Destroy(Instance.gameObject);
            }

            Instance = (T)(object)this;
            Instance.ResetScreenToStartState(false);
        }
        
        public static void DestroyScreen()
        {
            if (!Instance) 
                return;

            Instance.OnScreenDestroy();
            Destroy(Instance.gameObject);
            Instance = null;
        }

        public static bool IsScreenVisible() => Instance && Instance.Panel.activeInHierarchy;

        public static void ShowScreen(bool condition)
        {
            if (condition == IsScreenVisible()) 
                return;
            
            if (condition)
            {
                ShowScreen();
            }
            else
            {
                HideScreen();
            }
        }
        
        public override void ResetScreenToStartState(bool cascade)
        {
            firstShow = true;
            firstHide = true;

            if (StartEnabled)
            {
                ShowScreen();
            }
            else
            {
                HideScreen();
            }

            if (!cascade) 
                return;
            
            foreach (var screen in GetComponentsInChildren<UIScreen>())
            {
                if (screen == this) 
                    continue;
                
                screen.ResetScreenToStartState(false);
            }
        }
        
        public void ShowScreenInstance()
        {
            ShowScreen();
        }

        public void HideScreenInstance()
        {
            HideScreen();
        }

        public void ToggleScreenInstance()
        {
            ToggleScreen();
        }
        
        protected static void ShowScreen()
        {
            if (!Instance) 
                return;
            
            if (!Instance.VerifyCanShow()) 
                return;
            
            Instance.Panel.Show();
            Instance.OnShowScreen(firstShow);
            firstShow = false;
        }

        protected static void HideScreen()
        {
            if (!Instance) 
                return;
            
            Instance.Panel.Hide();
            Instance.OnHideScreen(firstHide);
            firstHide = false;
        }

        private static void ToggleScreen()
        {
            if (IsScreenVisible())
            {
                HideScreen();
            }
            else
            {
                ShowScreen();
            }
        }
    }
}