using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobbyRelaySample
{
    [Flags]
    public enum LobbyState
    {
        Lobby = 1,
        CountDown = 2,
        InGame = 4
    }
    
    public struct LobbyInfo
    {
        public string LobbyID { get; set; }
        public string LobbyCode { get; set; }
        public string RelayCode { get; set; }
        public string LobbyName { get; set; }
        public bool Private { get; set; }
        public int MaxPlayerCount { get; set; }
        public LobbyState State { get; set; }
        public long? AllPlayersReadyTime { get; set; }
        
        public LobbyInfo(LobbyInfo existing)
        {
            LobbyID = existing.LobbyID;
            LobbyCode = existing.LobbyCode;
            RelayCode = existing.RelayCode;
            LobbyName = existing.LobbyName;
            Private = existing.Private;
            MaxPlayerCount = existing.MaxPlayerCount;
            State = existing.State;
            AllPlayersReadyTime = existing.AllPlayersReadyTime;
        }

        public LobbyInfo(string lobbyCode)
        {
            LobbyID = null;
            LobbyCode = lobbyCode;
            RelayCode = null;
            LobbyName = null;
            Private = false;
            MaxPlayerCount = -1;
            State = LobbyState.Lobby;
            AllPlayersReadyTime = null;
        }
    }

    /// <summary>
    /// The local lobby data that the game can observe
    /// </summary>
    [System.Serializable]
    public class LocalLobby : Observed<LocalLobby>
    {
        Dictionary<string, LobbyUser> m_LobbyUsers = new Dictionary<string, LobbyUser>();
        public Dictionary<string, LobbyUser> LobbyUsers => m_LobbyUsers;

        #region LocalLobbyData
        private LobbyInfo m_data;
        public LobbyInfo Data
        {
            get { return new LobbyInfo(m_data); }
        }

        float m_CountDownTime;

        public float CountDownTime
        {
            get { return m_CountDownTime; }
            set
            {
                m_CountDownTime = value;
                OnChanged(this);
            }
        }

        DateTime m_TargetEndTime;

        public DateTime TargetEndTime
        {
            get => m_TargetEndTime;
            set
            {
                m_TargetEndTime = value;
                OnChanged(this);
            }
        }

        ServerAddress m_relayServer;

        public ServerAddress RelayServer
        {
            get => m_relayServer;
            set
            {
                m_relayServer = value;
                OnChanged(this);
            }
        }

        #endregion

        public void AddPlayer(LobbyUser user)
        {
            if (m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to lobby: {LobbyID} twice");
                return;
            }

            DoAddPlayer(user);
            OnChanged(this);
        }

        private void DoAddPlayer(LobbyUser user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.onChanged += OnChangedUser;
        }

        public void RemovePlayer(LobbyUser user)
        {
            DoRemoveUser(user);
            OnChanged(this);
        }

        private void DoRemoveUser(LobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.onChanged -= OnChangedUser;
        }

        private void OnChangedUser(LobbyUser user)
        {
            OnChanged(this);
        }

        public string LobbyID
        {
            get => m_data.LobbyID;
            set
            {
                m_data.LobbyID = value;
                OnChanged(this);
            }
        }

        public string LobbyCode
        {
            get => m_data.LobbyCode;
            set
            {
                m_data.LobbyCode = value;
                OnChanged(this);
            }
        }

        public string RelayCode
        {
            get => m_data.RelayCode;
            set
            {
                m_data.RelayCode = value;
                OnChanged(this);
            }
        }

        public string LobbyName
        {
            get => m_data.LobbyName;
            set
            {
                m_data.LobbyName = value;
                OnChanged(this);
            }
        }

        public LobbyState State
        {
            get => m_data.State;
            set
            {
                m_data.State = value;
                OnChanged(this);
            }
        }
        
        public bool Private
        {
            get => m_data.Private;
            set
            {
                m_data.Private = value;
                OnChanged(this);
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_data.MaxPlayerCount;
            set
            {
                m_data.MaxPlayerCount = value;
                OnChanged(this);
            }
        }

        public long? AllPlayersReadyTime => m_data.AllPlayersReadyTime;

        /// <summary>
        /// Checks if we have n players that have the Status.
        /// -1 Count means you need all Lobbyusers
        /// </summary>
        /// <returns>True if enough players are of the input status.</returns>
        public bool PlayersOfState(UserStatus status, int playersCount = -1)
        {
            var statePlayers = m_LobbyUsers.Values.Count(user => user.UserStatus == status);

            if (playersCount < 0)
                return statePlayers == m_LobbyUsers.Count;
            return statePlayers == playersCount;
        }

        public void CopyObserved(LobbyInfo info, Dictionary<string, LobbyUser> oldUsers)
        {
            m_data = info;
            if (oldUsers == null)
                m_LobbyUsers = new Dictionary<string, LobbyUser>();
            else
            {
                List<LobbyUser> toRemove = new List<LobbyUser>();
                foreach (var user in m_LobbyUsers)
                {
                    if (oldUsers.ContainsKey(user.Key))
                        user.Value.CopyObserved(oldUsers[user.Key]);
                    else
                        toRemove.Add(user.Value);
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var oldUser in oldUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(oldUser.Key))
                        DoAddPlayer(oldUser.Value);
                }
            }

            OnChanged(this);
        }

        public override void CopyObserved(LocalLobby oldObserved)
        {
            CopyObserved(oldObserved.Data, oldObserved.m_LobbyUsers);
        }
    }
}
