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
    public class NestedClassesEnnemy : MonoBehaviour
    {

        [System.Serializable]
        public class Weapon
        {
            [BeginHorizontal]
            public string name = "";
            [EndHorizontal]
            public float strength = 0f;
        }

        [System.Serializable]
        public class Bag
        {
            [Range(1, 10)]
            public int weight;

            [Inspector(rendererType = "InlineClassRenderer")]
            public Weapon mainWeapon;

            public List<Weapon> otherWeapons;
        }

        [Inspector(rendererType = "InlineClassRenderer")]
        public Bag mainBag;

        public Bag[] otherBagsList;
    }
}
