using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TreeEditor;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VidTools.Vis;
using static System.Math;

public class Fluid : MonoBehaviour
{
    public new Camera camera;
    public Vector2 gravity = new Vector2(0, 0), startPos;
    private Vector2[] pos, predictedPos, velocity;
    private Particle[] cells;
    private int[] adress;
    private Vector2Int gridSize;
    public float[] densities;
    private Vector2 boundingBox = new Vector2(16,16), boundingBoxPos = Vector2.zero;
    public int count, particleTracker;
    public float damping = 0F, spacing, influenceRadius = 0.5F, mass = 1, targetDensity, pressureMultiplier, viscosityMultiplier, interactionMultiplier, interactionRadius, cellSize = 1;

    public Vector2 BoundingBox {
        get { return boundingBox; }
        set {
            boundingBox = value;
            ResetGrid();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AddParticles(count, spacing, startPos);
        ResetGrid();
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < pos.Count(); i++) {
            predictedPos[i] = pos[i] + velocity[i] * 1F/30F;
            predictedPos[i].x = Clamp(predictedPos[i].x, boundingBoxPos.x - (BoundingBox.x / 2), boundingBoxPos.x + (BoundingBox.x / 2));
            predictedPos[i].y = Clamp(predictedPos[i].y, boundingBoxPos.y - (BoundingBox.y / 2), boundingBoxPos.y + (BoundingBox.y / 2));
        }

        // sort particles into cells
        for(int i = 0; i < pos.Count(); i++) {
            cells[i].cellId = Util.CellToId(WorldToGrid(predictedPos[cells[i].particleId]), gridSize);
        }

        Array.Sort(cells);

        for(int i = 0; i < adress.Count(); i++) {
            adress[i] = -1;
        }

        for(int i = cells.Count() - 1; i >= 0; i--){
            adress[cells[i].cellId] = i;
        }




        for(int i = 0; i < pos.Count(); i++) {
            densities[i] = ClaculateDensity(predictedPos[i]);
        }

        for(int i = 0; i < cells.Count(); i++) {
            int id = cells[i].particleId;
            Vector2 pressure = CalculatePressureCells(i);
            Vector2 pressureAcceleration = pressure / densities[id];
            Vector2 viscosity = CalculateViscosityCells(i);
            Vector2 viscosityForce = viscosity / densities[id];
            Vector2 interactionForce = Vector2.zero;
            if(Input.GetMouseButton(0)) {
                interactionForce = InteractionForce(camera.ScreenToWorldPoint(Input.mousePosition), interactionRadius, interactionMultiplier, id);
            }
            if(Input.GetMouseButton(1)) {
                interactionForce = InteractionForce(camera.ScreenToWorldPoint(Input.mousePosition), interactionRadius, -interactionMultiplier, id);
                
            }

            velocity[id] += (pressureAcceleration + viscosityForce + gravity + interactionForce) * Time.deltaTime;
            velocity[id] *= 1 - damping;
            pos[id] += velocity[id] * Time.deltaTime;

            if(Abs(pos[id].x - boundingBoxPos.x) > BoundingBox.x / 2) {
                pos[id].x = BoundingBox.x / 2 * Sign(pos[id].x) + boundingBoxPos.x;
                velocity[id].x *= -1;
            }
            if(Abs(pos[id].y - boundingBoxPos.y) > BoundingBox.y / 2) {
                pos[id].y = BoundingBox.y / 2 * Sign(pos[id].y) + boundingBoxPos.y;
                velocity[id].y *= -1;
            }

            Draw.BoxOutline(boundingBoxPos, BoundingBox, 0.05F, Color.white, 1);
            Draw.Point(pos[id], 0.05F, Color.blue);
        }
    }

    void AddParticles(int count, float spacing, Vector2 position) {
        int dimension = (int)Ceiling(Sqrt(count));

        velocity = new Vector2[count];
        pos = new Vector2[count];
        predictedPos = new Vector2[count];
        densities = new float[count];
        cells = new Particle[count];

        for(int i = 0; i < count; i++ ) {
            velocity[i] = Vector2.zero;
            cells[i] = new Particle(i);
            pos[i] = new Vector2(i % dimension * spacing - ((float)(dimension - 1) / 2 * spacing) + position.x, (i - i % dimension) / dimension * spacing - ((float)(dimension - 1) / 2 * spacing) + position.y);
        }
    }

    float SmoothingKernel(float distance, float radius) {
        if (distance >= radius) return 0;

        return (float)Pow(radius - distance, 3) / ((float)PI * (float)Pow(radius, 5) / 10);
    }

    float SmoothingKernelDerivative(float distance, float radius) {
        if(distance >= radius) return 0;

        return (float)(-30 * Pow(radius - distance, 2) / (Pow(radius, 5) * (float)PI));
    }

    float ViscosityKernel(float distance, float radius) {
        if (distance >= radius) return 0;

        return (float)Pow(Pow(radius, 2) - Pow(distance, 2), 3) / ((float)PI * (float)Pow(radius, 8) / 4);
    }

    float ViscosityKernelDerivative(float distance, float radius) {
        if(distance >= radius) return 0;

        return (float)(24 * distance * Pow(Pow(radius, 2) - Pow(distance, 2), 2) / ((float)PI * Pow(radius, 8)));
    }

    float ClaculateDensity(Vector2 position) {
        float density = 0;

        LinkedList<int> relevantCells = RelevantCells(Util.CellToId(WorldToGrid(position), gridSize));

        foreach(int cellId in relevantCells){
            if (adress[cellId] == -1) continue;
            particleTracker = adress[cellId];
            while(cells.Count() > particleTracker && cells[particleTracker].cellId == cellId){
                density += mass * SmoothingKernel((position - predictedPos[cells[particleTracker].particleId]).magnitude, influenceRadius);

                particleTracker++;
            }
        }    
        return density;
    }

    Vector2 CalculatePressureCells(int id) {
        Vector2 pressure = Vector2.zero;

        if(densities[id] == 0) return pressure;

        LinkedList<int> relevantCells = RelevantCells(cells[id].cellId);
        
        foreach(int cellId in relevantCells){
            if (adress[cellId] == -1) continue;
            particleTracker = adress[cellId];
            while(cells.Count() > particleTracker && cells[particleTracker].cellId == cellId){
                if(particleTracker == id) {particleTracker++; continue;}
                float distance = (predictedPos[cells[id].particleId] - predictedPos[cells[particleTracker].particleId]).magnitude;
                if(distance >= influenceRadius) {particleTracker++; continue;}

                float slope = SmoothingKernelDerivative(distance, influenceRadius);
                Vector2 direction = (predictedPos[cells[id].particleId] - predictedPos[cells[particleTracker].particleId]) / distance;
                if(distance == 0) direction = RandomDirection();
                float sharedPressure = (DensityToPressure(densities[cells[id].particleId]) + DensityToPressure(densities[cells[particleTracker].particleId])) / 2;
                pressure += direction * sharedPressure * slope * mass / densities[cells[particleTracker].particleId];

                particleTracker++;
            }
        }
        return pressure;
    }

    Vector2 CalculatePressure(int id) {
        Vector2 pressure = Vector2.zero;

        if(densities[id] == 0) return pressure;

        for(int i = 0; i < pos.Count(); i ++){
            if(id == i) continue;
            float distance = (predictedPos[id] - predictedPos[i]).magnitude;
            if(distance >= influenceRadius) continue;

            float slope = SmoothingKernelDerivative(distance, influenceRadius);
            Vector2 direction = (predictedPos[id] - predictedPos[i]) / distance;
            if(distance == 0) direction = RandomDirection();
            float sharedPressure = (DensityToPressure(densities[id]) + DensityToPressure(densities[i])) / 2;
            pressure += direction * sharedPressure * slope * mass / densities[i];
        }
        return pressure;
    }

    float DensityToPressure(float density) {
        return (targetDensity - density) * pressureMultiplier;
    }

    Vector2 CalculateViscosityCells(int id) {
        Vector2 viscosity = Vector2.zero;
        
        LinkedList<int> relevantCells = RelevantCells(cells[id].cellId);

        foreach(int cellId in relevantCells){
            if (adress[cellId] == -1) continue;
            particleTracker = adress[cellId];
            while(cells.Count() > particleTracker && cells[particleTracker].cellId == cellId){
                if(particleTracker == id) {particleTracker++; continue;}
                float distance = (predictedPos[cells[id].particleId] - predictedPos[cells[particleTracker].particleId]).magnitude;
                
                float influence = ViscosityKernel(distance, influenceRadius);
                viscosity += (velocity[cells[particleTracker].particleId] - velocity[cells[id].particleId]) * influence * viscosityMultiplier;

                particleTracker++;
            }
        }
        return viscosity;
    }

    Vector2 CalculateViscosity(int id) {
        Vector2 viscosity = Vector2.zero;

        for(int i = 0; i < pos.Count(); i ++){
            if(id == i) continue;
            float distance = (predictedPos[id] - predictedPos[i]).magnitude;
            
            float influence = ViscosityKernel(distance, influenceRadius);
            viscosity += (velocity[i] - velocity[id]) * influence * viscosityMultiplier;
        }
        return viscosity;
    }

    Vector2 InteractionForce(Vector2 position, float radius, float strength, int id) {
        Vector2 force = Vector2.zero;
        float distance = (predictedPos[id] - position).magnitude;

        if(distance <= radius) {
            Vector2 direction = (position - predictedPos[id]).normalized;

            force = direction * strength;
        }
        return force;
    }

    Vector2 RandomDirection() {
        float angle = UnityEngine.Random.Range(0,360);
        return new Vector2((float)Cos(angle), (float)Sin(angle));
    }

    Vector2Int WorldToGrid(Vector2 pos){
        return new Vector2Int(
            (int)Floor((pos.x - boundingBoxPos.x + (BoundingBox.x / 2)) / cellSize),
            (int)Floor((pos.y - boundingBoxPos.y + (BoundingBox.x / 2)) / cellSize)
        );
    }

    LinkedList<int> RelevantCells(int cell){
        LinkedList<int> result = new LinkedList<int>();
        Vector2Int cellPos = Util.IdToCell(cell, gridSize);

        for(int x = -1; x <= 1; x++){
            for(int y = -1; y <= 1; y++){
                Vector2Int neighbourPos = new Vector2Int(x, y) + cellPos;
                if(Util.CellInBounds(neighbourPos, gridSize)){
                    result.AddFirst(Util.CellToId(neighbourPos, gridSize));
                }
            }
        }

        return result;
    }

    private void ResetGrid() {
        cellSize = influenceRadius * 2;
        gridSize = new Vector2Int((int)Ceiling(BoundingBox.x / cellSize), (int)Ceiling(BoundingBox.y / cellSize));
        adress = new int[gridSize.x * gridSize.y];
    }
}
