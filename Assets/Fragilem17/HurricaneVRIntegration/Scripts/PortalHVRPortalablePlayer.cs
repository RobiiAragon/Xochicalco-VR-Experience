using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fragilem17.MirrorsAndPortals
{
	public class PortalHVRPortalablePlayer : PortalableObject
    {
        public GameObject RigRoot;

        private static HVRTeleporter _HVRTeleporter;

        public List<Transform> PlayerColliderRootElements;

        protected List<Collider> _playerColliders;

        protected static HVRTeleportCollisonHandler _hvrTeleportCollisionHandler;
        protected static LayerMask _originalCollisionHandlerMask;

        public UnityEvent<Vector3, Vector3, bool> OnPostPortalEventPositionRotation;
        private CapsuleCollider _capsuleCollider;
        private Portal _fromPortal;


        protected override void Start()
		{
			base.Start();

            if (_HVRTeleporter == null)
            {
                _HVRTeleporter = FindObjectOfType<HVRTeleporter>();
            }

            if (_HVRTeleporter)
            {
                _HVRTeleporter.PositionUpdate.AddListener(OnPositionUpdate);
            }

            if (_hvrTeleportCollisionHandler == null)
            {
                _hvrTeleportCollisionHandler = FindObjectOfType<HVRTeleportCollisonHandler>();
                _originalCollisionHandlerMask = _hvrTeleportCollisionHandler.LayerMask;
            }

			if (IsMasterPortalableObject)
			{
                _characterController = _hvrTeleportCollisionHandler.GetComponent<CharacterController>();
                _capsuleCollider = GetComponent<CapsuleCollider>();
			}
        }

        private void OnDestroy()
        {
            if (_HVRTeleporter)
            {
                _HVRTeleporter.PositionUpdate.RemoveListener(OnPositionUpdate);
            }
        }

        protected void FixedUpdate()
        {
            if (IsMasterPortalableObject && _characterController && _capsuleCollider)
            {
                _capsuleCollider.height = _characterController.height;
                _capsuleCollider.center = new Vector3(0, (-_characterController.height / 2), 0);
                _capsuleCollider.radius = _characterController.radius;
            }
        }

        

        protected override void PostWarp(Portal fromPortal)
		{
            OnPostPortalEventPositionRotation.Invoke(TransformToPortal.position, TransformToPortal.forward, true);
            base.PostWarp(fromPortal);

            /*
            float scaleFactor = (fromPortal.OtherPortal.transform.lossyScale.x / fromPortal.transform.lossyScale.x);
            float newScale = 1;
            if (RigRoot != null)
            { 
                newScale = RigRoot.transform.localScale.x * scaleFactor;
            }
                                                
            // this moved the CharacterController of HVR
            OnPostPortalEventPositionRotation.Invoke(TransformToPortal.position, TransformToPortal.forward, true, 1);
            //Debug.Break();
            //Vector3 newPos = _HVRTeleporter.CharacterController.transform.position;

            base.PostWarp(fromPortal);

			if (RigRoot != null)
			{
                _HVRTeleporter.BeforeDashTeleport.Invoke(TransformToPortal.position);
                Debug.Log("newScale= " + newScale);
                ScaleAround(RigRoot, TransformToPortal.position, newScale);
                //Debug.Break();
                _HVRTeleporter.PositionUpdate.Invoke(TransformToPortal.position);
                _HVRTeleporter.AfterDashTeleport.Invoke(TransformToPortal.position);
                //_HVRTeleporter.CharacterController.transform.position = newPos;

            }
            */


        }

        protected override void OnMasterPortalablePreWarp(PortalableObject masterObject, Portal fromPortal)
        {
            _fromPortal = fromPortal;
            base.OnMasterPortalablePreWarp(masterObject, fromPortal);

        }

        private void OnPositionUpdate(Vector3 arg0)
        {
            if (_fromPortal != null)
            {
                /*if (PortalAlongWithMasterPortalable)
                {
                    Debug.Log("HVR OnMasterPortalableWarped! i will go along " + name);
                    PortalFrom(_fromPortal, true);
                }*/
                //base.PostWarp(_fromPortal);         
                _fromPortal = null;
            }
        }

        protected override void OnMasterPortalableWarped(PortalableObject masterObject, Portal fromPortal)
        {
            base.OnMasterPortalableWarped(masterObject, fromPortal);
        }

        protected override CloneRenderer[] FindChildCloneRenderers()
        {
            List<CloneRenderer> newList = new List<CloneRenderer>();
            PortalHVRHandCloneRenderer hvrHCR = GetComponent<PortalHVRHandCloneRenderer>();

            if (hvrHCR != null)
            {
                newList.Add(hvrHCR);
            }

            if (hvrHCR != null && hvrHCR.ChildCloneRenderers != null)
            {
                newList.AddRange(hvrHCR.ChildCloneRenderers);
            }

            // the playerColliders are potential condidates for cloneRenderers
            PlayerColliderRootElements.ForEach((Transform playerRootElement) => {
                newList.AddRange(playerRootElement.GetComponentsInChildren<CloneRenderer>(false));
            });

            newList.AddRange(base.FindChildCloneRenderers());
            //Debug.Log("FINDING CHILD CLONE RENDERERS on " + gameObject.name + " : " + newList.Count);
            return newList.ToArray();
        }

        protected override void PreWarp(Portal fromPortal)
		{
			if (IsMasterPortalableObject)
			{
                _hvrTeleportCollisionHandler.LayerMask = 0;
			}
            base.PreWarp(fromPortal);
        }


		public override void SetIsInPortal(Portal portal)
		{
            //Debug.Log("SetIsInPortal: " + portal.name);
			base.SetIsInPortal(portal);
            if (portal.wallCollider)
            {
                if (PortallingEnabled)
                {
                    // disable collisions with other portal
                    _playerColliders = GetPlayerColliders(); 
                    for (int i = 0; i < _playerColliders.Count; i++)
                    {
                        Physics.IgnoreCollision(_playerColliders[i], portal.wallCollider);
                    }
                }
            }
        }

		private List<Collider> GetPlayerColliders()
		{
            List<Collider> allColliders = new List<Collider>();
            PlayerColliderRootElements.ForEach((Transform playerRootElement)=> {
                allColliders.AddRange(playerRootElement.GetComponentsInChildren<Collider>(true));            
            });
            return allColliders;
        }

		public override void ExitPortal(Portal portal)
		{
			base.ExitPortal(portal);

			if (IsMasterPortalableObject == true)
			{
                _hvrTeleportCollisionHandler.LayerMask = _originalCollisionHandlerMask;
			}

            if (portal.wallCollider && _playerColliders != null && _playerColliders.Count > 0)
            {
                for (int i = 0; i < _playerColliders.Count; i++)
                {
                    Physics.IgnoreCollision(_playerColliders[i], portal.wallCollider, false);
                }
            }
        }

        public void ScaleAround(GameObject target, Vector3 pivot, float newScale)
        {
            Vector3 pivotDelta = target.transform.position - pivot;
            Vector3 scaleFactor = new Vector3(
                newScale / target.transform.localScale.x,
                newScale / target.transform.localScale.y,
                newScale / target.transform.localScale.z);
            pivotDelta.Scale(scaleFactor);
            target.transform.localPosition = pivot + pivotDelta;

            //scale
            target.transform.localScale = Vector3.one * newScale;
        }
    }
}
