using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forcefield : MonoBehaviour
{
    public float shrinkWaitTime;
    public float shrinkAmount;
    public float shrinkDuration;
    public float minShrinkAmount;

    public int playerDamage;

    private float lastShrinkEndTime;
    private bool shrinking;
    private float targetDiameter;
    private float lastPlayerCheckTime;

    private void Start()
    {
        lastShrinkEndTime = Time.time;
        targetDiameter = transform.localScale.x;
    }

    private void Update()
    {
        if (shrinking)
        {
            // Shrink the scale
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one * targetDiameter, (shrinkAmount / shrinkDuration) * Time.deltaTime);

            // Check if at target diameter
            if (transform.localScale.x == targetDiameter)
                shrinking = false;
        }
        else
        {
            // can we shrink again?
            if (Time.time - lastShrinkEndTime >= shrinkWaitTime && transform.localScale.x > minShrinkAmount)
            {
                Shrink();
            }
        }

        CheckPlayers();
    }

    private void Shrink()
    {
        shrinking = true;

        // don't go below min amount
        if (transform.localScale.x - shrinkAmount > minShrinkAmount)
            targetDiameter -= shrinkAmount;
        else
            targetDiameter = minShrinkAmount;

        lastShrinkEndTime = Time.time + shrinkDuration;
    }

    private void CheckPlayers()
    {
        if (Time.time - lastPlayerCheckTime > 1.0f)
        {
            lastPlayerCheckTime = Time.time;

            foreach (PlayerController player in GameManager.instance.players)
            {
                if (!player || player.dead) continue;

                if (Vector3.Distance(Vector3.zero, player.transform.position) >= transform.localScale.x)
                {
                    player.photonView.RPC("TakeDamage", player.photonPlayer, 0, playerDamage);
                }
            }
        }
    }
}
