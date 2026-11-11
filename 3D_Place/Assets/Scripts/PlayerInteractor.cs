using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public FirstPersonMover mover;     // Player의 FirstPersonMover

    private Exhibit nearby;            // 지금 가까이 있는 작품
    private GameObject openPanel;      // 지금 열려 있는 패널

    private void OnTriggerEnter(Collider other)
    {
        Exhibit e = other.GetComponent<Exhibit>();
        if (e == null) return;
        nearby = e;
        nearby.ShowPrompt(true);
    }

    private void OnTriggerExit(Collider other)
    {
        Exhibit e = other.GetComponent<Exhibit>();
        if (e == null || e != nearby) return;
        nearby.ShowPrompt(false);
        nearby = null;
    }
}
