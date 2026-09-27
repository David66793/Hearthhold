using System.Collections.Generic;
using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    // Equipped items are built from actual 3D parts, mounted on the hero rig.
    // This receives the village loadout in previews and the battle snapshot in combat.
    public sealed partial class ModelViews
    {
        private bool AddHeroEquipmentArt(GameObject root, HeroKind hero, IList<int> slots, IList<int> levels, Bounds body)
        {
            if (slots == null || levels == null) return false;
            bool hammerEquipped = false;
            Transform torchFlame = null, chaliceEmber = null;
            int start = (int)hero * 2;
            for (int slot = 0; slot < 2; slot++)
            {
                int index = start + slot < slots.Count ? slots[start + slot] : -1;
                if (index < 0 || index >= Rules.EquipmentNames.Length || index >= levels.Count || levels[index] <= 0) continue;
                EquipmentKind kind = (EquipmentKind)index;
                if (kind == EquipmentKind.RiftHammer) hammerEquipped = true;
                int level = Mathf.Clamp(levels[index], 1, 3);
                Transform mount = new GameObject("Equipped " + kind).transform;
                mount.SetParent(root.transform.Find("3D model"), false);
                Transform hammerGrip = kind == EquipmentKind.RiftHammer ? FindEquipmentBone(root, "hand.r") : null;
                if (hammerGrip == null && kind == EquipmentKind.RiftHammer) hammerGrip = FindEquipmentBone(root, "lowerarm.r");
                if (hammerGrip == null && kind == EquipmentKind.RiftHammer) hammerGrip = FindEquipmentBone(root, "upperarm.r");
                switch (kind)
                {
                    case EquipmentKind.HearthShield: BuildHearthShield(root, mount, body, level); break;
                    case EquipmentKind.RiftHammer: BuildRiftHammer(root, mount, body, level, hammerGrip); break;
                    case EquipmentKind.MarchTorch: torchFlame = BuildMarchTorch(root, mount, body, level); break;
                    case EquipmentKind.EmberChalice: chaliceEmber = BuildEmberChalice(root, mount, body, level); break;
                }
                if (kind == EquipmentKind.RiftHammer && hammerGrip != null) mount.SetParent(hammerGrip, true);
                else AttachEquipmentMount(root, mount, kind == EquipmentKind.HearthShield ? "upperarm.l" : "spine");
            }
            if (torchFlame != null || chaliceEmber != null)
                root.AddComponent<HeroEquipmentMotion>().Configure(torchFlame, chaliceEmber);
            return hammerEquipped;
        }

        private static void AttachEquipmentMount(GameObject root, Transform mount, string boneSuffix)
        {
            Transform bone = FindEquipmentBone(root, boneSuffix);
            if (bone != null) mount.SetParent(bone, true);
        }

        private static Transform FindEquipmentBone(GameObject root, string boneSuffix)
        {
            Transform visual = root.transform.Find("3D model");
            if (visual == null) return null;
            foreach (Transform bone in visual.GetComponentsInChildren<Transform>())
                if (bone.name.EndsWith(boneSuffix, System.StringComparison.OrdinalIgnoreCase)) return bone;
            return null;
        }

        private Transform GearPart(GameObject root, Transform mount, string name, PrimitiveType shape,
            Vector3 position, Vector3 size, int color, Vector3? rotation = null)
        {
            Ornament(root, name, shape, position, size, color, rotation ?? Vector3.zero);
            Transform part = root.transform.Find("3D model").Find(name);
            part.SetParent(mount, true);
            return part;
        }

        private void BuildHearthShield(GameObject root, Transform mount, Bounds b, int level)
        {
            float y = b.min.y + b.size.y * 0.56f;
            GearPart(root, mount, "Shield forged rim", PrimitiveType.Capsule,
                new Vector3(-0.63f, y, 0.30f), new Vector3(0.34f, 0.31f, 0.10f), 0xA46E45);
            GearPart(root, mount, "Shield inlaid face", PrimitiveType.Capsule,
                new Vector3(-0.63f, y, 0.37f), new Vector3(0.28f, 0.27f, 0.08f), 0x465C65);
            GearPart(root, mount, "Shield hearth boss", PrimitiveType.Sphere,
                new Vector3(-0.63f, y, 0.46f), new Vector3(0.13f, 0.14f, 0.09f), 0xE3A456);
            if (level >= 2) GearPart(root, mount, "Equipped HearthShield tier 2", PrimitiveType.Cube,
                new Vector3(-0.63f, y + 0.15f, 0.47f), new Vector3(0.07f, 0.21f, 0.05f), 0xD7BC86);
            if (level >= 3) GearPart(root, mount, "Equipped HearthShield tier 3", PrimitiveType.Sphere,
                new Vector3(-0.63f, y - 0.16f, 0.48f), Vector3.one * 0.09f, 0xF3C67B);
        }

        private void BuildRiftHammer(GameObject root, Transform mount, Bounds b, int level, Transform gripBone)
        {
            // Place the haft through the actual right-hand bone before parenting the whole forged assembly.
            // The forearm/upper-arm fallbacks still swing with the attack on alternate KayKit rigs.
            Vector3 grip = gripBone != null ? root.transform.InverseTransformPoint(gripBone.position)
                : new Vector3(0.46f, b.min.y + b.size.y * 0.47f, 0.30f);
            float x = grip.x, y = grip.y, z = grip.z + 0.06f;
            GearPart(root, mount, "Rift hammer diagonal haft", PrimitiveType.Cylinder,
                new Vector3(x, y + 0.24f, z), new Vector3(0.065f, 0.48f, 0.065f), 0x78533B, new Vector3(0, 0, -10f));
            GearPart(root, mount, "Rift hammer asymmetric head", PrimitiveType.Cube,
                new Vector3(x + 0.06f, y + 0.68f, z), new Vector3(0.55f, 0.23f, 0.29f), 0x58666A, new Vector3(0, 0, -10f));
            GearPart(root, mount, "Rift hammer splitting beak", PrimitiveType.Capsule,
                new Vector3(x + 0.34f, y + 0.72f, z), new Vector3(0.21f, 0.13f, 0.18f), 0xC3A574, new Vector3(0, 0, 80f));
            if (level >= 2) GearPart(root, mount, "Equipped RiftHammer tier 2", PrimitiveType.Cube,
                new Vector3(x + 0.04f, y + 0.69f, z + 0.17f), new Vector3(0.34f, 0.06f, 0.05f), 0xE4B66E);
            if (level >= 3) GearPart(root, mount, "Equipped RiftHammer tier 3", PrimitiveType.Sphere,
                new Vector3(x + 0.06f, y + 0.69f, z + 0.28f), Vector3.one * 0.14f, 0xF0C986);
        }

        private Transform BuildMarchTorch(GameObject root, Transform mount, Bounds b, int level)
        {
            float y = b.min.y + b.size.y * 0.66f;
            GearPart(root, mount, "March torch iron handle", PrimitiveType.Cylinder,
                new Vector3(-0.48f, y - 0.08f, -0.47f), new Vector3(0.05f, 0.23f, 0.05f), 0x5B6060);
            GearPart(root, mount, "March torch open brazier", PrimitiveType.Cylinder,
                new Vector3(-0.48f, y + 0.19f, -0.47f), new Vector3(0.16f, 0.08f, 0.16f), 0xBA8046);
            Transform flame = GearPart(root, mount, "March torch living flame", PrimitiveType.Capsule,
                new Vector3(-0.48f, y + 0.40f, -0.47f), new Vector3(0.12f, 0.16f, 0.12f), 0xF1A250);
            if (level >= 2) GearPart(root, mount, "Equipped MarchTorch tier 2", PrimitiveType.Sphere,
                new Vector3(-0.48f, y + 0.29f, -0.47f), Vector3.one * 0.12f, 0xF5CB78);
            if (level >= 3) GearPart(root, mount, "Equipped MarchTorch tier 3", PrimitiveType.Capsule,
                new Vector3(-0.33f, y + 0.39f, -0.47f), new Vector3(0.07f, 0.10f, 0.07f), 0xFFD994);
            Light light = flame.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.range = 1.7f; light.intensity = 0.42f; light.shadows = LightShadows.None;
            light.color = new Color32(255, 179, 96, 255);
            return flame;
        }

        private Transform BuildEmberChalice(GameObject root, Transform mount, Bounds b, int level)
        {
            float y = b.min.y + b.size.y * 0.42f;
            GearPart(root, mount, "Ember chalice fitted cup", PrimitiveType.Cylinder,
                new Vector3(0.51f, y, 0.30f), new Vector3(0.19f, 0.13f, 0.19f), 0xD5B071);
            GearPart(root, mount, "Ember chalice narrow stem", PrimitiveType.Cylinder,
                new Vector3(0.51f, y - 0.17f, 0.30f), new Vector3(0.055f, 0.13f, 0.055f), 0x9D724C);
            Transform ember = GearPart(root, mount, "Ember chalice floating ember", PrimitiveType.Sphere,
                new Vector3(0.51f, y + 0.23f, 0.30f), Vector3.one * 0.17f, 0x95D8CA);
            if (level >= 2) GearPart(root, mount, "Equipped EmberChalice tier 2", PrimitiveType.Capsule,
                new Vector3(0.72f, y + 0.02f, 0.30f), new Vector3(0.10f, 0.15f, 0.10f), 0xD5B071, new Vector3(0, 0, 30f));
            if (level >= 3) GearPart(root, mount, "Equipped EmberChalice tier 3", PrimitiveType.Sphere,
                new Vector3(0.51f, y + 0.39f, 0.30f), Vector3.one * 0.09f, 0xDFF8DD);
            return ember;
        }
    }

    internal sealed class HeroEquipmentMotion : MonoBehaviour
    {
        private Transform torchFlame, chaliceEmber;
        private Vector3 torchRest, chaliceRest;
        private float chaliceHeight;

        internal void Configure(Transform torch, Transform chalice)
        {
            torchFlame = torch; chaliceEmber = chalice;
            if (torch != null) torchRest = torch.localScale;
            if (chalice != null) { chaliceRest = chalice.localScale; chaliceHeight = chalice.localPosition.y; }
        }

        private void Update()
        {
            if (torchFlame != null)
                torchFlame.localScale = torchRest * (1f + Mathf.Sin(Time.time * 8.4f) * 0.08f);
            if (chaliceEmber != null)
            {
                chaliceEmber.localScale = chaliceRest * (1f + Mathf.Sin(Time.time * 3.6f) * 0.08f);
                Vector3 position = chaliceEmber.localPosition;
                position.y = chaliceHeight + Mathf.Sin(Time.time * 2.4f) * 0.035f;
                chaliceEmber.localPosition = position;
            }
        }
    }
}
