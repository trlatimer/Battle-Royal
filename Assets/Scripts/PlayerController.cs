using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun
{
    [Header("Info")]
    public int id;
    private int curAttackerId;

    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;
    public int curHP;
    public int maxHP;
    public int kills;
    public bool dead;

    private bool flashingDamage;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;
    public PlayerWeapon weapon;
    public MeshRenderer mr;

    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;

        // Add to players list
        GameManager.instance.players[id - 1] = this;

        // not local player
        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            rig.isKinematic = true;
        }
        else
        {
            GameUI.instance.Initialize(this);
        }
    }

    private void Update()
    {
        if (photonView.IsMine && !dead)
        {
            Move();

            if (Input.GetKeyDown(KeyCode.Space))
                TryJump();

            if (Input.GetMouseButtonDown(0))
                weapon.TryShoot();
        }
    }

    private void Move()
    {
        // Get input axis
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate direction relative to forward direction
        Vector3 dir = (transform.forward * z + transform.right * x) * moveSpeed;
        dir.y = rig.velocity.y;

        // Set as velocity
        rig.velocity = dir;
    }

    private void TryJump()
    {
        // Create ray facing down
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 1.5f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead) return;

        curHP -= damage;
        curAttackerId = attackerId;

        // Flash player red
        photonView.RPC("DamageFlash", RpcTarget.Others);

        // Update Health UI
        GameUI.instance.UpdateHealthBar();

        // Die if no health
        if (curHP <= 0)
            photonView.RPC("Die", RpcTarget.All);
    }

    [PunRPC]
    private void DamageFlash()
    {
        if (flashingDamage) return;

        StartCoroutine(DamageFlashCoroutine());

        IEnumerator DamageFlashCoroutine()
        {
            flashingDamage = true;

            Color defaultColor = mr.material.color;
            mr.material.color = Color.red;

            yield return new WaitForSeconds(0.05f);

            mr.material.color = defaultColor;
            flashingDamage = false;
        }
    }

    [PunRPC]
    private void Die()
    {
        curHP = 0;
        dead = true;

        GameManager.instance.alivePlayers--;

        // Host checks win
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();

        // is this us?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);

            // Set cam to spectator
            GetComponentInChildren<CameraController>().SetAsSpectator();

            // Disable physics, hide player
            rig.isKinematic = true;
            transform.position = new Vector3(0, -50, 0);
        }
    }

    [PunRPC]
    public void AddKill()
    {
        kills++;

        // Update UI
        GameUI.instance.UpdatePlayerInfoText();
    }

    [PunRPC]
    public void Heal(int amount)
    {
        curHP = Mathf.Clamp(curHP + amount, 0, maxHP);

        // Update UI
        GameUI.instance.UpdateHealthBar();
    }
}
