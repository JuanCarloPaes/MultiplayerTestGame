using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerMovement : NetworkBehaviour
{
    public Transform playerSprite;
    public Transform playerVisual;
    public Animator animator;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float jumpSpeed = 5f;
    public float knockbackForce = 3f;
    public float knockbackHeight = 3f;
    public float knockbackDuration = 0.5f;
    public float stunDuration = 1.0f; // Stun duration (in seconds)
    public TMP_Text playerIndicatorText;
    public ParticleSystem dustEffect;
    public ParticleSystem stunEffect;

    private float timeToNextScoring;
    private float airProgress = 0f;
    private bool isInAir = false;
    private bool isStunned = false;
    private Vector2 knockbackDirection;
    private float stunTimer = 0f; // Timer for stun duration

    public NetworkVariable<int> playerId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> movement = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> fell = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float currentHeight = 0f;
    private bool isInsideSpotlight;

    public float CurrentHeight { get => currentHeight; set => currentHeight = value; }
    public bool IsStunned { get => isStunned; set => isStunned = value; }

    private void Start()
    {
        EventManager.AddListener<Attack, GameObject>("AttackHit", CheckAttack);
        fell.OnValueChanged += DoFall;
    }

    private void DoFall(bool previousValue, bool newValue)
    {
        gameObject.SetActive(!newValue);
        if (!newValue)
            StartStun();
        if (IsHost && newValue)
            EventManager.TriggerEvent("PlayerRespawn", transform, fell);
    }

    public override void OnDestroy()
    {
        EventManager.RemoveListener<Attack, GameObject>("AttackHit", CheckAttack);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerIndicatorText.text = "P" + (playerId.Value + 1);
    }

    private void CheckAttack(Attack attack, GameObject hit)
    {
        if (isStunned) return;
        if (hit != playerSprite.gameObject) return;
        if (attack.Player == this) return;

        if (Mathf.Abs(attack.Player.CurrentHeight - CurrentHeight) < 0.5f) // Ensure attacks only hit if on the same height level
        {
            StartAirborneStateClientRpc(attack.Direction, true);
            StartStun();
        }
    }

    [ClientRpc]
    private void StartAirborneStateClientRpc(Vector2 direction, bool stun)
    {
        if (!isInAir)
        {
            isInAir = true;
            animator.SetBool("isInAir", true);
            airProgress = 0f;
            knockbackDirection = direction.normalized * knockbackForce;
        }
        else
        {
            knockbackDirection += direction.normalized * knockbackForce; // Stack knockback
            airProgress = Mathf.Clamp(airProgress - 0.2f, 0f, 1f);
        }
        if (stun)
        {
            animator.SetTrigger("Stunned");
            stunEffect.Play();
        }
    }

    private void StartStun()
    {
        IsStunned = true;
        stunTimer = stunDuration;
    }

    private void Update()
    {
        if (IsHost)
        {
            if (IsStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0)
                {
                    IsStunned = false;
                }
            }
            if (isInsideSpotlight)
            {
                if (Time.time >= timeToNextScoring)
                {
                    EventManager.TriggerEvent("ChangePlayerScore", OwnerClientId, 5);
                    timeToNextScoring = Time.time + 1f;
                }
            }
        }
        if (IsOwner)
        {
            // Movement input
            Vector2 inputMovement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            // Normalize movement to prevent diagonal speed boost
            if (inputMovement.magnitude > 1)
            {
                inputMovement.Normalize();
            }

            movement.Value = inputMovement;

            // Jump input
            if (Input.GetKeyDown(KeyCode.Space) && !isInAir)
            {
                RequestJumpServerRpc();
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsHost && !IsStunned && !fell.Value)
        {
            // Apply movement
            transform.position += new Vector3(movement.Value.x * moveSpeed * Time.fixedDeltaTime, movement.Value.y * moveSpeed * Time.fixedDeltaTime, 0);
        }

        bool isMoving = movement.Value != Vector2.zero;

        animator.SetBool("Moving", isMoving);
        var emission = dustEffect.emission;
        emission.enabled = isMoving; // Instantly start or stop emitting particles


        if (isInAir)
        {
            airProgress += Time.fixedDeltaTime * jumpSpeed;
            float airOffset = Mathf.Sin(airProgress * Mathf.PI) * jumpHeight;

            if (IsHost)
            {
                transform.position += new Vector3(knockbackDirection.x, knockbackDirection.y, 0) * (airOffset * Time.fixedDeltaTime);
            }

            playerSprite.localPosition = Vector3.up + Vector3.up * airOffset;
            CurrentHeight = airOffset;

            if (airProgress >= 1f)
            {
                isInAir = false;
                animator.SetBool("isInAir", false);
                playerSprite.localPosition = Vector3.up;
                CurrentHeight = 0f;
            }
        }

        if (IsServer)
        {
            if (!CheckIfOnGround()&&(!isInAir||airProgress>=1f))
            {
                EventManager.TriggerEvent("ChangePlayerScore", OwnerClientId, -50);
                fell.Value = true;
            }
        }
    }

    [ClientRpc]
    private void PlayerDeathClientRpc()
    {
        gameObject.SetActive(false);
    }

    private bool CheckIfOnGround()
    {
        float groundCheckRadius = 0.1f; // Pequeno raio para verificar colisão
        Vector2 position = transform.position; // Posição do personagem
        Collider2D groundCollider = Physics2D.OverlapCircle(position, groundCheckRadius, LayerMask.GetMask("Ground"));

        return groundCollider != null;
    }

    [ServerRpc]
    private void RequestJumpServerRpc()
    {
        StartAirborneStateClientRpc(Vector2.zero, false);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsHost) return;
        if (collision.CompareTag("Spotlight"))
        {
            isInsideSpotlight = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Spotlight"))
        {
            isInsideSpotlight = false;
        }
    }

}
