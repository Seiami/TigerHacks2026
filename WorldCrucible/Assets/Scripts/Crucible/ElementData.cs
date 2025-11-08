using UnityEngine;

[CreateAssetMenu(fileName = "ElementData", menuName = "CrucibleLab/Element")]
public class ElementData : ScriptableObject {
    public string elementName;
    public string atomicSymbol;
    public float molarMass; // maybe use as weight in combinations
    public Color color;
    public Sprite icon;
}
