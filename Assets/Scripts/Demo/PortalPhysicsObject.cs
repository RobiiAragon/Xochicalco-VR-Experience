using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PortalPhysicsObject : PortalTraveller {

    public float force = 10;
    Rigidbody rb; // Renombrado para evitar conflictos
    public Color[] colors;
    static int i;

    void Awake () {
        rb = GetComponent<Rigidbody> ();
        graphicsObject.GetComponent<MeshRenderer> ().material.color = colors[i];
        i++;
        if (i > colors.Length - 1) {
            i = 0;
        }
    }

    public override void Teleport (Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        base.Teleport (fromPortal, toPortal, pos, rot);
        rb.velocity = toPortal.TransformVector (fromPortal.InverseTransformVector (rb.velocity));
        rb.angularVelocity = toPortal.TransformVector (fromPortal.InverseTransformVector (rb.angularVelocity));
    }
}