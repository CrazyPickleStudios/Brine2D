using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text;

namespace Brine2D.Systems.Rendering
{
    internal class ParticleEmitterState
    {
        public bool IsEmitting;
        public float EmissionRate;
        public int MaxParticles;
        public float ParticleLifetime;
        public float LifetimeVariation;
        public Color StartColor;
        public Color EndColor;
        public float StartSize;
        public float EndSize;
        public Vector2 InitialVelocity;
        public float VelocitySpread;
        public float SpeedVariation;
        public Vector2 Gravity;
        public Vector2 SpawnOffset;
        public float SpawnRadius;
        public ITexture? ParticleTexture;
        public AtlasRegion? ParticleAtlasRegion;
        public float InitialRotation;
        public float InitialRotationVariation;
        public float RotationSpeed;
        public float RotationSpeedVariation;
        public bool EnableTrails;
        public int TrailLength;
        public float TrailStartAlpha;
        public float TrailEndAlpha;
        public BlendMode BlendMode;
        public EmitterShape Shape;
        public Vector2 ShapeSize;
        public float ConeAngle;
    }
}
