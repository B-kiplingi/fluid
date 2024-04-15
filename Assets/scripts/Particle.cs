using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Particle : IComparable<Particle>, IEquatable<Particle>
{
    public int particleId;
    public int cellId;

    public Particle(int particleId){
        this.particleId = particleId;
    }

    public int CompareTo(Particle other)
    {
        return cellId - other.cellId;
    }

    public bool Equals(Particle other)
    {
        return this.cellId == other.cellId;
    }
}
