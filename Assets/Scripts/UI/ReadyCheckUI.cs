using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class ReadyCheckUI : MonoBehaviour
    {
        public void OnReadyButton()
        {
            ChangeState(UserStatus.Ready);
        }
        public void OnCancelButton()
        {
            ChangeState(UserStatus.Lobby);
        }
        private void ChangeState(UserStatus status)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeLobbyUserState, status);
        }
    }
}
