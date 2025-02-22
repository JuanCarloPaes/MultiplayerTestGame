using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    private PlayerMovement _player;
    private Vector2 _direction;

    public Vector2 Direction { get => _direction; set => _direction = value; }
    public PlayerMovement Player { get => _player; set => _player = value; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EventManager.TriggerEvent("AttackHit", this, collision.gameObject);
    }
}
