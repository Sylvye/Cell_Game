using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour
{
    public float maxHP;
    public float hp;
    public float collisionDamageMult;
    public GameObject deathFX;
    public List<Resistance> resistances;
    public Collider2D col;
    public Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = rb.GetComponent<Collider2D>();
    }

    /// <summary>
    /// Returns true if death occurs
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool DealDamage(Damage amount)
    {
        if (hp > 0)
        {
            float amt = amount.Evaluate(GetResists());
            hp -= amt;
            if (amt < 100)
                Debug.Log(gameObject.name + ": Just took ~" + Mathf.Round(amt) + " damage!");
            else
                Debug.LogWarning(gameObject.name + ": Just took ~" + Mathf.Round(amt) + " damage!");
            if (hp < 0)
            {
                hp = 0;
                return OnDeath();
            }
        }
        return false;
    }

    /// <summary>
    /// Returns whether or not the death is confirmed
    /// </summary>
    /// <returns></returns>
    public virtual bool OnDeath()
    {
        SpawnDeathFX();
        Destroy(gameObject);
        return true;
    }

    public void SpawnDeathFX()
    {
        if (deathFX != null)
        {
            Instantiate(deathFX, transform.position, Quaternion.identity);
        }
    }

    public virtual List<Resistance> GetResists()
    {
        return resistances;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null && collision.gameObject.TryGetComponent(out Body other))
        {
            Debug.Log(gameObject.name + ": Collided with " + collision.gameObject.name + "\nVelocity: " + collision.relativeVelocity.magnitude);
            float damageAmount = collision.relativeVelocity.magnitude * rb.mass * collisionDamageMult;
            other.DealDamage(new Damage(damageAmount, Damage.Type.Kinetic, this));
        }
    }
}
