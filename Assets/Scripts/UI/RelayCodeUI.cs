using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Read Only input field (for copy/paste reasons) Watches for the changes in the lobby's Relay Code
    /// </summary>
    public class RelayCodeUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_InputField relayCodeText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            if (!string.IsNullOrEmpty(observed.RelayCode))
            {
                relayCodeText.text = observed.RelayCode;
                Show();
            }
            else
            {
                Hide();
            }
        }
    }
}
