using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CrucibleLab/CelestialBody", fileName = "NewCelestialBody")]
public class BodyData : ScriptableObject
{
    public string bodyName;
    public Sprite bodySprite;
    public List<ElementRatio> composition = new();
    public float temperature; // Kelvin
    public float radius;
    public float mass; // In terms of Earth Mass (1)

    [System.Serializable]
    public struct ElementRatio
    {
        public ElementData element;
        [Range(0f, 1f)]
        public float ratio;
    }
}
