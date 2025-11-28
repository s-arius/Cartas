using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TMP_Text p1Text;
    public TMP_Text p2Text;

    public GameObject winnerPanel;
    public TMP_Text winnerText;

    public Button hostBtn;
    public Button clientBtn;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        winnerPanel.SetActive(false);

        hostBtn.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        clientBtn.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }

    public void UpdateScores(int s1, int s2)
    {
        p1Text.text = "P1: " + s1;
        p2Text.text = "P2: " + s2;
    }

    public void ShowWinner(int s1, int s2)
    {
        winnerPanel.SetActive(true);

        if (s1 > s2) winnerText.text = "¡Gana Player 1!";
        else if (s2 > s1) winnerText.text = "¡Gana Player 2!";
        else winnerText.text = "¡Empate!";
    }
}
