using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Controls : MonoBehaviour
{
    public Fluid fluid;
    public void OnPressureMultiplierChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.PressureMultiplier = float.Parse(text);
    }

    public void OnViscoisitiMultiplierChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.ViscosityMultiplier = float.Parse(text);
    }

    public void OnTargetDensityChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.TargetDensity = float.Parse(text);
    }

    public void OnInfluenceRadiusChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.InfluenceRadius = float.Parse(text);
    }

    public void OnMassChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.Mass = float.Parse(text);
    }

    public void OnInteractionForceChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.InteractionMultiplier = float.Parse(text);
    }

    public void OnInteractionRadiusChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.InteractionRadius = float.Parse(text);
    }

    public void OnGravityXChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.gravity.x = float.Parse(text);
    }

    public void OnGravityYChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.gravity.y = float.Parse(text);
    }

    public void OnBoundingBoxSizeXChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.BoundingBox = new Vector2(float.Parse(text), fluid.BoundingBox.y);
    }

    public void OnBoundingBoxSizeYChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.BoundingBox = new Vector2(fluid.BoundingBox.x, float.Parse(text));
    }

    public void OnBoundingBoxPosXChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.BoundingBoxPos = new Vector2(float.Parse(text), fluid.BoundingBoxPos.y);
    }

    public void OnBoundingBoxPosYChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.BoundingBoxPos = new Vector2(fluid.BoundingBoxPos.x, float.Parse(text));
    }

    public void OnParticleCountChange(string text) {
        if(!string.IsNullOrWhiteSpace(text))
            fluid.Count = int.Parse(text);
    }

    public void OnResetPressed() {
        fluid.Reset();
    }
}
