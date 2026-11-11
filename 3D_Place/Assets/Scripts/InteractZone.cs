using UnityEngine;
using UnityEngine.InputSystem;

public class InteractZone : MonoBehaviour
{
    public GameObject prompt;   // PRESS E 글자
    public GameObject panel;    // E 를 누르면 열릴 패널
    private bool playerNear;

    private void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;   // 플레이 중 큐브 숨김
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = true;
        prompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        prompt.SetActive(false);
        panel.SetActive(false);
    }

    private void Update()
    {
        if (playerNear && Keyboard.current.eKey.wasPressedThisFrame)
            panel.SetActive(!panel.activeSelf);
    }
}

