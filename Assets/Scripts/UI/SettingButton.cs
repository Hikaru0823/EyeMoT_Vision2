using UnityEngine;

public class SettingsButton : MonoBehaviour
{
    public void OnClicked()
    {
        if(InterfaceManager.Instance.MainPanelManager.GetCurrentPanelName() == "Settings")
            OnReturnClicked();
        else
            InterfaceManager.Instance.MainPanelManager.OpenPanel("Settings");
    }

    public void OnReturnClicked()
    {
        if(NetworkBootStrap.Instance.CurrentRole == ClientManager.NetworkRole.None)
            InterfaceManager.Instance.MainPanelManager.OpenPanel("HostClientControll");
        else
            InterfaceManager.Instance.MainPanelManager.OpenPanel("View");
    }
}