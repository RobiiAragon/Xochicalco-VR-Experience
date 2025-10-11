using System;
using System.Collections.Generic;
using Fragilem17.MirrorsAndPortals;
using HexabodyVR.PlayerController;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using UnityEngine;
using FragilemPortal = Fragilem17.MirrorsAndPortals.Portal;

[RequireComponent(typeof(CapsuleCollider))]
public class PortalsHexaTeleporter : MonoBehaviour
{
    Transform _inTransform;
    Transform _outTransform;

    public HexaBodyPlayer4 Hexa;
    public Vector3 TeleportPosition;
    public List<HVRHandGrabber> Hands = new List<HVRHandGrabber>();

	private CapsuleCollider _capsuleCollider;
    private bool _wannaTeleport;
	private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

	private void OnEnable()
	{
        _capsuleCollider = GetComponent<CapsuleCollider>();            
    }

	private void FixedUpdate()
    {
        if (_capsuleCollider)
        {
            _capsuleCollider.height = Vector3.Distance(Hexa.Camera.transform.position, Hexa.LocoBall.transform.position) + (Hexa.LocoCollider.radius * 2);
            _capsuleCollider.center = new Vector3(0, (-_capsuleCollider.height / 2), 0);
        }

        if (_wannaTeleport)
        {
            _wannaTeleport = false;


            List<Rigidbody> _hexaRB = new List<Rigidbody>();
            _hexaRB.Add(Hexa.Head);
            _hexaRB.Add(Hexa.Pelvis);
            _hexaRB.Add(Hexa.Knee);
            _hexaRB.Add(Hexa.LeftHandRigidBody);
            _hexaRB.Add(Hexa.RightHandRigidBody);
            _hexaRB.Add(Hexa.LocoBall);

            // add held object to teleport them
            for (int i = 0; i < Hands.Count; i++)
            {
                HVRHandGrabber hand = Hands[i];
                if (hand && hand.HeldObject && hand.HeldObject.Rigidbody)
                {
                    Rigidbody rb = hand.HeldObject.Rigidbody;
                    _hexaRB.Add(rb);
                }

            }

            foreach (Rigidbody rb in _hexaRB)
            {
                // Position the camera behind the other portal.
                Vector3 relativePos = _inTransform.InverseTransformPoint(rb.transform.position);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                rb.transform.position = _outTransform.TransformPoint(relativePos);
                rb.position = _outTransform.TransformPoint(relativePos);

                // Rotate the camera to look through the other portal.
                Quaternion relativeRot = Quaternion.Inverse(_inTransform.rotation) * rb.transform.rotation;
                relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
                rb.transform.rotation = _outTransform.rotation * relativeRot;
                rb.rotation = _outTransform.rotation * relativeRot;


                Vector3 relativeVel = _inTransform.InverseTransformDirection(rb.velocity);
                relativeVel = halfTurn * relativeVel;
                rb.velocity = _outTransform.TransformDirection(relativeVel);

                Vector3 relativeAngVel = _inTransform.InverseTransformDirection(rb.angularVelocity);
                relativeAngVel = halfTurn * relativeAngVel;
                rb.angularVelocity = _outTransform.TransformDirection(relativeAngVel);
            }

            Vector3 relativeVel2 = _inTransform.InverseTransformDirection(Hexa.GoalVelocity);
            relativeVel2 = halfTurn * relativeVel2;
            
            Hexa.GoalVelocity = _outTransform.TransformDirection(relativeVel2);
        }
    }



    public void Teleport(PortalableObject po, FragilemPortal fromPortal)
    {
        TeleportPosition = po.TransformToPortal.position;

        _inTransform = fromPortal.PortalSurface.transform;
        _outTransform = fromPortal.OtherPortal.PortalSurface.transform;

        //Hexa.Stop();
        _wannaTeleport = true;
        FixedUpdate();



        // instant move clones to other portal
        CloneRenderer[] childCloneRenderersArr = Hexa.GetComponentsInChildren<CloneRenderer>(true);
        List<CloneRenderer> childCloneRenderers = new List<CloneRenderer>(childCloneRenderersArr);

        for (int i = 0; i < Hands.Count; i++)
        {
            HVRHandGrabber hand = Hands[i];
            childCloneRenderers.AddRange(hand.GetComponentsInChildren<CloneRenderer>(true));
        }

        for (int i = 0; i < childCloneRenderers.Count; i++)
        {
            fromPortal.PortalTransporter.cloneObjects.Remove(childCloneRenderers[i]);
            childCloneRenderers[i].ExitPortal(fromPortal, false);

            Collider[] myColliders = childCloneRenderers[i].GetComponents<Collider>();
            foreach (Collider c in myColliders)
            {

                //Debug.DrawLine(childCloneRenderers[i].transform.position, fromPortal.OtherPortal.PortalTransporter.transform.position, Color.cyan, 10);                    

                Vector3 direction;
                float distance;
                bool overlapped = Physics.ComputePenetration(
                    c, childCloneRenderers[i].transform.position, childCloneRenderers[i].transform.rotation,
                    fromPortal.OtherPortal.PortalTransporter.MyCollider, fromPortal.OtherPortal.PortalTransporter.transform.position, fromPortal.OtherPortal.PortalTransporter.transform.rotation,
                    out direction, out distance);

                //Debug.Log(childCloneRenderers[i].name + " : " + direction + " : " + distance + " : " + overlapped);

                if (overlapped)
                {
                    //Debug.Log("AFTER WARP I WOULD BE TOUCHING SO SET ME IN PORTAL: " + childCloneRenderers[i].name);
                    childCloneRenderers[i].SetIsInPortal(fromPortal.OtherPortal, false);
                    fromPortal.OtherPortal.PortalTransporter.cloneObjects.Add(childCloneRenderers[i]);
                    break;
                }
            }
        }
    }

    public void OnEnterPortal(FragilemPortal portal)
    {
        if (portal.wallCollider)
        {
            // if that's the case.. then we can turn of the colliders
            // disable collisions with other portal
            Collider[] colliders = Hexa.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Physics.IgnoreCollision(colliders[i], portal.wallCollider);
            }

            for (int i = 0; i < Hands.Count; i++)
            {
                HVRHandGrabber hand = Hands[i];
                Collider[] handColliders = hand.GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < handColliders.Length; j++)
                {
                    Physics.IgnoreCollision(handColliders[j], portal.wallCollider);
                }
            }
        }
    }
    public void OnExitPortal(FragilemPortal portal)
    {
        if (portal.wallCollider)
        {
            Debug.Log("OnExitPortal: Intentando reactivar colisiones con la pared del portal.");

            // Reactivar colisiones para el cuerpo principal
            Collider[] colliders = Hexa.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Physics.IgnoreCollision(colliders[i], portal.wallCollider, false);
                Debug.Log($"Colisión reactivada entre {colliders[i].name} y {portal.wallCollider.name}");
            }

            // Reactivar colisiones para las manos
            for (int i = 0; i < Hands.Count; i++)
            {
                HVRHandGrabber hand = Hands[i];
                Collider[] handColliders = hand.GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < handColliders.Length; j++)
                {
                    Physics.IgnoreCollision(handColliders[j], portal.wallCollider, false);
                    Debug.Log($"Colisión reactivada entre {handColliders[j].name} y {portal.wallCollider.name}");
                }
            }

            // Verificar si algún collider sigue en contacto con la pared
            foreach (Collider collider in colliders)
            {
                if (Physics.ComputePenetration(
                    collider, collider.transform.position, collider.transform.rotation,
                    portal.wallCollider, portal.wallCollider.transform.position, portal.wallCollider.transform.rotation,
                    out Vector3 direction, out float distance))
                {
                    // Ajustar la posición para evitar conflictos
                    collider.transform.position += direction * (distance + 0.1f); // Mover un poco más lejos para evitar contacto
                    Debug.Log($"Ajustando posición de {collider.name} para evitar penetración con {portal.wallCollider.name}");
                }
            }

            // Asegurarse de que el jugador esté completamente fuera de la pared
            StartCoroutine(EnsurePlayerOutsideWall(portal.wallCollider));
        }
    }

    private System.Collections.IEnumerator EnsurePlayerOutsideWall(Collider wallCollider)
    {
        yield return new WaitForFixedUpdate(); // Esperar un frame de física para evitar conflictos

        Collider[] colliders = Hexa.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (Physics.ComputePenetration(
                collider, collider.transform.position, collider.transform.rotation,
                wallCollider, wallCollider.transform.position, wallCollider.transform.rotation,
                out Vector3 direction, out float distance))
            {
                // Mover el jugador completamente fuera de la pared
                collider.transform.position += direction * (distance + 0.1f);
            }
        }

        Debug.Log("Jugador ajustado fuera de la pared.");
    }
}