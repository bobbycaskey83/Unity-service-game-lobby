using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.Relay
{
    /// <summary>
    /// Does all the interaction with relay.
    /// </summary>
    public static class RelayInterface
    {
        private class InProgressRequest<T>
        {
            public InProgressRequest(Task<T> task, Action<T> onComplete)
            {
                DoRequest(task, onComplete);
            }

            private async void DoRequest(Task<T> task, Action<T> onComplete)
            {
                T result = default;
                string currentTrace = System.Environment.StackTrace;
                try {
                    result = await task;
                } catch (Exception e) {
                    Exception eFull = new Exception($"Call stack before async call:\n{currentTrace}\n", e);
                    throw eFull;
                } finally {
                    onComplete?.Invoke(result);
                }
            }
        }

        /// <summary>
        /// Creates a Relay Server, and returns the Allocation (Response.Result.Data.Allocation)
        /// </summary>
        public static void AllocateAsync(int maxConnections, Action<Allocation> onComplete)
        {
            CreateAllocationRequest createAllocationRequest = new CreateAllocationRequest(new AllocationRequest(maxConnections));
            var task = RelayService.AllocationsApiClient.CreateAllocationAsync(createAllocationRequest);
            new InProgressRequest<Response<AllocateResponseBody>>(task, OnResponse);

            void OnResponse(Response<AllocateResponseBody> response)
            {
                if (response == null)
                    Debug.LogError("Relay returned a null Allocation. It's possible the Relay service is currently down.");
                else if (response.Status >= 200 && response.Status < 300)
                    onComplete?.Invoke(response.Result.Data.Allocation);
                else
                    Debug.LogError($"Allocation returned a non Success code: {response.Status}");
            };
        }

        /// <summary>
        /// Get a JoinCode( Response.Result.Data.JoinCode) from an Allocated Server
        /// </summary>
        public static void GetJoinCodeAsync(Guid hostAllocationId, Action<Response<JoinCodeResponseBody>> onComplete)
        {
            CreateJoincodeRequest joinCodeRequest = new CreateJoincodeRequest(new JoinCodeRequest(hostAllocationId));
            var task = RelayService.AllocationsApiClient.CreateJoincodeAsync(joinCodeRequest);

            new InProgressRequest<Response<JoinCodeResponseBody>>(task, onComplete);
        }

        public static void GetJoinCodeAsync(Guid hostAllocationId, Action<string> onComplete)
        {
            GetJoinCodeAsync(hostAllocationId, a =>
            {
                if (a.Status >= 200 && a.Status < 300)
                    onComplete.Invoke(a.Result.Data.JoinCode);
                else
                {
                    Debug.LogError($"Join Code Get returned a non Success code: {a.Status}");
                }
            });
        }

        /// <summary>
        /// Retrieve an Allocation(Response.Result.Data.Allocation) by join code
        /// </summary>
        public static void JoinAsync(string joinCode, Action<Response<JoinResponseBody>> onComplete)
        {
            JoinRelayRequest joinRequest = new JoinRelayRequest(new JoinRequest(joinCode));
            var task = RelayService.AllocationsApiClient.JoinRelayAsync(joinRequest);

            new InProgressRequest<Response<JoinResponseBody>>(task, onComplete);
        }

        public static void JoinAsync(string joinCode, Action<JoinAllocation> onComplete)
        {
            JoinAsync(joinCode, a =>
            {
                if (a.Status >= 200 && a.Status < 300)
                    onComplete.Invoke(a.Result.Data.Allocation);
                else
                {
                    Debug.LogError($"Join Call returned a non Success code: {a.Status}");
                }
            });
        }
    }
}
