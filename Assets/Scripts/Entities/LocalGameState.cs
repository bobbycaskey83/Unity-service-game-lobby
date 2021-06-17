using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobbyRooms
{
    /// <summary>
    /// Current state of the local Game.
    /// Set as a flag to allow for the unity inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum GameState
    {
        Menu = 1,
        Lobby = 2,
        InGame = 4,
        Joining = 8
    }

    /// <summary>
    /// Awaits player input to change the local game Data
    /// </summary>
    [System.Serializable]
    public class LocalGameState : Observed<LocalGameState>
    {
        GameState m_State = GameState.Menu;

        public GameState State
        {
            get => m_State;
            set
            {
                m_State = value;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LocalGameState oldObserved)
        {
            m_State = oldObserved.State;
            OnChanged(this);
        }
    }
}
