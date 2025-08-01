using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class VesselComponent : Body
{
    public float respawnCooldown;
    public bool respawning = false;
    protected Vessel parentVessel;
    protected Rigidbody2D vesselRB;
    private SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake();
        vesselRB = GetComponentInParent<Rigidbody2D>();
        sr = GetComponentInParent<SpriteRenderer>();
        parentVessel = GetComponentInParent<Vessel>();
    }

    public override bool OnDeath()
    {
        SpawnDeathFX();
        StartCoroutine(RespawnCoroutine());
        return true;
    }

    public override List<Resistance> GetResists()
    {
        return parentVessel.resistances;
    }

    private IEnumerator RespawnCoroutine()
    {
        sr.enabled = false;
        col.enabled = false;
        respawning = true;
        yield return new WaitForSeconds(respawnCooldown);
        respawning = false;
        sr.enabled = true;
        col.enabled = true;
    }
}
