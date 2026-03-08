using System;
using Godot;

[GlobalClass]
public partial class CloudManager : Node3D
{
    public partial class Cloud : MultiMeshInstance3D
    {
        private int _lifeTime;
        private Vector3 _step;
        private RandomNumberGenerator _rng;
        private bool _isDespawning = false;
        private float _targetScale;
        private const int FadeFrames = 60; // 60fps (tick) -> 1s

        public Cloud(int lifeTime, Vector3 step, Vector3 spawnPosition, float cloudScale, RandomNumberGenerator rng,
            Material material)
        {
            _lifeTime = lifeTime;
            this._step = step * cloudScale;
            Position = spawnPosition;
            _targetScale = cloudScale;
            _rng = rng;
            GenerateMesh(material, 40);
        }

        public override void _Ready()
        {
            Scale = Vector3.Zero;
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector3(_targetScale, _targetScale, _targetScale), 1.0)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        private void GenerateMesh(Material material, int instanceCount)
        {
            Multimesh = new MultiMesh();
            Multimesh.SetMesh(new SphereMesh() { Material = material });
            Multimesh.SetTransformFormat(MultiMesh.TransformFormatEnum.Transform3D);
            Multimesh.SetInstanceCount(instanceCount);
            Multimesh.SetVisibleInstanceCount(instanceCount);
            for (int i = 0; i < Multimesh.VisibleInstanceCount; i++)
            {
                var offset = GetCloudOffset(i, 1.0f);
                var transform = new Transform3D(Basis.Identity, offset);
                Multimesh.SetInstanceTransform(i, transform);
            }
        }

        private Vector3 GetCloudOffset(int index, float scale)
        {
            Vector3 p;
            do
            {
                p = new Vector3(
                    _rng.Randf() * 2f - 1f,
                    _rng.Randf() * 2f - 1f,
                    _rng.Randf() * 2f - 1f
                );
            } while (p.LengthSquared() > 1f);

            p.X *= scale * 1.8f; // wider
            p.Y *= scale * 0.6f; // flatter
            p.Z *= scale * 1.2f; // slightly deep

            return p;
        }

        public Cloud()
        {
        }

        public override void _PhysicsProcess(double delta)
        {
            Position += _step;
            _lifeTime--;

            if (_lifeTime <= FadeFrames && !_isDespawning)
            {
                _isDespawning = true;
                var tween = CreateTween();
                tween.TweenProperty(this, "scale", Vector3.Zero, 1.0)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.In);
                tween.Finished += QueueFree;
            }
        }
    }

    public partial class CloudLayer : Resource
    {
        [Export] public float Height;
        [Export] public float Coverage;
        [Export] public float WindSpeed;
        [Export] public float CloudScale;
        public Vector3 WindDirection; 

        public int TargetCount;
        public float SpawnProbability;

        public CloudLayer(float height, float coverage, float windSpeed, float cloudScale = 1f)
        {
            this.Height = height;
            this.Coverage = coverage;
            this.WindSpeed = windSpeed;
            this.CloudScale = cloudScale;
        }

        public CloudLayer()
        {
        }

        public void CalculateStats(float radius, float secondsAlive)
        {
            // estimate the area of 1 cloud cluster. 
            // cloud offset logic
            float cloudArea = Mathf.Pi * (1.8f * CloudScale) * (1.2f * CloudScale);
            float skyArea = Mathf.Pi * radius * radius;

            // #clouds -> cover the sky
            TargetCount = Mathf.RoundToInt((Coverage * skyArea) / cloudArea);
            // maintain cover after dissipation
            SpawnProbability = (float)TargetCount / (secondsAlive * 60f);
        }
    }

    [Export] public CloudLayer[] CloudLayers = [new(40f, 0.2f, 0.01f, 5f), new(60f, 0f, 0.004f, 8f)];
    public Vector3 SpawnPosition;
    private RandomNumberGenerator _rng = new();
    [Export] public Material CloudMaterial;
    private const float Radius = 450f;
    private const int SecondsAlive = 25;

    public override void _Ready()
    {
        _rng.Randomize();
        SpawnPosition = Position;
        foreach (var cloudLayer in CloudLayers)
        {
            cloudLayer.WindDirection = new Vector3(_rng.Randf() * 2f - 1f, 0f, _rng.Randf() * 2f - 1f).Normalized();
            cloudLayer.CalculateStats(Radius, SecondsAlive);

            // Fill the sky initially
            for (int i = 0; i < cloudLayer.TargetCount; i++)
            {
                var pos = GetRandomPointInRadius(Radius);
                pos.Y = cloudLayer.Height;
                SpawnCloudAt(cloudLayer, pos);
            }
        }
    }

    private Vector3 GetRandomPointInRadius(float radius)
    {
        float r = radius * Mathf.Sqrt(_rng.Randf());
        float theta = _rng.Randf() * Mathf.Tau;
        return new Vector3(r * Mathf.Cos(theta), 0, r * Mathf.Sin(theta));
    }

    public void SpawnCloud(CloudLayer cloudLayer)
    {
        var pos = GetRandomPointInRadius(Radius);
        pos.Y = cloudLayer.Height;
        SpawnCloudAt(cloudLayer, pos);
    }

    private void SpawnCloudAt(CloudLayer cloudLayer, Vector3 position)
    {
        AddChild(new Cloud(SecondsAlive * 60, cloudLayer.WindSpeed * cloudLayer.WindDirection, position,
            cloudLayer.CloudScale,
            _rng, CloudMaterial));
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var cloudLayer in CloudLayers)
        {
            float p = cloudLayer.SpawnProbability;
            // Handle probabilities > 1 by spawning multiple clouds if needed
            while (p > 0)
            {
                if (_rng.Randf() < p) SpawnCloud(cloudLayer);
                p -= 1.0f;
            }
        }
    }
}