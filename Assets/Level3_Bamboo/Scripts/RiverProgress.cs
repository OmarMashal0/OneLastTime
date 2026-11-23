using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level3Bamboo.River
{
    // The handoff exists only between the two Level 3 scenes.
    public static class RiverProgress
    {
        public const int RequiredLogs = 6;
        private const string RiverScene = "L3_B_RiverCrossing";
        private static HashSet<string> carriedLogs;
        private static int health;
        private static int sanity;
        private static int maxHealth;
        private static int maxSanity;
        public static bool MusicMuted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            carriedLogs = null;
            MusicMuted = false;
            SceneManager.sceneLoaded -= DiscardUnclaimedCarry;
            SceneManager.sceneLoaded += DiscardUnclaimedCarry;
        }

        // A carry is only valid for the river load that follows it, so a new run
        // (or any other scene) can never pick up stale bamboo progress.
        private static void DiscardUnclaimedCarry(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && scene.name != RiverScene)
                carriedLogs = null;
        }

        public static void Carry(IEnumerable<string> ids, BambooPlayerController player, bool muted)
        {
            carriedLogs = new HashSet<string>(ids);
            MusicMuted = muted;
            health = player.Health;
            sanity = player.Sanity;
            maxHealth = player.maxHealth;
            maxSanity = player.maxSanity;
        }

        public static int Receive(BambooPlayerController player)
        {
            if (carriedLogs == null)
            {
                MusicMuted = false;
                return 0;
            }

            int count = carriedLogs.Count;
            player.RestoreCarriedVitals(health, sanity, maxHealth, maxSanity);
            carriedLogs = null;
            return count;
        }
    }
}

