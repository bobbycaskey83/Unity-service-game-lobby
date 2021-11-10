using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the menu for a player creating a new lobby.
    /// </summary>
    public class CreateMenuUI : UIPanelBase
    {
        private LocalLobby.LobbyData m_ServerRequestData = new LocalLobby.LobbyData{ LobbyName = "New Lobby", MaxPlayerCount = 4 };

        public override void Start()
        {
            base.Start();
            Hide();
        }
        public void SetServerName(string serverName)
        {
            m_ServerRequestData.LobbyName = serverName;
        }

        public void SetServerPrivate(bool priv)
        {
            m_ServerRequestData.Private = priv;
        }

        public void OnCreatePressed()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.CreateLobbyRequest, m_ServerRequestData);
        }
    }
}
