using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Level3Bamboo.River
{
    // The path to the river at the end of the bamboo trail (like Lab 7's EnterLevel2 trigger).
    // With all 6 logs, walking into this trigger carries Ryu's progress over and loads the river scene.
    // Without them, a message says more logs are needed.
    public class BambooRiverExit : MonoBehaviour
    {
        public LogCollection collection;
        public BambooPlayerController player;
        public AudioSource music;
        public TMP_Text message;

        private bool loading;

        void OnTriggerStay2D(Collider2D other)
        {
            if (other.tag != "Player" || loading || player.IsDead || Time.timeScale <= 0f)
                return;

            bool hasAllLogs = collection.CollectedCount >= RiverProgress.RequiredLogs;
            message.gameObject.SetActive(!hasAllLogs);
            if (!hasAllLogs)
                return;

            loading = true;
            RiverProgress.Carry(collection.CollectedIds, player, music != null && music.mute);
            SceneManager.LoadSceneAsync("L3_B_RiverCrossing", LoadSceneMode.Single);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.tag == "Player")
                message.gameObject.SetActive(false);
        }
    }
}
