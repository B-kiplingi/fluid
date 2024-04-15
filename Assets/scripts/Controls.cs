using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controls : MonoBehaviour
{
    public Fluid fluid;
    public Slider gravityX, gravityY, pressureMultiplier, viscositiMultiplier, influenceRadius, targetDensity, mass, interactionForce, interactionRadius;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGravityXChange() {
        fluid.gravity.x = gravityX.value;
    }

    public void OnGravityYChange() {
        fluid.gravity.y = gravityY.value;
    }

    public void OnPressureMultiplierChange() {
        fluid.pressureMultiplier = pressureMultiplier.value;
    }

    public void OnViscoisitiMultiplierChange() {
        fluid.viscosityMultiplier = viscositiMultiplier.value;
    }

    public void OnInfluenceRadiusChange() {
        fluid.influenceRadius = influenceRadius.value;
    }

    public void OnTargetDensityChange() {
        fluid.targetDensity = targetDensity.value;
    }

    public void OnMassChange() {
        fluid.mass = mass.value;
    }

    public void OnInteractionForceChange() {
        fluid.interactionMultiplier = interactionForce.value;
    }

    public void OnInteractionRadiusChange() {
        fluid.interactionRadius = interactionRadius.value;
    }
}
