﻿using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

namespace LobbyRelaySample.lobby
{
    /// <summary>
    /// Convert the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class ToLocalLobby
    {
        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void Convert(Lobby lobby, LocalLobby outputToHere)
        {
            LocalLobby.LobbyData info = new LocalLobby.LobbyData // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            {   LobbyID             = lobby.Id,
                LobbyCode           = lobby.LobbyCode,
                Private             = lobby.IsPrivate,
                LobbyName           = lobby.Name,
                MaxPlayerCount      = lobby.MaxPlayers,
                RelayCode           = lobby.Data?.ContainsKey("RelayCode") == true ? lobby.Data["RelayCode"].Value : null, // TODO: Remove?
                State               = lobby.Data?.ContainsKey("State") == true ? (LobbyState) int.Parse(lobby.Data["State"].Value) : LobbyState.Lobby, // TODO: Consider TryParse, just in case (and below). Although, we don't have fail logic anyway...
                AllPlayersReadyTime = lobby.Data?.ContainsKey("AllPlayersReady") == true ? long.Parse(lobby.Data["AllPlayersReady"].Value) : (long?)null // TODO: Remove
            };

            Dictionary<string, LobbyUser> lobbyUsers = new Dictionary<string, LobbyUser>(); // TODO: So, right, why this?
            foreach (var player in lobby.Players)
            {
                // If we already know about this player and this player is already connected to Relay, don't overwrite things that Relay might be changing.
                if (player.Data?.ContainsKey("UserStatus") == true && int.TryParse(player.Data["UserStatus"].Value, out int status))
                {
                    if (status > (int)UserStatus.Connecting && outputToHere.LobbyUsers.ContainsKey(player.Id))
                    {
                        lobbyUsers.Add(player.Id, outputToHere.LobbyUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, or if we just don't know about them yet, get the most recent data that the lobby knows. // TODO: Relay should handle the latter case.
                // (If we have no local representation of the player, that gets added by the LocalLobby.)
                LobbyUser incomingData = new LobbyUser(); // TODO: This is unclear; this should be just a data object replacing that of the user, not a new user whose data are taken.
                incomingData.IsHost = lobby.HostId.Equals(player.Id);
                incomingData.DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : default;
                incomingData.Emote       = player.Data?.ContainsKey("Emote") == true ? (EmoteType)int.Parse(player.Data["Emote"].Value) : default;
                incomingData.UserStatus  = player.Data?.ContainsKey("UserStatus") == true ? (UserStatus)int.Parse(player.Data["UserStatus"].Value) : default;
                incomingData.ID = player.Id;
                lobbyUsers.Add(incomingData.ID, incomingData);
            }
            outputToHere.CopyObserved(info, lobbyUsers);
        }

        /// <summary>
        /// Create a list of new LocalLobby from the content of a retrieved lobby.
        /// </summary>
        public static List<LocalLobby> Convert(QueryResponse response)
        {
            List<LocalLobby> retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
                retLst.Add(Convert(lobby));
            return retLst;
        }
        private static LocalLobby Convert(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            Convert(lobby, data);
            return data;
        }

        public static Dictionary<string, string> RetrieveLobbyData(LocalLobby lobby)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("RelayCode", lobby.RelayCode);
            data.Add("State", ((int)lobby.State).ToString());
            // We only want the ArePlayersReadyTime to be set when we actually are ready for it, and it's null otherwise. So, don't set that here.
            return data;
        }

        public static Dictionary<string, string> RetrieveUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add("DisplayName", user.DisplayName);
            data.Add("Emote", ((int)user.Emote).ToString());
            data.Add("UserStatus", ((int)user.UserStatus).ToString());
            return data;
        }
    }
}
