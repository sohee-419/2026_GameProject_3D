using UnityEngine;

public class Exhibit : MonoBehaviour
{
    public GameObject panel;     // 이 작품의 상세 보기 패널
    public GameObject prompt;    // 가까이 가면 켜지는 E 표시

    public void ShowPrompt(bool on)
    { if (prompt != null) prompt.SetActive(on); }
}
