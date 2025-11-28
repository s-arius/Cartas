using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NetworkPlayer : NetworkBehaviour
{
    public float speed = 5f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector2 dir = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        transform.position += (Vector3)(dir.normalized * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!IsOwner) return;

        var card = c.GetComponent<NetworkCard>();
        if (card != null)
            FlipCardServerRpc(card.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    void FlipCardServerRpc(ulong cardId, ServerRpcParams p = default)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
        if (obj == null) return;

        var card = obj.GetComponent<NetworkCard>();
        if (card == null) return;

        NetworkGameManager.Instance.CardFlipped(card, p.Receive.SenderClientId);
    }
}
