using HurricaneVR.Framework.Core.Player;
using HurricaneVR.Framework.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    public class PortalHVRTeleporter : HVRTeleporter
    {
	    public Vector3 TeleportDestinationRot { get; protected set; }

        private Quaternion _leftRot = Quaternion.identity;
        private Vector3 _leftPos = Vector3.zero;
        private Quaternion _rightRot = Quaternion.identity;
        private Vector3 _rightPos = Vector3.zero;
        private Quaternion _leftGrabRot = Quaternion.identity;
        private Vector3 _leftGrabPos = Vector3.zero;
        private Quaternion _rightGrabRot = Quaternion.identity;
        private Vector3 _rightGrabPos = Vector3.zero;

        protected override void UpdatePlayerPosition(Vector3 position)
	    {
		    base.UpdatePlayerPosition(position);

		    if (TeleportDestinationRot != Vector3.zero)
		    {
			    Player.FaceDirection(TeleportDestinationRot);
			    TeleportDestinationRot = Vector3.zero;
		    }

            if (LeftHand)
            {
                LeftHand.transform.SetPositionAndRotation(Player.transform.TransformPoint(_leftPos), Player.transform.rotation * _leftRot);
                if (LeftHand.GrabbedTarget)
                {
                    LeftHand.GrabbedTarget.transform.SetPositionAndRotation(LeftHand.transform.TransformPoint(_leftGrabPos), LeftHand.transform.rotation * _leftGrabRot);
                }
            }

            if (RightHand)
            {
                RightHand.transform.SetPositionAndRotation(Player.transform.TransformPoint(_rightPos), Player.transform.rotation * _rightRot);
                if (RightHand.GrabbedTarget)
                {
                    RightHand.GrabbedTarget.transform.SetPositionAndRotation(RightHand.transform.TransformPoint(_rightGrabPos), RightHand.transform.rotation * _rightGrabRot);
                }
            }
        }

        /*
	    public override void Teleport(Vector3 position, Vector3 direction, bool faceDirection)
	    {
            if (!Teleport(position))
                return;

            if (!Player) return;

            TeleportDestinationRot = direction;

            //snapshot relative values and then reapply after rotating the player
            if (LeftHand)
            {
                Player.GetRelativeValues(LeftHand, out _leftPos, out _leftRot);
                if (LeftHand.GrabbedTarget)
                {
                    LeftHand.GetRelativeValues(LeftHand.GrabbedTarget, out _leftGrabPos, out _leftGrabRot);
                }
            }

            if (RightHand)
            {
                Player.GetRelativeValues(RightHand, out _rightPos, out _rightRot);
                if (RightHand.GrabbedTarget) RightHand.GetRelativeValues(RightHand.GrabbedTarget, out _rightGrabPos, out _rightGrabRot);
            }
        }
        */
    }
}
