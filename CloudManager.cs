using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class CloudManager : Node3D
{
    public class CloudInstance
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float TargetScale;
        public float CurrentScale;
        public float Age;
        public float Lifetime;
        public Vector3[] SphereOffsets;
        public int StartIndex;
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

        public MultiMeshInstance3D MMInstance;
        public List<CloudInstance> Clouds = new();
        public Queue<int> FreeIndices = new();

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
            float cloudArea = Mathf.Pi * (1.8f * CloudScale) * (1.2f * CloudScale);
            float skyArea = Mathf.Pi * radius * radius;
            TargetCount = Mathf.RoundToInt((Coverage * skyArea) / cloudArea);
            SpawnProbability = (float)TargetCount / (secondsAlive * 60f);
        }
    }

    [Export] public CloudLayer[] CloudLayers = [new(40f, 0.2f, 0.01f, 5f), new(60f, 0.15f, 0.004f, 8f)];
    [Export] public Material CloudMaterial;

    private RandomNumberGenerator _rng = new();
    private const float Radius = 450f;
    private const int SecondsAlive = 25;
    private const int SpheresPerCloud = 20;

    public override void _Ready()
    {
        _rng.Randomize();
        foreach (var layer in CloudLayers)
        {
            layer.WindDirection = new Vector3(_rng.Randf() * 2f - 1f, 0f, _rng.Randf() * 2f - 1f).Normalized();
            layer.CalculateStats(Radius, SecondsAlive);

            var mm = new MultiMesh
            {
                Mesh = new SphereMesh
                {
                    Material = CloudMaterial,
                    RadialSegments = 8, // shadows
                    Rings = 6 // believe it or not, also shadows
                },
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount = (layer.TargetCount + 20) * SpheresPerCloud
            };

            layer.MMInstance = new MultiMeshInstance3D { Multimesh = mm };
            AddChild(layer.MMInstance);

            for (int i = 0; i < mm.InstanceCount; i += SpheresPerCloud)
            {
                layer.FreeIndices.Enqueue(i);
                for (int j = 0; j < SpheresPerCloud; j++)
                    mm.SetInstanceTransform(i + j, new Transform3D(Basis.Identity.Scaled(Vector3.Zero), Vector3.Zero));
            }

            for (int i = 0; i < layer.TargetCount; i++)
            {
                var pos = GetRandomPointInRadius(Radius);
                pos.Y = layer.Height;
                SpawnCloudAt(layer, pos, _rng.Randf() * SecondsAlive);
            }
        }
    }

    private Vector3 GetRandomPointInRadius(float radius)
    {
        float r = radius * Mathf.Sqrt(_rng.Randf());
        float theta = _rng.Randf() * Mathf.Tau;
        return new Vector3(r * Mathf.Cos(theta), 0, r * Mathf.Sin(theta));
    }

    private void SpawnCloudAt(CloudLayer layer, Vector3 position, float initialAge = 0)
    {
        if (layer.FreeIndices.Count == 0) return;

        int startIndex = layer.FreeIndices.Dequeue();
        var cloud = new CloudInstance
        {
            Position = position,
            Velocity = layer.WindDirection * layer.WindSpeed * layer.CloudScale,
            TargetScale = layer.CloudScale,
            CurrentScale = 0,
            Age = initialAge,
            Lifetime = SecondsAlive,
            StartIndex = startIndex,
            SphereOffsets = new Vector3[SpheresPerCloud]
        };

        for (int i = 0; i < SpheresPerCloud; i++)
        {
            Vector3 p;
            do
            {
                p = new Vector3(_rng.Randf() * 2f - 1f, _rng.Randf() * 2f - 1f, _rng.Randf() * 2f - 1f);
            } while (p.LengthSquared() > 1f);

            p.X *= 1.8f;
            p.Y *= 0.6f;
            p.Z *= 1.2f;
            cloud.SphereOffsets[i] = p;
        }

        layer.Clouds.Add(cloud);
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;
        foreach (var layer in CloudLayers)
        {
            var mm = layer.MMInstance.Multimesh;

            for (int i = layer.Clouds.Count - 1; i >= 0; i--)
            {
                var cloud = layer.Clouds[i];
                cloud.Age += fDelta;
                cloud.Position += cloud.Velocity;

                // Handle scaling
                if (cloud.Age < 1.0f) cloud.CurrentScale = Mathf.Lerp(0, cloud.TargetScale, cloud.Age);
                else if (cloud.Age > cloud.Lifetime - 1.0f)
                    cloud.CurrentScale = Mathf.Lerp(cloud.TargetScale, 0, cloud.Age - (cloud.Lifetime - 1.0f));
                else cloud.CurrentScale = cloud.TargetScale;

                // Update transforms
                for (int j = 0; j < SpheresPerCloud; j++)
                {
                    var basis = Basis.Identity.Scaled(new Vector3(cloud.CurrentScale, cloud.CurrentScale,
                        cloud.CurrentScale));
                    var transform =
                        new Transform3D(basis, cloud.Position + cloud.SphereOffsets[j] * cloud.CurrentScale);
                    mm.SetInstanceTransform(cloud.StartIndex + j, transform);
                }

                if (cloud.Age >= cloud.Lifetime)
                {
                    // Hide dead cloud
                    for (int j = 0; j < SpheresPerCloud; j++)
                        mm.SetInstanceTransform(cloud.StartIndex + j,
                            new Transform3D(Basis.Identity.Scaled(Vector3.Zero), Vector3.Zero));

                    layer.FreeIndices.Enqueue(cloud.StartIndex);
                    layer.Clouds.RemoveAt(i);
                }
            }

            // Continuous spawning
            if (_rng.Randf() < layer.SpawnProbability)
            {
                var pos = GetRandomPointInRadius(Radius);
                pos.Y = layer.Height;
                SpawnCloudAt(layer, pos);
            }
        }
    }
}