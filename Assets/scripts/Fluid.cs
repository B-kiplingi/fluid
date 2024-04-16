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
    public Vector2 gravity = new(0, 0), startPos;
    private Vector2[] pos, predictedPos, velocity;
    private Particle[] cells;
    private int[] adress;
    private Vector2Int gridSize;
    private float[] densities;
    private Vector2 boundingBox = new(16,16), boundingBoxPos = Vector2.zero;
    private int count = 400;
    private int particleTracker;
    public float spacing;
    private float cellSize, influenceRadius = 1.4F, damping, mass = 1, targetDensity = 3;
    private bool resetGrid = false;
    private float pressureMultiplier = 100;
    private float viscosityMultiplier = 3;
    private float interactionMultiplier = 20;
    private float interactionRadius = 3;

    public Vector2 BoundingBox {
        get { return boundingBox; }
        set {
            if(value.x > 0 && value.y > 0) {
                boundingBox = value;
                resetGrid = true;
                FixOutOfBounds();
            }
        }
    }

    public Vector2 BoundingBoxPos {
        get { return boundingBoxPos; }
        set {
            boundingBoxPos = value;
            resetGrid = true;
            FixOutOfBounds();
        }
    }

    public float InfluenceRadius {
        get { return influenceRadius; }
        set {
            if(value > 0) {
                influenceRadius = value;
                resetGrid = true;
            }
        }
    }

    public float Damping { get => damping; set => damping = Clamp(value, 0, 1); }

    public float Mass { get => mass; set { if(value > 0) mass = value;}}

    public float TargetDensity { get => targetDensity; set { if(value > 0) targetDensity = value;}}
    public float PressureMultiplier { get => pressureMultiplier; set => pressureMultiplier = value; }
    public float ViscosityMultiplier { get => viscosityMultiplier; set => viscosityMultiplier = value; }
    public float InteractionMultiplier { get => interactionMultiplier; set => interactionMultiplier = value; }
    public float InteractionRadius { get => interactionRadius; set => interactionRadius = value; }
    public int Count { get => count; set => count = Clamp(value, 0, 10000); }

    // Start is called before the first frame update
    void Start()
    {
        AddParticles(Count, spacing, startPos);
        ResetGrid();
    }

    // Update is called once per frame
    void Update()
    {
        if(resetGrid) {
            ResetGrid(); 
            resetGrid = false;
        }
        for(int i = 0; i < pos.Count(); i++) {
            predictedPos[i] = pos[i] + velocity[i] * 1F/30F;
            predictedPos[i].x = Clamp(predictedPos[i].x, BoundingBoxPos.x - (BoundingBox.x / 2), BoundingBoxPos.x + (BoundingBox.x / 2));
            predictedPos[i].y = Clamp(predictedPos[i].y, BoundingBoxPos.y - (BoundingBox.y / 2), BoundingBoxPos.y + (BoundingBox.y / 2));
        }

        // sort particles into cells
        for(int i = 0; i < pos.Count(); i++) {
            cells[i].cellId = Util.CellToId(WorldToGrid(predictedPos[cells[i].particleId]), gridSize);
            if(!Util.CellInBounds(WorldToGrid(predictedPos[cells[i].particleId]), gridSize)){
                Debug.Log("cell out of bounds");
            }
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
                interactionForce = InteractionForce(camera.ScreenToWorldPoint(Input.mousePosition), InteractionRadius, InteractionMultiplier, id);
            }
            if(Input.GetMouseButton(1)) {
                interactionForce = InteractionForce(camera.ScreenToWorldPoint(Input.mousePosition), InteractionRadius, -InteractionMultiplier, id);
                
            }

            velocity[id] += (pressureAcceleration + viscosityForce + gravity + interactionForce) * Time.deltaTime;
            velocity[id] *= 1 - Damping;
            pos[id] += velocity[id] * Time.deltaTime;
        }

        FixOutOfBounds();

        for(int i = 0; i < cells.Count(); i++) {
            Draw.BoxOutline(BoundingBoxPos, BoundingBox, 0.05F, Color.white, 1);
            Draw.Point(pos[i], 0.05F, Color.blue);
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
                density += Mass * SmoothingKernel((position - predictedPos[cells[particleTracker].particleId]).magnitude, InfluenceRadius);

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
                if(distance >= InfluenceRadius) {particleTracker++; continue;}

                float slope = SmoothingKernelDerivative(distance, InfluenceRadius);
                Vector2 direction = (predictedPos[cells[id].particleId] - predictedPos[cells[particleTracker].particleId]) / distance;
                if(distance == 0) direction = RandomDirection();
                float sharedPressure = (DensityToPressure(densities[cells[id].particleId]) + DensityToPressure(densities[cells[particleTracker].particleId])) / 2;
                pressure += direction * sharedPressure * slope * Mass / densities[cells[particleTracker].particleId];

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
            if(distance >= InfluenceRadius) continue;

            float slope = SmoothingKernelDerivative(distance, InfluenceRadius);
            Vector2 direction = (predictedPos[id] - predictedPos[i]) / distance;
            if(distance == 0) direction = RandomDirection();
            float sharedPressure = (DensityToPressure(densities[id]) + DensityToPressure(densities[i])) / 2;
            pressure += direction * sharedPressure * slope * Mass / densities[i];
        }
        return pressure;
    }

    float DensityToPressure(float density) {
        return (TargetDensity - density) * PressureMultiplier;
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
                
                float influence = ViscosityKernel(distance, InfluenceRadius);
                viscosity += (velocity[cells[particleTracker].particleId] - velocity[cells[id].particleId]) * influence * ViscosityMultiplier;

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
            
            float influence = ViscosityKernel(distance, InfluenceRadius);
            viscosity += (velocity[i] - velocity[id]) * influence * ViscosityMultiplier;
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
            (int)Floor((pos.x - BoundingBoxPos.x + (BoundingBox.x / 2)) / cellSize),
            (int)Floor((pos.y - BoundingBoxPos.y + (BoundingBox.y / 2)) / cellSize)
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
        cellSize = InfluenceRadius;
        gridSize = new Vector2Int((int)Ceiling(BoundingBox.x / cellSize + 1), (int)Ceiling(BoundingBox.y / cellSize + 1));
        adress = new int[gridSize.x * gridSize.y];
    }

    private void FixOutOfBounds() {
        for(int i = 0; i < pos.Count(); i++) {
            if(Abs(pos[i].x - BoundingBoxPos.x) > BoundingBox.x / 2) {
                pos[i].x = BoundingBox.x / 2 * Sign(pos[i].x - BoundingBoxPos.x) + BoundingBoxPos.x;
                velocity[i].x *= -1;
            }
            if(Abs(pos[i].y - BoundingBoxPos.y) > BoundingBox.y / 2) {
                pos[i].y = BoundingBox.y / 2 * Sign(pos[i].y - BoundingBoxPos.y) + BoundingBoxPos.y;
                velocity[i].y *= -1;
            }
        }
    }

    public void Reset() {
        Start();
    }
}
