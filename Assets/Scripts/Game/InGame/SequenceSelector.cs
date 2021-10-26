﻿using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyRelaySample.inGame
{
    /// <summary>
    /// Handles selecting the randomized sequence of symbols to spawn. This also selects a subset of the selected symbols to be the target
    /// sequence that each player needs to select in order.
    /// </summary>
    public class SequenceSelector : NetworkBehaviour
    {
        [SerializeField] private SymbolData m_symbolData = default;
        [SerializeField] private RawImage[] m_targetSequenceOutput = default;
        public const int k_symbolCount = 100;
        private List<int> m_fullSequence = new List<int>(); // This is owned by the host, and each index is assigned as a NetworkVariable to each SymbolObject.
        private NetworkList<int> m_targetSequence; // This is owned by the host but needs to be available to all clients, so it's a NetworkedList here.
        private int m_targetSequenceCurrentIndex = -1;

        public void Awake()
        {
            m_targetSequence = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
            {
                // Choose some subset of the list of symbols to be present in this game, along with a target sequence.
                List<int> symbolsForThisGame = SelectSymbols(m_symbolData.m_availableSymbols.Count, 8);
                m_targetSequence.Add(symbolsForThisGame[0]);
                m_targetSequence.Add(symbolsForThisGame[1]);
                m_targetSequence.Add(symbolsForThisGame[2]);

                // Then, ensure that the target sequence is present in order throughout most of the full set of symbols to spawn.
                int numTargetSequences = k_symbolCount / 6; // About 1/2 of the 3 symbols will be definitely part of the target sequence.
                for (; numTargetSequences >= 0; numTargetSequences--)
                {   m_fullSequence.Add(m_targetSequence[2]); // We want a List instead of a Queue or Stack for faster insertion, but we will remove indices backwards so as to not reshift other entries.
                    m_fullSequence.Add(m_targetSequence[1]);
                    m_fullSequence.Add(m_targetSequence[0]);
                }
                // Then, fill in with a good mix of the remaining symbols.
                AddHalfRemaining(3, 2);
                AddHalfRemaining(4, 2);
                AddHalfRemaining(5, 2);
                AddHalfRemaining(6, 2);
                AddHalfRemaining(7, 1);

                void AddHalfRemaining(int symbolIndex, int divider)
                {
                    int remaining = k_symbolCount - m_fullSequence.Count;
                    for (int n = 0; n < remaining / divider; n++)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, m_fullSequence.Count);
                        m_fullSequence.Insert(randomIndex, symbolsForThisGame[symbolIndex]);
                    }
                }
            }
        }

        // Very simple random selection. Duplicates are allowed.
        private static List<int> SelectSymbols(int numOptions, int targetCount)
        {
            List<int> list = new List<int>();
            for (int n = 0; n < targetCount; n++)
                list.Add(UnityEngine.Random.Range(0, numOptions));
            return list;
        }

        public void Update()
        {
            // We can't guarantee timing with the host's selection of the target sequence, so retrieve it once it's available.
            if (m_targetSequenceCurrentIndex < 0 && m_targetSequence.Count > 0)
            {
                for (int n = 0; n < m_targetSequence.Count; n++)
                    m_targetSequenceOutput[n].texture = m_symbolData.GetSymbolForIndex(m_targetSequence[n]).texture;
                m_targetSequenceCurrentIndex = 0;
                ScaleTargetUi();
            }
        }

        /// <summary>
        /// If the index is correct, this will advance the current sequence index.
        /// </summary>
        public bool ConfirmSymbolCorrect(int symbolIndex)
        {
            if (symbolIndex != m_targetSequence[m_targetSequenceCurrentIndex])
                return false;
            if (++m_targetSequenceCurrentIndex >= m_targetSequence.Count)
                m_targetSequenceCurrentIndex = 0;
            
            ScaleTargetUi();
            return true;
        }
        private void ScaleTargetUi()
        {
            for (int i = 0; i < m_targetSequenceOutput.Length; i++)
                m_targetSequenceOutput[i].transform.localScale = Vector3.one * (m_targetSequenceCurrentIndex == i ? 1 : 0.7f);
        }

        public int GetNextSymbol(int symbolObjectIndex)
        {
            return m_fullSequence[symbolObjectIndex];
        }
    }
}
