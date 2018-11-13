﻿using static CommonClass.Game.CardUseStruct;

namespace CommonClass.Game
{
    public class RoomState
    {
        private bool m_isClient;
        private Player m_currentPlayer;
        private string m_currentCardUsePattern, m_currentRespondSKill;
        private CardUseReason m_currentCardUseReason;
        private string m_currentCardResponsePrompt;
        private Player m_currentAskforPeachPlayer;
        private int m_global_response_id;
        private int m_current_response_id;
        public RoomState(bool isClient)
        {
            m_isClient = isClient;
            m_currentAskforPeachPlayer = null;
            m_global_response_id = 0;
        }
        //~RoomState();
        public bool IsClient() => m_isClient;
        public void SetCurrentPlayer(Player player) => m_currentPlayer = player;
        public string GetCurrentCardUsePattern(Player player = null)
        {
            if (player != null && m_currentCardResponsePrompt == "askForSinglePeach")
            {
                if (player == m_currentAskforPeachPlayer)
                    return "peach+analeptic";
                else
                    return "peach";
            }
            return m_currentCardUsePattern;
        }
        public void SetCurrentCardUsePattern(string newPattern) => m_currentCardUsePattern = newPattern;
        public Player GetCurrentPlayer() => m_currentPlayer;
        public CardUseReason GetCurrentCardUseReason() => m_currentCardUseReason;
        public void SetCurrentCardUseReason(CardUseReason reason) => m_currentCardUseReason = reason;
        public string GetCurrentCardResponsePrompt() => m_currentCardResponsePrompt;
        public void SetCurrentCardResponsePrompt(string prompt) => m_currentCardResponsePrompt = prompt;
        public void SetCurrentAskforPeachPlayer(Player player) => m_currentAskforPeachPlayer = player;
        public Player GetCurrentAskforPeachPlayer() => m_currentAskforPeachPlayer;
        public void SetGlobalResponseID() => m_global_response_id++;
        public int GlobalResponseID => m_global_response_id;
        public void SetCurrentResponseID(int id) => m_current_response_id = id;
        public int GetCurrentResponseID() => m_current_response_id;
        public void SetCurrentResponseSkill(string skill_name) => m_currentRespondSKill = skill_name;
        public string GetCurrentResponseSkill() => m_currentRespondSKill;

        //Card *getCard(int cardId) const;

        // Update a card in the room.
        // @param cardId
        //        Id of card to be updated.
        // @param newCard
        //        Card to be updated in the room.
        // @return
        //void resetCard(int cardId);
        // Reset all cards, generals' states of the room instance
        //void reset();

    }
}
