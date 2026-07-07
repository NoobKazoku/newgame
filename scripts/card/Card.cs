using System;
using Godot;

namespace GFrameworkGodotTemplate.scripts.card;

public partial class Card : RigidBody3D
{
    private const string FrontMaterialName = "Front_Custom_Image";
    private const string BackMaterialName = "Back_Official";
    private const string EdgeMaterialName = "Card_Edge";
    private const float SourceRadius = 2.0117188f;
    private const float SourceThickness = 0.11694336f;

    [Export] public MeshInstance3D? CardMesh { get; set; }
    [Export] public Node3D? VisualRoot { get; set; }
    [Export] public CollisionShape3D? CollisionShape { get; set; }

    public override void _Ready()
    {
        Mass = 0.05f;
        LinearDamp = 0.4f;
        AngularDamp = 0.8f;
        GravityScale = 1.0f;

        VisualRoot ??= GetNodeOrNull<Node3D>("圆卡");
        CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        CardMesh ??= FindFirstMeshInstance(this);
    }

    public void SetCardTextures(Texture2D front, Texture2D back, Texture2D? edge = null)
    {
        SetSurfaceTextureByMaterialName(FrontMaterialName, front);
        SetSurfaceTextureByMaterialName(BackMaterialName, back);

        if (edge == null)
            SetSurfaceMaterialByMaterialName(EdgeMaterialName, CreateEdgeMaterial());
        else
            SetSurfaceTextureByMaterialName(EdgeMaterialName, edge);
    }

    public void SetCardSize(float radius, float thickness)
    {
        if (VisualRoot != null)
            VisualRoot.Scale = new Vector3(radius / SourceRadius, thickness / SourceThickness, radius / SourceRadius);

        if (CollisionShape?.Shape is CylinderShape3D cylinder)
        {
            cylinder.Radius = radius;
            cylinder.Height = thickness;
        }
    }

    public void Launch(Vector3 impulse, Vector3 torque)
    {
        Sleeping = false;
        ApplyCentralImpulse(impulse);
        ApplyTorqueImpulse(torque);
    }

    public void ResetMotion(Transform3D transform)
    {
        GlobalTransform = transform;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Sleeping = false;
    }

    private void SetSurfaceTextureByMaterialName(string materialName, Texture2D texture)
    {
        var material = new StandardMaterial3D
        {
            AlbedoTexture = texture,
            Roughness = 0.65f
        };

        SetSurfaceMaterialByMaterialName(materialName, material);
    }

    private void SetSurfaceMaterialByMaterialName(string materialName, Material material)
    {
        if (CardMesh?.Mesh == null) return;

        for (var i = 0; i < CardMesh.Mesh.GetSurfaceCount(); i++)
        {
            var sourceMaterial = CardMesh.Mesh.SurfaceGetMaterial(i);
            if (!IsMaterialMatch(sourceMaterial, materialName)) continue;

            CardMesh.SetSurfaceOverrideMaterial(i, material);
            return;
        }
    }

    private static bool IsMaterialMatch(Material? material, string materialName)
    {
        if (material == null) return false;

        return string.Equals(material.ResourceName, materialName, StringComparison.Ordinal) ||
               string.Equals(material.ResourceName.Trim(), materialName, StringComparison.Ordinal) ||
               material.ResourcePath.Contains(materialName, StringComparison.Ordinal);
    }

    private static StandardMaterial3D CreateEdgeMaterial()
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.78f, 0.66f, 0.46f),
            Roughness = 0.85f
        };
    }

    private static MeshInstance3D? FindFirstMeshInstance(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D meshInstance && meshInstance.Mesh != null)
                return meshInstance;

            var nested = FindFirstMeshInstance(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
