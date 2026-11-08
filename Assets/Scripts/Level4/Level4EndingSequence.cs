using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Salem.Level4
{
    public class Level4EndingSequence : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform escapeWindowTransform;
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject cinematicBroomWitch;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CanvasGroup creditsCanvasGroup;
        [SerializeField] private RectTransform creditsScrollContent;

        [Header("Audio")]
        [SerializeField] private AudioClip glassShatterSfx;
        [SerializeField] private AudioClip creditsMusic;

        [Header("Ending Tuning")]
        [SerializeField] private float flightSpeed = 8f;
        [SerializeField] private float creditsScrollSpeed = 35f;

        private bool _isTriggered = false;

        private void Awake()
        {
            if (creditsCanvasGroup != null)
            {
                creditsCanvasGroup.alpha = 0f;
                creditsCanvasGroup.gameObject.SetActive(false);
            }

            if (cinematicBroomWitch != null)
            {
                cinematicBroomWitch.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;

            if (other.CompareTag("Player"))
            {
                StartCoroutine(RunEndingCinematic(other.gameObject));
            }
        }

        private IEnumerator RunEndingCinematic(GameObject player)
        {
            _isTriggered = true;

            // 1. Shatter glass sound
            if (glassShatterSfx != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(glassShatterSfx);
            }

            // Hide gameplay player
            if (player != null) player.SetActive(false);

            // Activate flying witch mounted on broom
            if (cinematicBroomWitch != null && escapeWindowTransform != null)
            {
                cinematicBroomWitch.transform.position = escapeWindowTransform.position;
                cinematicBroomWitch.SetActive(true);
            }

            // 2. Fly up and away towards the moon
            float flightTimer = 0f;
            Vector3 flightDir = new Vector3(1.4f, 0.9f, 0f).normalized;

            while (flightTimer < 4.0f)
            {
                flightTimer += Time.deltaTime;
                if (cinematicBroomWitch != null)
                {
                    cinematicBroomWitch.transform.position += flightDir * flightSpeed * Time.deltaTime;
                }
                if (mainCamera != null && cinematicBroomWitch != null)
                {
                    Vector3 camTarget = cinematicBroomWitch.transform.position;
                    camTarget.z = -10f;
                    mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, camTarget, Time.deltaTime * 2f);
                }
                yield return null;
            }

            // 3. Start Credits Music
            if (creditsMusic != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(creditsMusic);
            }

            // 4. Fade in Credits
            if (creditsCanvasGroup != null)
            {
                creditsCanvasGroup.gameObject.SetActive(true);
                float fade = 0f;
                while (fade < 1.0f)
                {
                    fade += Time.deltaTime * 0.8f;
                    creditsCanvasGroup.alpha = fade;
                    yield return null;
                }
            }

            // 5. Scroll credits upward
            if (creditsScrollContent != null)
            {
                float scrollTime = 0f;
                while (scrollTime < 25f)
                {
                    scrollTime += Time.deltaTime;
                    creditsScrollContent.anchoredPosition += new Vector2(0f, creditsScrollSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level1");
        }
    }
}
