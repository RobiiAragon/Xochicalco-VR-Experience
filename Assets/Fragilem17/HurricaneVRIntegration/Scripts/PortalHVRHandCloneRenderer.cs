using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    public class PortalHVRHandCloneRenderer : CloneRenderer
    {
        //private static HVRTeleporter _HVRTeleporter;

        [NonSerialized]
        public List<CloneRenderer> ChildCloneRenderers;

        //protected Portal _newPortal;

        public HVRHandGrabber HandGrabber;


		protected override void OnEnable()
		{
			base.OnEnable();
            ChildCloneRenderers = new List<CloneRenderer>();

            /*if (_HVRTeleporter == null)
            {
                _HVRTeleporter = FindObjectOfType<HVRTeleporter>();
            }*/

            /*if (_HVRTeleporter && PortalableObject.MasterPortalable)
            {
                PortalableObject.MasterPortalable.OnPrePortalEvent.AddListener(OnPrePortalEvent);
                _HVRTeleporter.PositionUpdate.AddListener(OnPositionUpdate);
			}
			else
			{
                Debug.Log("OH OH no master or teleporter");
			}
            */
            if (HandGrabber)
            {
                HandGrabber.Grabbed.AddListener(OnHandGrabbed);
            }
        }


		protected void OnHandGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            //Debug.Log("adding grabbable to childCloneRenderers: " + grabbable.name);
            ChildCloneRenderers.AddRange(grabbable.GetComponentsInChildren<CloneRenderer>());

            /*ChildCloneRenderers.ForEach((c) =>
            {
                c.EnableCloneColliders();
            });*/

            grabbable.Released.AddListener(OnGrabbableReleased);
        }

		private void OnGrabbableReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (!grabbable.IsBeingHeld)
            { 
                //Debug.Log("OnGrabbableReleased Removing grabbable2 from childCloneRenderers: " + grabbable.name);
                grabbable.Released.RemoveListener(OnGrabbableReleased);
                CloneRenderer[] clones = grabbable.GetComponentsInChildren<CloneRenderer>();

                for (int i = 0; i < clones.Length; i++)
                {
                    //clones[i].DisableCloneColliders();
                    ChildCloneRenderers.Remove(clones[i]);
                }
            }
        }

		protected override void OnDisable()
		{
			base.OnDisable();
            /*if (PortalableObject.MasterPortalable)
            {
                PortalableObject.MasterPortalable.OnPostPortalEvent.RemoveListener(OnPrePortalEvent);
            }*/
            /*if (_HVRTeleporter)
            {
                _HVRTeleporter.PositionUpdate.RemoveListener(OnPositionUpdate);
            }*/
            if (HandGrabber)
            {
                HandGrabber.Grabbed.RemoveListener(OnHandGrabbed);
            }
        }

		/*private void OnPositionUpdate(Vector3 arg0)
        {
            if (_newPortal != null)
            {
                //Debug.Log("OnPositionUpdate");
                _newPortal.OtherPortal.PortalTransporter.cloneObjects.Remove(this);
                ExitPortal(_newPortal.OtherPortal, false);

                _newPortal.PortalTransporter.cloneObjects.Add(this);
                SetIsInPortal(_newPortal, false);

                if (ChildCloneRenderers != null && ChildCloneRenderers.Count > 0)
                {
                    foreach (CloneRenderer cr in ChildCloneRenderers)
                    {
                        _newPortal.OtherPortal.PortalTransporter.cloneObjects.Remove(cr);
                        cr.ExitPortal(_newPortal.OtherPortal, false);

                        _newPortal.PortalTransporter.cloneObjects.Add(cr);
                        cr.SetIsInPortal(_newPortal, false);
                    }
                }
                _newPortal = null;
            }
        }

        private void OnPrePortalEvent(PortalableObject portalableObj, Portal portalFrom)
        {
            //Debug.Log("PostMasterPortal");
            _newPortal = portalFrom.OtherPortal;
        }*/
    }
}