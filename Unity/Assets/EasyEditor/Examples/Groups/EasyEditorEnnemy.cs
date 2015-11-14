//
// Copyright (c) 2016 Easy Editor 
// All Rights Reserved 
//  
//
 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EasyEditor
{
    public class EasyEditorEnnemy : MonoBehaviour
    {
    	[Inspector(group = "Game Designer Settings")]
        public Color skinColor;
        public float maxSpeed;
        public float height = 3f;

    	[Inspector(group = "Basic Settings")]
    	[Comment("this option is not optimized for mobiles.")]
        public bool usePhysic = true;
        public Vector3 initialPosition;
    	
    	[Inspector(group = "Advanced Settings", groupDescription = "These settings can only be" +
            " tuned by a programmer. Do not change any of these settings.")]
    	[Comment("Target cannot exceed a number of 10.")]
        public List<Bounds> listOfTarget;
        public List<Collider> BodyColliders;
        
    	[EETooltip("Trigger the fury state")]
    	[Comment("The animation plays only in Play mode.")]
    	[Inspector(group = "Game Designer Settings", order = 1)]
        public void GetIntoFuryState()
        {
            Debug.Log("Here starts the fury state !!!");
            GetComponent<Animation>().Play();
        }
    }
}