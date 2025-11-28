using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    public GameObject cardPrefab;
    public Transform boardParent;

    public int columns = 4;
    public int rows = 3;
    public float spacingX = 1.5f;
    public float spacingY = 2f;

    public Vector3 boardStartPosition = new Vector3(-3f, 3f, 0f);

    public float mismatchRevealTime = 1f;

    public NetworkVariable<int> scoreP1 = new NetworkVariable<int>();
    public NetworkVariable<int> scoreP2 = new NetworkVariable<int>();

    private List<NetworkCard> allCards = new List<NetworkCard>();
    private Dictionary<ulong, NetworkCard> first = new();
    private Dictionary<ulong, NetworkCard> second = new();

    public static NetworkGameManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(SetupRoutine());

        scoreP1.OnValueChanged += (_, __) => UIManager.Instance.UpdateScores(scoreP1.Value, scoreP2.Value);
        scoreP2.OnValueChanged += (_, __) => UIManager.Instance.UpdateScores(scoreP1.Value, scoreP2.Value);
    }

    IEnumerator SetupRoutine()
    {
        yield return null;
        SetupBoard();
    }

    void SetupBoard()
    {
        int total = columns * rows;
        if (total % 2 != 0) return;

        List<int> ids = new();
        int pairs = total / 2;

        for (int i = 0; i < pairs; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }

        ids = ids.OrderBy(x => Random.value).ToList();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector3 pos = boardStartPosition + new Vector3(c * spacingX, -r * spacingY, 0);
                GameObject go = Instantiate(cardPrefab, pos, Quaternion.identity, boardParent);

                NetworkObject no = go.GetComponent<NetworkObject>();
                no.Spawn();

                var card = go.GetComponent<NetworkCard>();
                card.SetCardIdServerRpc(ids[r * columns + c]);

                allCards.Add(card);
            }
        }
    }

    public void CardFlipped(NetworkCard card, ulong playerId)
    {
        if (!IsServer) return;
        if (card.isFaceUp.Value || card.isMatched.Value) return;

        if (second.ContainsKey(playerId) && second[playerId] != null) return;

        card.RevealServerRpc();

        if (!first.ContainsKey(playerId) || first[playerId] == null)
        {
            first[playerId] = card;
        }
        else
        {
            second[playerId] = card;
            StartCoroutine(Check(playerId));
        }
    }

    IEnumerator Check(ulong playerId)
    {
        yield return new WaitForSeconds(0.2f);

        var a = first[playerId];
        var b = second[playerId];

        if (a.cardId.Value == b.cardId.Value)
        {
            AddPoint(playerId);
            a.SetMatchedServerRpc();
            b.SetMatchedServerRpc();
        }
        else
        {
            yield return new WaitForSeconds(mismatchRevealTime);
            a.HideServerRpc();
            b.HideServerRpc();
        }

        first[playerId] = null;
        second[playerId] = null;

        if (allCards.All(x => x.isMatched.Value))
            GameOverClientRpc(scoreP1.Value, scoreP2.Value);
    }

    void AddPoint(ulong id)
    {
        if (id == NetworkManager.Singleton.LocalClientId && IsServer)
            scoreP1.Value++;
        else
            scoreP2.Value++;
    }

    [ClientRpc]
    void GameOverClientRpc(int s1, int s2)
    {
        UIManager.Instance.ShowWinner(s1, s2);
    }
}
