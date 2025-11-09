using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolController : MonoBehaviour
{
    public List<ElementData> elements;
    public List<PoolPartical> particals;
    public ElementData selection;
    public float massPerSecond;
    public float crucibleMass;
    private float timeSinceLast;

    private enum eInd : int{
        H, He, Fe, O, S, Si, Mg, Ne, C, N, NH3, NH4, CO, CO2, H2O, CH3OH, CH4
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //massPerSecond;
        selection = elements[(int)eInd.C];
        timeSinceLast = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLast += Time.deltaTime;
        if(Input.GetMouseButtonDown(0)) {
            if (timeSinceLast > 0.125f) {

            } 
        }
    }
}
