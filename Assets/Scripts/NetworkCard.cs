using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class NetworkCard : NetworkBehaviour
{
    public NetworkVariable<int> cardId = new(-1);
    public NetworkVariable<bool> isFaceUp = new(false);
    public NetworkVariable<bool> isMatched = new(false);

    public Sprite backSprite;
    public Sprite[] cardFaces;

    SpriteRenderer sr;
    Collider2D col;
    Vector3 originalScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        UpdateVisual();

        isFaceUp.OnValueChanged += (_, __) => UpdateVisual();
        cardId.OnValueChanged += (_, __) => UpdateVisual();
        isMatched.OnValueChanged += (_, v) => { if (v) col.enabled = false; };
    }

    void UpdateVisual()
    {
        if (isFaceUp.Value && cardId.Value >= 0)
            sr.sprite = cardFaces[cardId.Value];
        else
            sr.sprite = backSprite;
    }

    IEnumerator Flip()
    {
        float t = 0.12f;
        float a = 0f;

        while (a < t)
        {
            a += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0f, a / t);
            transform.localScale = new Vector3(s, originalScale.y, originalScale.z);
            yield return null;
        }

        UpdateVisual();

        a = 0f;
        while (a < t)
        {
            a += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1f, a / t);
            transform.localScale = new Vector3(s, originalScale.y, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    [ClientRpc]
    void PlayFlipClientRpc()
    {
        StartCoroutine(Flip());
    }

    [ServerRpc(RequireOwnership = false)]
    public void RevealServerRpc()
    {
        isFaceUp.Value = true;
        PlayFlipClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideServerRpc()
    {
        isFaceUp.Value = false;
        PlayFlipClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCardIdServerRpc(int id)
    {
        cardId.Value = id;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMatchedServerRpc()
    {
        isMatched.Value = true;
        isFaceUp.Value = true;
        PlayFlipClientRpc();
    }
}
