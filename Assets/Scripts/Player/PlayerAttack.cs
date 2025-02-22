using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    public PlayerMovement playerMovement;
    public Transform playerSprite;
    public Attack attackColliderPrefab;
    public float attackDuration = 0.3f;

    private Attack activeAttackCollider;

    private void Update()
    {
        if (IsOwner && Input.GetMouseButtonDown(0))
        {
            Vector2 attackDirection = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - playerSprite.position).normalized;
            RequestAttackServerRpc(attackDirection);
        }
    }

    [ServerRpc]
    private void RequestAttackServerRpc(Vector2 attackDirection)
    {
        if (playerMovement.IsStunned) return;
        if (activeAttackCollider == null)
        {
            float angle = Mathf.Atan2(-attackDirection.x, attackDirection.y) * Mathf.Rad2Deg;
            activeAttackCollider = Instantiate(attackColliderPrefab, playerSprite.position, Quaternion.Euler(0, 0, angle));
            activeAttackCollider.Player = playerMovement;
            activeAttackCollider.Direction = attackDirection;
            PointTowardAttackCLientRpc(attackDirection);
            Destroy(activeAttackCollider.gameObject, attackDuration);
        }
    }

    [ClientRpc]
    private void PointTowardAttackCLientRpc(Vector2 attackDirection)
    {
        // Set the rotation so that the down direction of the sprite points towards movement
        if (attackDirection != Vector2.zero) // Prevent rotation when not moving
        {
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg; // Get angle in degrees
            playerMovement.playerVisual.rotation = Quaternion.Euler(0, 0, angle + 90f); // Rotate so that down faces movement
        }
        playerMovement.animator.SetTrigger("Attack");
    }
}
