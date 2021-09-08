﻿using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using MsgType = LobbyRelaySample.relay.RelayUtpSetup.MsgType;

namespace LobbyRelaySample.relay
{
    /// <summary>
    /// This observes the local player and updates remote players over Relay when there are local changes, demonstrating basic data transfer over the Unity Transport (UTP).
    /// Created after the connection to Relay has been confirmed.
    /// </summary>
    public class RelayUtpClient : MonoBehaviour // This is a MonoBehaviour merely to have access to Update.
    {
        protected LobbyUser m_localUser;
        protected LocalLobby m_localLobby;
        protected NetworkDriver m_networkDriver;
        protected List<NetworkConnection> m_connections; // For clients, this has just one member, but for hosts it will have more.
        protected bool m_IsRelayConnected { get { return m_localLobby.RelayServer != null; } }

        protected bool m_hasSentInitialMessage = false;
        private const float k_heartbeatPeriod = 5;

        public virtual void Initialize(NetworkDriver networkDriver, List<NetworkConnection> connections, LobbyUser localUser, LocalLobby localLobby)
        {
            m_localUser = localUser;
            m_localLobby = localLobby;
            m_localUser.onChanged += OnLocalChange;
            m_networkDriver = networkDriver;
            m_connections = connections;
            Locator.Get.UpdateSlow.Subscribe(UpdateSlow, k_heartbeatPeriod);
        }
        protected virtual void Uninitialize()
        {
            m_localUser.onChanged -= OnLocalChange;
            Leave();
            Locator.Get.UpdateSlow.Unsubscribe(UpdateSlow);
            m_connections.Clear();
            m_networkDriver.Dispose();
        }
        public void OnDestroy()
        {
            Uninitialize();
        }

        private void OnLocalChange(LobbyUser localUser)
        {
            if (m_connections.Count == 0) // This could be the case for the host alone in the lobby.
                return;
            foreach (NetworkConnection conn in m_connections)
                DoUserUpdate(m_networkDriver, conn, m_localUser);
        }

        private void Update()
        {
            OnUpdate();
        }

        private void UpdateSlow(float dt)
        {
            // Clients need to send any data over UTP periodically, or else the connection will timeout.
            if (!m_IsRelayConnected) // However, if disconnected from Relay for some reason, we want the connection to timeout.
                return;
            foreach (NetworkConnection connection in m_connections)
                WriteByte(m_networkDriver, connection, "0", MsgType.Ping, 0); // The ID doesn't matter here, so send a minimal number of bytes.
        }

        protected virtual void OnUpdate()
        {
            if (!m_hasSentInitialMessage)
                ReceiveNetworkEvents(m_networkDriver); // Just on the first execution, make sure to handle any events that accumulated while completing the connection.
            m_networkDriver.ScheduleUpdate().Complete(); // This pumps all messages, which pings the Relay allocation and keeps it alive. It should be called no more often than ReceiveNetworkEvents.
            ReceiveNetworkEvents(m_networkDriver); // This reads the message queue which was just updated.
            if (!m_hasSentInitialMessage)
                SendInitialMessage(m_networkDriver, m_connections[0]); // On a client, the 0th (and only) connection is to the host.
        }

        private void ReceiveNetworkEvents(NetworkDriver driver)
        {
            NetworkConnection conn;
            DataStreamReader strm;
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEvent(out conn, out strm)) != NetworkEvent.Type.Empty) // NetworkConnection also has PopEvent, but NetworkDriver.PopEvent automatically includes new connections.
            {
                ProcessNetworkEvent(conn, strm, cmd);
            }
        }

        // See the Write* methods for the expected event format.
        private void ProcessNetworkEvent(NetworkConnection conn, DataStreamReader strm, NetworkEvent.Type cmd)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                MsgType msgType = (MsgType)strm.ReadByte();
                string id = ReadLengthAndString(ref strm);
                if (id == m_localUser.ID || !m_localLobby.LobbyUsers.ContainsKey(id)) // We don't hold onto messages, since an incoming user will be fully initialized before they send events.
                    return;

                if (msgType == MsgType.PlayerName)
                {
                    string name = ReadLengthAndString(ref strm);
                    m_localLobby.LobbyUsers[id].DisplayName = name;
                }
                else if (msgType == MsgType.Emote)
                {
                    EmoteType emote = (EmoteType)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].Emote = emote;
                }
                else if (msgType == MsgType.ReadyState)
                {
                    UserStatus status = (UserStatus)strm.ReadByte();
                    m_localLobby.LobbyUsers[id].UserStatus = status;
                }
                else if (msgType == MsgType.StartCountdown)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.StartCountdown, null);
                else if (msgType == MsgType.CancelCountdown)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.CancelCountdown, null);
                else if (msgType == MsgType.ConfirmInGame)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.ConfirmInGameState, null);
                else if (msgType == MsgType.EndInGame)
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null);

                ProcessNetworkEventDataAdditional(conn, strm, msgType, id);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
                ProcessDisconnectEvent(conn, strm);
        }

        protected virtual void ProcessNetworkEventDataAdditional(NetworkConnection conn, DataStreamReader strm, MsgType msgType, string id) { }
        protected virtual void ProcessDisconnectEvent(NetworkConnection conn, DataStreamReader strm)
        {
            // The host disconnected, and Relay does not support host migration. So, all clients should disconnect.
            string msg;
            if (m_IsRelayConnected)
                msg = "Host disconnected! Leaving the lobby.";
            else
                msg = "Connection to host was lost. Leaving the lobby.";

            Debug.LogError(msg);
            Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, msg);
            Leave();
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
        }

        /// <summary>
        /// Relay uses raw pointers for efficiency. This converts them to byte arrays, assuming the stream contents are 1 byte for array length followed by contents.
        /// </summary>
        unsafe private string ReadLengthAndString(ref DataStreamReader strm)
        {
            byte length = strm.ReadByte();
            byte[] bytes = new byte[length];
            fixed (byte* ptr = bytes)
            {
                strm.ReadBytes(ptr, length);
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Once a client is connected, send a message out alerting the host.
        /// </summary>
        private void SendInitialMessage(NetworkDriver driver, NetworkConnection connection)
        {
            WriteByte(driver, connection, m_localUser.ID, MsgType.NewPlayer, 0);
            ForceFullUserUpdate(driver, connection, m_localUser); // Assuming this is only created after the Relay connection is successful.
            m_hasSentInitialMessage = true;
        }

        /// <summary>
        /// When player data is updated, send out events for just the data that actually changed.
        /// </summary>
        private void DoUserUpdate(NetworkDriver driver, NetworkConnection connection, LobbyUser user)
        {
            // Only update with actual changes. (If multiple change at once, we send messages for each separately, but that shouldn't happen often.)
            if (0 < (user.LastChanged & LobbyUser.UserMembers.DisplayName))
                WriteString(driver, connection, user.ID, MsgType.PlayerName, user.DisplayName);
            if (0 < (user.LastChanged & LobbyUser.UserMembers.Emote))
                WriteByte(driver, connection, user.ID, MsgType.Emote, (byte)user.Emote);
            if (0 < (user.LastChanged & LobbyUser.UserMembers.UserStatus))
                WriteByte(driver, connection, user.ID, MsgType.ReadyState, (byte)user.UserStatus);
        }
        /// <summary>
        /// Sometimes (e.g. when a new player joins), we need to send out the full current state of this player.
        /// </summary>
        protected void ForceFullUserUpdate(NetworkDriver driver, NetworkConnection connection, LobbyUser user)
        {
            // Note that it would be better to send a single message with the full state, but for the sake of shorter code we'll leave that out here.
            WriteString(driver, connection, user.ID, MsgType.PlayerName, user.DisplayName);
            WriteByte(driver, connection, user.ID, MsgType.Emote, (byte)user.Emote);
            WriteByte(driver, connection, user.ID, MsgType.ReadyState, (byte)user.UserStatus);
        }

        /// <summary>
        /// Write string data as: [1 byte: msgType] [1 byte: id length N] [N bytes: id] [1 byte: string length M] [M bytes: string]
        /// </summary>
        protected void WriteString(NetworkDriver driver, NetworkConnection connection, string id, MsgType msgType, string str)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(id);
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);

            List<byte> message = new List<byte>(idBytes.Length + strBytes.Length + 3);
            message.Add((byte)msgType);
            message.Add((byte)idBytes.Length);
            message.AddRange(idBytes);
            message.Add((byte)strBytes.Length);
            message.AddRange(strBytes);

            if (driver.BeginSend(connection, out var dataStream) == 0) // Oh, should check this first?
            {
                byte[] bytes = message.ToArray();
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        dataStream.WriteBytes(bytesPtr, message.Count);
                        driver.EndSend(dataStream);
                    }
                }
            }
        }

        /// <summary>
        /// Write byte data as: [1 byte: msgType] [1 byte: id length N] [N bytes: id] [1 byte: data]
        /// </summary>
        protected void WriteByte(NetworkDriver driver, NetworkConnection connection, string id, MsgType msgType, byte value)
        {
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(id);
            List<byte> message = new List<byte>(idBytes.Length + 3);
            message.Add((byte)msgType);
            message.Add((byte)idBytes.Length);
            message.AddRange(idBytes);
            message.Add(value);

            if (driver.BeginSend(connection, out var dataStream) == 0) // Oh, should check this first?
            {
                byte[] bytes = message.ToArray();
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        dataStream.WriteBytes(bytesPtr, message.Count);
                        driver.EndSend(dataStream);
                    }
                }
            }
        }

        public virtual void Leave()
        {
            foreach (NetworkConnection connection in m_connections)
                WriteByte(m_networkDriver, connection, m_localUser.ID, MsgType.PlayerDisconnect, 0); // If the client breaks the connection, the host might still maintain it, so message instead.
            m_localLobby.RelayServer = null;
        }
    }
}
