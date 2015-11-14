using UnityEngine;
using System.Collections;

namespace EasyEditor
{
    public enum IngredientUnit { Spoon, Cup, Bowl, Piece }

    // Custom serializable class
    [System.Serializable]
    public class Ingredient
    {
        public string name;
        public int amount = 1;
        public IngredientUnit unit;
    }

    public class PropertyDrawerExample : MonoBehaviour
    {
        [Inspector(group = "Potion Bag")]
        [Popup("Wizard Potion", "Warrior Potion", "Priest Potion")]
        public string potionType;
        [Visibility("potionType", "Wizard Potion")]
        [Popup(1, 2, 3)]
        public int wizardLevel;
        public Ingredient potionResult;
        public Ingredient[] potionIngredients;
    }
}