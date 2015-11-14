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
    public class CustomEditorEnnemy : MonoBehaviour
    {
    	[Inspector(group = "Game Designer Settings")]
        public Color skinColor;
        public float maxSpeed;
        public float height = 3f;

    	[Inspector(group = "Basic Settings")]
        public bool usePhysic = true;
        public Vector3 initialPosition;

    	[Inspector(group = "Advanced Settings")]
        public List<Bounds> listOfTarget;
        public List<Collider> BodyColliders;
        
    	[Inspector(group = "Game Designer Settings", order = 1)]
        public void GetIntoFuryState()
        {
            Debug.Log("Here starts the fury state !!!");
            GetComponent<Animation>().Play();
        }
    }
}