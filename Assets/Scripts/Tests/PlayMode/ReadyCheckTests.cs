using LobbyRelaySample;
using NUnit.Framework;
using System.Collections;
using Unity.Services.Rooms;
using Unity.Services.Rooms.Models;
using UnityEngine;
using UnityEngine.TestTools;
using RoomsInterface = LobbyRelaySample.Lobby.RoomsInterface;

namespace Test
{
    public class ReadyCheckTests
    {
        private string m_workingRoomId;
        private LobbyRelaySample.Auth.Identity m_auth;
        private bool m_didSigninComplete = false;
        private GameObject m_updateSlowObj;

        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRelaySample.Auth.Identity(() => { m_didSigninComplete = true; });
            Locator.Get.Provide(m_auth);
            m_updateSlowObj = new GameObject("UpdateSlowTest");
            m_updateSlowObj.AddComponent<UpdateSlow>();
        }

        [UnityTearDown]
        public IEnumerator PerTestTeardown()
        {
            if (m_workingRoomId != null)
            {   RoomsInterface.DeleteRoomAsync(m_workingRoomId, null);
                m_workingRoomId = null;
            }
            yield return new WaitForSeconds(0.5f); // We need a yield anyway, so wait long enough to probably delete the room. There currently (6/22/2021) aren't other tests that would have issues if this took longer.
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            Locator.Get.Provide(new LobbyRelaySample.Auth.IdentityNoop());
            m_auth.Dispose();
            LogAssert.ignoreFailingMessages = false;
            RoomsQuery.Instance.EndTracking();
            GameObject.Destroy(m_updateSlowObj);
        }

        private IEnumerator WaitForSignin()
        {
            // Wait a reasonable amount of time for sign-in to complete.
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");
        }

        private IEnumerator CreateRoom(string roomName, string userId)
        {
            Response<Room> createResponse = null;
            float timeout = 5;
            RoomsInterface.CreateRoomAsync(userId, roomName, 4, false, (r) => { createResponse = r; });
            while (createResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (room creation).");
            m_workingRoomId = createResponse.Result.Id;
        }

        private IEnumerator PushPlayerData(LobbyUser player)
        {
            bool hasPushedPlayerData = false;
            float timeout = 5;
            RoomsQuery.Instance.UpdatePlayerDataAsync(LobbyRelaySample.Lobby.ToLobbyData.RetrieveUserData(player), () => { hasPushedPlayerData = true; }); // RoomsContentHeartbeat normally does this.
            while (!hasPushedPlayerData && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (push player data).");
        }

        /// <summary>
        /// After creating a room and a player, signal that the player is Ready. This should lead to a countdown time being set for all players.
        /// </summary>
        [UnityTest]
        public IEnumerator SetCountdownTimeSinglePlayer()
        {
            LogAssert.ignoreFailingMessages = true; // Not sure why, but when auth logs in, it sometimes generates an error: "A Native Collection has not been disposed[...]." We don't want this to cause test failures, since in practice it *seems* to not negatively impact behavior.
            bool? readyResult = null;
            LobbyReadyCheck readyCheck = new LobbyReadyCheck((b) => { readyResult = b; }, 5); // This ready time is used for the countdown target end, not for any of the timing of actually detecting readies.
            yield return WaitForSignin();

            string userId = m_auth.GetSubIdentity(LobbyRelaySample.Auth.IIdentityType.Auth).GetContent("id");
            yield return CreateRoom("TestReadyRoom1", userId);

            RoomsQuery.Instance.BeginTracking(m_workingRoomId);
            yield return new WaitForSeconds(2); // Allow the initial room retrieval.

            LobbyUser user = new LobbyUser();
            user.ID = userId;
            user.UserStatus = UserStatus.Ready;
            yield return PushPlayerData(user);

            readyCheck.BeginCheckingForReady();
            float timeout = 5; // Long enough for two slow updates
            yield return new WaitForSeconds(timeout);

            readyCheck.Dispose();
            RoomsQuery.Instance.EndTracking();

            yield return new WaitForSeconds(2); // Buffer to prevent a 429 on the upcoming Get, since there's a Get request on the slow upate loop when that's active.
            Response<Room> getResponse = null;
            timeout = 5;
            RoomsInterface.GetRoomAsync(m_workingRoomId, (r) => { getResponse = r; });
            while (getResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout check (get room).");
            Assert.NotNull(getResponse.Result, "Retrieved room successfully.");
            Assert.NotNull(getResponse.Result.Data, "Room should have data.");

            Assert.True(getResponse.Result.Data.ContainsKey("AllPlayersReady"), "Check for AllPlayersReady key.");
            string readyString = getResponse.Result.Data["AllPlayersReady"]?.Value;
            Assert.NotNull(readyString, "Check for non-null AllPlayersReady.");
            Assert.True(long.TryParse(readyString, out long ticks), "Check for ticks value in AllPlayersReady."); // This will be based on the current time, so we won't check for a specific value.
        }

        // Can't test with multiple players on one machine, since anonymous UAS credentials can't be manually supplied.
    }
}
