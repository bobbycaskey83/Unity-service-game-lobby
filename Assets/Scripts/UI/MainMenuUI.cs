using System.Collections;
using System.Collections.Generic;
using LobbyRooms.UI;
using UnityEngine;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Watches for Changes in the Game State to/from Menu
    /// </summary>
    [RequireComponent(typeof(LocalGameStateObserver))]
    public class MainMenuUI : ObserverPanel<LocalGameState>
    {
        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.Menu)
                Show();
            else
            {
                Hide();
            }
        }
    }
}
