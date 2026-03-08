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

        public Cloud(int lifeTime, Vector3 step, Vector3 spawnPosition, float cloudScale, RandomNumberGenerator rng,
            Material material)
        {
            _lifeTime = lifeTime;
            this._step = step;
            // var mesh = new BoxMesh();
            // Mesh = mesh;
            Position = spawnPosition;
            _rng = rng;
            generateMesh(material, cloudScale, 20);
        }

        private void generateMesh(Material material, float cloudScale, int instanceCount)
        {
            Multimesh = new MultiMesh();
            Multimesh.SetMesh(new SphereMesh() { Material = material });
            Multimesh.SetTransformFormat(MultiMesh.TransformFormatEnum.Transform3D);
            Multimesh.SetInstanceCount(instanceCount);
            Multimesh.SetVisibleInstanceCount(instanceCount);
            for (int i = 0; i < Multimesh.VisibleInstanceCount; i++)
            {
                var offset = GetCloudOffset(i, cloudScale) / cloudScale;
                var basis = Basis.Identity;
                var transform = new Transform3D(basis, Position + offset);
                transform = transform.Scaled(new Vector3(cloudScale, cloudScale, cloudScale));

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
            if (_lifeTime == 0) QueueFree();
            _lifeTime--;
        }
    }

    public partial class CloudLayer : Resource
    {
        [Export] public float Height;
        [Export] public float Frequency;
        [Export] public float WindSpeed;
        [Export] public float CloudScale;

        public CloudLayer(float height, float frequency, float windSpeed, float cloudScale = 1f)
        {
            this.Height = height;
            this.Frequency = Mathf.Pow(frequency, 6f);
            this.WindSpeed = windSpeed;
            this.CloudScale = cloudScale;
        }

        public CloudLayer()
        {
        }
    }

    [Export] public CloudLayer[] CloudLayers = [new(10f, 0.5f, 0.1f, 2f), new(15f, 0.5f, 0.08f, 4f)];
    public Vector3 SpawnPosition;
    private RandomNumberGenerator _rng = new();
    [Export] public Material CloudMaterial;

    public override void _Ready()
    {
        _rng.Randomize();
        SpawnPosition = Position;
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var cloudLayer in CloudLayers)
        {
            var spawn = _rng.Randf() < cloudLayer.Frequency;
            if (spawn)
            {
                var windDirection = new Vector3(_rng.Randf() * 2f - 1f, 0f, _rng.Randf() * 2f - 1f).Normalized();
                AddChild(new Cloud(5 * 60, cloudLayer.WindSpeed * windDirection, new Vector3(1, cloudLayer.Height, 0),
                    cloudLayer.CloudScale, _rng, CloudMaterial));
            }
        }
    }
}