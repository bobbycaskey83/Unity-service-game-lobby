﻿using System.Collections.Generic;
using Unity.Services.Rooms.Models;

namespace LobbyRelaySample.Lobby
{
    /// <summary>
    /// Convert the Room resulting from a Rooms request into a LobbyData for use in the game logic.
    /// </summary>
    public static class ToLobbyData
    {
        /// <summary>
        /// Create a new LobbyData from the content of a retrieved Room. Its data can be copied into an existing LobbyData for use.
        /// </summary>
        public static void Convert(Room room, LobbyData outputToHere, LobbyUser existingLocalUser = null)
        {
            LobbyInfo info = new LobbyInfo
            {   RoomID              = room.Id,
                RoomCode            = room.RoomCode,
                Private             = room.IsPrivate,
                LobbyName           = room.Name,
                MaxPlayerCount      = room.MaxPlayers,
                RelayCode           = room.Data?.ContainsKey("RelayCode") == true ? room.Data["RelayCode"].Value : null,
                State               = room.Data?.ContainsKey("State") == true ? (LobbyState) int.Parse(room.Data["State"].Value) : LobbyState.Lobby,
                AllPlayersReadyTime = room.Data?.ContainsKey("AllPlayersReady") == true ? long.Parse(room.Data["AllPlayersReady"].Value) : (long?)null
            };
            Dictionary<string, LobbyUser> roomUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in room.Players)
            {
                if (existingLocalUser != null && player.Id.Equals(existingLocalUser.ID))
                {
                    existingLocalUser.IsHost = room.HostId.Equals(player.Id);
                    existingLocalUser.DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : existingLocalUser.DisplayName;
                    existingLocalUser.Emote = player.Data?.ContainsKey("Emote") == true ? player.Data["Emote"].Value : existingLocalUser.Emote;
                    roomUsers.Add(existingLocalUser.ID, existingLocalUser);
                }
                else
                {
                    LobbyUser user = new LobbyUser(
                        displayName: player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : "NewPlayer",
                        isHost: room.HostId.Equals(player.Id),
                        id: player.Id,
                        emote: player.Data?.ContainsKey("Emote") == true ? player.Data["Emote"].Value : null,
                        userStatus: player.Data?.ContainsKey("UserStatus") == true ? player.Data["UserStatus"].Value : UserStatus.Lobby.ToString()
                    );
                    roomUsers.Add(user.ID, user);
                }
            }

            outputToHere.CopyObserved(info, roomUsers);
        }

        /// <summary>
        /// Create a list of new LobbyData from the content of a retrieved Room.
        /// </summary>
        public static List<LobbyData> Convert(QueryResponse response)
        {
            List<LobbyData> retLst = new List<LobbyData>();
            foreach (var room in response.Results)
                retLst.Add(Convert(room));
            return retLst;
        }
        private static LobbyData Convert(Room room)
        {
            LobbyData data = new LobbyData();
            Convert(room, data, null);
            return data;
        }

        public static Dictionary<string, string> RetrieveRoomData(LobbyData room)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("RelayCode", room.RelayCode);
            data.Add("State", ((int)room.State).ToString());
            // We only want the ArePlayersReadyTime to be set when we actually are ready for it, and it's null otherwise. So, don't set that here.
            return data;
        }

        public static Dictionary<string, string> RetrieveUserData(LobbyUser user)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (user == null || string.IsNullOrEmpty(user.ID))
                return data;
            data.Add("DisplayName", user.DisplayName);
            data.Add("Emote", user.Emote); // Emote could be null, which is fine.
            data.Add("UserStatus", user.UserStatus.ToString());
            return data;
        }
    }
}
