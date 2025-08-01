using UnityEngine;

public class ParticleCannon : Cannon
{
    public ParticleSystem ps;
    public int rate;
    public float kickbackMult;

    public override void Activate()
    {
        ps.Emit(rate);
        if (kickbackMult != 0)
        {
            float flatMult = 10;
            Vector2 forceDir = AngleHelper.DegreesToVector(transform.eulerAngles.z + 180).normalized;
            Vector2 force = flatMult * kickbackMult * forceDir;
            Vector2 relative = (Vector2)transform.position - vesselRB.worldCenterOfMass;
            float torque = relative.x * force.y - relative.y * force.x;
            vesselRB.AddForce(force, ForceMode2D.Force);
            vesselRB.AddTorque(torque, ForceMode2D.Force);
            Debug.DrawLine(transform.position, transform.position - (Vector3)force, Color.purple);
        }
    }

    public override bool CanFire()
    {
        return true;
    }
}
