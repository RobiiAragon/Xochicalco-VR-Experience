using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
	public class PortalHVRPortalableObject : PortalableObject
	{
		public HVRGrabbable Grabbable;

		protected override void Awake()
		{
			base.Awake();

			if (Grabbable == null)
			{
				Grabbable = GetComponent<HVRGrabbable>();
				if (Grabbable == null && TransformToPortal != null)
				{
					Grabbable = TransformToPortal.GetComponent<HVRGrabbable>();
				}
			}

			if (Grabbable != null)
			{
				Grabbable.HandGrabbed.AddListener(OnHandGrabbed);
				Grabbable.HandReleased.AddListener(OnHandReleased);
				Grabbable.Socketed.AddListener(OnSocketed);
				Grabbable.UnSocketed.AddListener(OnUnSocketed);
			}
		}

		public override void SetIsInPortal(Portal portal)
		{
			_inPortal = portal;
			OnEnterPortalCollider?.Invoke(portal);
			//if (PortallingEnabled)
			//{
				//CheckForParentPortalables();
				//Debug.Log("setInPortal " + this.name + " inPortal: " + portal.name + " turning OFF colliders ");
				if (portal.wallCollider)
				{
					// if that's the case.. then we can turn of the colliders
					// disable collisions with other portal
					_colliders = TransformToPortal.GetComponentsInChildren<Collider>(true);
					for (int i = 0; i < _colliders.Length; i++)
					{
						Physics.IgnoreCollision(_colliders[i], portal.wallCollider);
					}
				}
			//}
		}

		protected override void OnMasterPortalEnterPortalCollider(Portal portal)
		{
			if (Grabbable.IsHandGrabbed)
			{
				SetIsInPortal(portal);
			}
		}

		private void OnHandGrabbed(HVRHandGrabber grabber, HVRGrabbable grabbable)
		{
			SetPortallingEnabled = false;
		}

		private void OnHandReleased(HVRHandGrabber grabber, HVRGrabbable grabbable)
		{
			// other hand could still be grabbing it
            if (!grabbable.IsHandGrabbed)
            {
				SetPortallingEnabled = true;
            }
		}
		private void OnSocketed(HVRSocket socket, HVRGrabbable grabbable)
		{
			SetPortallingEnabled = false;
		}

		private void OnUnSocketed(HVRSocket socket, HVRGrabbable grabbable)
		{
			SetPortallingEnabled = true;
		}

	}
}
