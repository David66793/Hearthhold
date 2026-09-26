using Hearthhold.Core;
using UnityEngine;

namespace Hearthhold.UnityClient
{
    // Identity pieces change silhouette and equipment, not just a material tint.
    // Keep each role's geometry here so an upgrade can be reviewed independently.
    public sealed partial class ModelViews
    {
        private void AddBuildingIdentityUpgrade(GameObject root, BuildingKind kind, int level, Bounds b)
        {
            switch (kind)
            {
                case BuildingKind.Keep: UpgradeKeep(root, level, b); break;
                case BuildingKind.Mine: UpgradeMine(root, level, b); break;
                case BuildingKind.Reservoir: UpgradeReservoir(root, level, b); break;
                case BuildingKind.Barracks: UpgradeBarracks(root, level, b); break;
                case BuildingKind.Cannon: UpgradeCannon(root, level, b); break;
                case BuildingKind.Watchtower: UpgradeWatchtower(root, level, b); break;
                case BuildingKind.TrainingCamp: UpgradeTrainingCamp(root, level, b); break;
                case BuildingKind.Laboratory: UpgradeLaboratory(root, level, b); break;
                case BuildingKind.Mortar: UpgradeMortar(root, level, b); break;
                case BuildingKind.AirDefense: UpgradeAirDefense(root, level, b); break;
                case BuildingKind.ArcTower: UpgradeArcTower(root, level, b); break;
                case BuildingKind.BeamTower: UpgradeBeamTower(root, level, b); break;
                case BuildingKind.HeroHall: UpgradeHeroHall(root, level, b); break;
                case BuildingKind.PetLodge: UpgradePetLodge(root, level, b); break;
            }
        }

        private void UpgradeKeep(GameObject r, int level, Bounds b)
        {
            Ornament(r, "Keep signal brazier", PrimitiveType.Cylinder, new Vector3(b.center.x, b.max.y + 0.16f, b.center.z), new Vector3(0.38f, 0.22f, 0.38f), 0x8E5540);
            Ornament(r, "Keep watch flame", PrimitiveType.Sphere, new Vector3(b.center.x, b.max.y + 0.48f, b.center.z), new Vector3(0.23f, 0.42f, 0.23f), 0xF2A84B);
            if (level >= 3)
            {
                Ornament(r, "Keep command pennant mast", PrimitiveType.Cylinder, new Vector3(b.center.x + 0.63f, b.max.y + 0.36f, b.center.z - 0.55f), new Vector3(0.06f, 0.48f, 0.06f), 0xD5B078);
                Ornament(r, "Keep command pennant", PrimitiveType.Cube, new Vector3(b.center.x + 0.81f, b.max.y + 0.70f, b.center.z - 0.55f), new Vector3(0.36f, 0.18f, 0.08f), 0xA9563D);
            }
        }
        private void UpgradeMine(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Mine winch drum", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.15f, z), new Vector3(0.43f, 0.23f, 0.43f), 0x856340, new Vector3(0, 0, 90));
            Ornament(r, "Mine headframe beam", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.37f, z), new Vector3(1.25f, 0.15f, 0.18f), 0x9B7248);
            if (level >= 3) Ornament(r, "Mine ore hopper", PrimitiveType.Cube, new Vector3(x + 0.65f, 0.55f, z - 0.62f), new Vector3(0.62f, 0.70f, 0.65f), 0x4E6571);
        }
        private void UpgradeReservoir(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Reservoir raised siphon", PrimitiveType.Cylinder, new Vector3(x + 0.62f, b.max.y + 0.24f, z), new Vector3(0.14f, 0.55f, 0.14f), 0x7BC8C5);
            Ornament(r, "Reservoir floating collector", PrimitiveType.Sphere, new Vector3(x + 0.62f, b.max.y + 0.85f, z), new Vector3(0.42f, 0.21f, 0.42f), 0x99E1D6);
            if (level >= 3) Ornament(r, "Reservoir condenser arch", PrimitiveType.Cube, new Vector3(x - 0.58f, b.max.y + 0.45f, z), new Vector3(0.18f, 0.90f, 0.75f), 0xC9D9CC);
        }
        private void UpgradeBarracks(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Barracks weapon rack", PrimitiveType.Cube, new Vector3(x + 0.75f, 0.65f, z + 0.82f), new Vector3(0.18f, 1.05f, 0.92f), 0x79523A);
            Ornament(r, "Barracks shield rack", PrimitiveType.Cube, new Vector3(x + 0.90f, 0.72f, z + 0.82f), new Vector3(0.18f, 0.62f, 0.54f), 0xB88853);
            if (level >= 3) Ornament(r, "Barracks mustering horn", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.30f, z), new Vector3(0.17f, 0.48f, 0.17f), 0xD5B078, new Vector3(0, 0, 34));
        }
        private void UpgradeCannon(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Cannon reinforced breech", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.09f, z), new Vector3(0.40f, 0.20f, 0.40f), 0x4F5E67, new Vector3(90, 0, 0));
            Ornament(r, "Cannon recoil rail", PrimitiveType.Cube, new Vector3(x, 0.35f, z + 0.55f), new Vector3(0.70f, 0.18f, 0.95f), 0x9D6C42);
            if (level >= 3) Ornament(r, "Cannon pressure gauge", PrimitiveType.Sphere, new Vector3(x + 0.42f, b.max.y + 0.20f, z), Vector3.one * 0.24f, 0xE7BD66);
        }
        private void UpgradeWatchtower(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Watchtower lookout hood", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.19f, z), new Vector3(0.98f, 0.24f, 0.85f), 0x805F45);
            Ornament(r, "Watchtower range marker", PrimitiveType.Cylinder, new Vector3(x + 0.45f, b.max.y + 0.46f, z), new Vector3(0.10f, 0.30f, 0.10f), 0xDEC07B);
            if (level >= 3) Ornament(r, "Watchtower signal lantern", PrimitiveType.Sphere, new Vector3(x + 0.45f, b.max.y + 0.77f, z), Vector3.one * 0.26f, 0xF2C36E);
        }
        private void UpgradeTrainingCamp(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Training camp practice dummy", PrimitiveType.Cylinder, new Vector3(x + 0.70f, 0.65f, z + 0.62f), new Vector3(0.22f, 0.62f, 0.22f), 0xA47148);
            Ornament(r, "Training camp dummy shield", PrimitiveType.Cube, new Vector3(x + 0.70f, 0.95f, z + 0.87f), new Vector3(0.46f, 0.53f, 0.10f), 0xD4B071);
            if (level >= 3) Ornament(r, "Training camp drill standard", PrimitiveType.Cylinder, new Vector3(x - 0.72f, b.max.y + 0.39f, z), new Vector3(0.08f, 0.78f, 0.08f), 0xD9C69A);
        }
        private void UpgradeLaboratory(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Laboratory glass retort", PrimitiveType.Sphere, new Vector3(x + 0.65f, b.max.y + 0.20f, z), new Vector3(0.39f, 0.47f, 0.39f), 0x83D7C6);
            Ornament(r, "Laboratory condenser", PrimitiveType.Cylinder, new Vector3(x + 0.65f, b.max.y + 0.66f, z), new Vector3(0.10f, 0.35f, 0.10f), 0xD1E8D6);
            if (level >= 3) Ornament(r, "Laboratory lens bridge", PrimitiveType.Cube, new Vector3(x - 0.40f, b.max.y + 0.35f, z), new Vector3(0.85f, 0.13f, 0.16f), 0xB5C4BE);
        }
        private void UpgradeMortar(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Mortar shell rack", PrimitiveType.Cube, new Vector3(x + 0.60f, 0.44f, z + 0.50f), new Vector3(0.56f, 0.18f, 0.57f), 0x6A6050);
            Ornament(r, "Mortar spare shell", PrimitiveType.Sphere, new Vector3(x + 0.60f, 0.65f, z + 0.50f), new Vector3(0.33f, 0.47f, 0.33f), 0x363D42);
            if (level >= 3) Ornament(r, "Mortar range collar", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.08f, z), new Vector3(0.46f, 0.12f, 0.46f), 0xC9A568);
        }
        private void UpgradeAirDefense(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Air defense tracking lens", PrimitiveType.Sphere, new Vector3(x, b.max.y + 0.23f, z), Vector3.one * 0.36f, 0x8CCFDE);
            Ornament(r, "Air defense aiming fork", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.61f, z), new Vector3(0.70f, 0.14f, 0.15f), 0x63717B);
            if (level >= 3) Ornament(r, "Air defense altitude vane", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.90f, z), new Vector3(0.12f, 0.50f, 0.65f), 0xE2C584);
        }
        private void UpgradeArcTower(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Arc tower grounding rod", PrimitiveType.Cylinder, new Vector3(x + 0.57f, b.max.y + 0.30f, z), new Vector3(0.10f, 0.60f, 0.10f), 0x789AA2);
            Ornament(r, "Arc tower lightning fin", PrimitiveType.Cube, new Vector3(x - 0.52f, b.max.y + 0.25f, z), new Vector3(0.18f, 0.70f, 0.43f), 0x75D6E5);
            if (level >= 3) Ornament(r, "Arc tower discharge crown", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.55f, z), new Vector3(0.56f, 0.08f, 0.56f), 0xA5E7ED);
        }
        private void UpgradeBeamTower(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Beam tower focus aperture", PrimitiveType.Cylinder, new Vector3(x, b.max.y + 0.18f, z), new Vector3(0.47f, 0.10f, 0.47f), 0xBDA064);
            Ornament(r, "Beam tower focus core", PrimitiveType.Sphere, new Vector3(x, b.max.y + 0.46f, z), new Vector3(0.25f, 0.40f, 0.25f), 0xFFE5A0);
            if (level >= 3) Ornament(r, "Beam tower sight rail", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.67f, z), new Vector3(0.80f, 0.12f, 0.14f), 0xD4BB86);
        }
        private void UpgradeHeroHall(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Hero hall reliquary", PrimitiveType.Cube, new Vector3(x, b.max.y + 0.20f, z), new Vector3(0.60f, 0.37f, 0.60f), 0xB67B48);
            Ornament(r, "Hero hall oath flame", PrimitiveType.Sphere, new Vector3(x, b.max.y + 0.60f, z), new Vector3(0.23f, 0.38f, 0.23f), 0xF1A247);
            if (level >= 3) Ornament(r, "Hero hall banner mast", PrimitiveType.Cylinder, new Vector3(x + 0.70f, b.max.y + 0.60f, z), new Vector3(0.09f, 0.90f, 0.09f), 0xD8BB82);
        }
        private void UpgradePetLodge(GameObject r, int level, Bounds b)
        {
            float x = b.center.x, z = b.center.z;
            Ornament(r, "Pet lodge feeding trough", PrimitiveType.Cube, new Vector3(x + 0.62f, 0.39f, z + 0.65f), new Vector3(0.84f, 0.25f, 0.40f), 0x8D6445);
            Ornament(r, "Pet lodge paw arch", PrimitiveType.Capsule, new Vector3(x, b.max.y + 0.34f, z), new Vector3(0.25f, 0.42f, 0.25f), 0xD8B57A);
            if (level >= 3) Ornament(r, "Pet lodge climbing perch", PrimitiveType.Cube, new Vector3(x - 0.67f, b.max.y + 0.27f, z), new Vector3(0.65f, 0.20f, 0.60f), 0xA07850);
        }

        private void AddTroopIdentityUpgrade(GameObject r, TroopKind kind, int level, Bounds b)
        {
            if (level < 2) return;
            float y = b.min.y + b.size.y * 0.63f, top = b.max.y;
            switch (kind)
            {
                case TroopKind.Vanguard:
                    Ornament(r, "Vanguard shield ridge", PrimitiveType.Cube, new Vector3(-0.43f, y, 0.32f), new Vector3(0.12f, 0.69f, 0.15f), 0xD6B172);
                    if (level >= 3) Ornament(r, "Vanguard split helm crest", PrimitiveType.Cube, new Vector3(0, top + 0.18f, -0.05f), new Vector3(0.18f, 0.37f, 0.40f), 0xA6503D);
                    break;
                case TroopKind.Ranger:
                    Ornament(r, "Ranger spare quiver", PrimitiveType.Cylinder, new Vector3(0.37f, y, -0.33f), new Vector3(0.16f, 0.35f, 0.16f), 0x765642, new Vector3(0, 0, 18));
                    if (level >= 3) Ornament(r, "Ranger bow sight", PrimitiveType.Sphere, new Vector3(0.49f, y + 0.29f, 0.25f), Vector3.one * 0.14f, 0xB3D890);
                    break;
                case TroopKind.Guardian:
                    Ornament(r, "Guardian shield spine", PrimitiveType.Cube, new Vector3(-0.56f, y, 0.31f), new Vector3(0.17f, 0.91f, 0.19f), 0xC4A36D);
                    if (level >= 3) Ornament(r, "Guardian raised shoulder plate", PrimitiveType.Cube, new Vector3(0.40f, y + 0.30f, 0), new Vector3(0.43f, 0.22f, 0.49f), 0x8A9FA5);
                    break;
                case TroopKind.Sapper:
                    Ornament(r, "Sapper blast harness", PrimitiveType.Cube, new Vector3(0, y, -0.44f), new Vector3(0.82f, 0.20f, 0.18f), 0xA16A45);
                    if (level >= 3) Ornament(r, "Sapper drill nose", PrimitiveType.Capsule, new Vector3(0.55f, y - 0.17f, 0.27f), new Vector3(0.18f, 0.41f, 0.18f), 0xC7A36A, new Vector3(74, 0, 0));
                    break;
                case TroopKind.SkyRider:
                    Ornament(r, "Sky rider wing spar", PrimitiveType.Cube, new Vector3(0, y + 0.16f, -0.39f), new Vector3(1.48f, 0.09f, 0.16f), 0x8ABCC8);
                    if (level >= 3) Ornament(r, "Sky rider tail stabilizer", PrimitiveType.Cube, new Vector3(0, y - 0.14f, -0.72f), new Vector3(0.62f, 0.31f, 0.10f), 0x6ED0E1);
                    break;
                case TroopKind.Alchemist:
                    Ornament(r, "Alchemist second reagent band", PrimitiveType.Cube, new Vector3(0, y + 0.14f, -0.48f), new Vector3(0.86f, 0.12f, 0.20f), 0xD0A064);
                    if (level >= 3) Ornament(r, "Alchemist distillation flask", PrimitiveType.Sphere, new Vector3(0.55f, y + 0.39f, 0.24f), new Vector3(0.28f, 0.36f, 0.28f), 0x58D4B9);
                    break;
                case TroopKind.Medic:
                    Ornament(r, "Medic field kit frame", PrimitiveType.Cube, new Vector3(-0.53f, y - 0.18f, 0.13f), new Vector3(0.43f, 0.12f, 0.46f), 0xD7C694);
                    if (level >= 3) Ornament(r, "Medic twin staff prong", PrimitiveType.Cube, new Vector3(0.65f, top + 0.23f, 0.03f), new Vector3(0.40f, 0.09f, 0.13f), 0x86E4D4);
                    break;
                case TroopKind.Summoner:
                    Ornament(r, "Summoner orbit sigil", PrimitiveType.Cylinder, new Vector3(0.65f, top + 0.20f, -0.06f), new Vector3(0.35f, 0.05f, 0.35f), 0xB48FE5);
                    if (level >= 3) Ornament(r, "Summoner shadow crown", PrimitiveType.Cube, new Vector3(0, top + 0.22f, -0.05f), new Vector3(0.60f, 0.21f, 0.18f), 0x634B81);
                    break;
            }
        }

        private void AddEmberWardenIdentity(GameObject r, int level, Bounds b)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Ornament(r, "Warden split mantle fold", PrimitiveType.Cube,
                    new Vector3(side * 0.27f, b.min.y + b.size.y * 0.49f, -0.28f),
                    new Vector3(0.26f, 0.62f, 0.12f), 0x8F352F, new Vector3(0, 0, side * 10f));
                Ornament(r, "Warden brass mantle clasp", PrimitiveType.Sphere,
                    new Vector3(side * 0.30f, b.min.y + b.size.y * 0.73f, -0.20f),
                    Vector3.one * 0.12f, 0xD0AA68);
            }
            Ornament(r, "Warden furnace chest", PrimitiveType.Cube,
                new Vector3(0, b.min.y + b.size.y * 0.61f, b.max.z + 0.01f),
                new Vector3(0.27f, 0.31f, 0.09f), 0xE57B38);
            if (level >= 2) Ornament(r, "Warden command shoulder", PrimitiveType.Cube,
                new Vector3(0.39f, b.min.y + b.size.y * 0.73f, 0),
                new Vector3(0.30f, 0.15f, 0.37f), 0xB4523E);
            if (level >= 3) Ornament(r, "Warden oath plume", PrimitiveType.Capsule,
                new Vector3(0, b.max.y + 0.20f, -0.13f),
                new Vector3(0.10f, 0.23f, 0.19f), 0xF3B15D, new Vector3(20, 0, 0));
        }

        private void AddPetIdentityUpgrade(GameObject r, PetKind kind, int level, Bounds b)
        {
            if (level < 2) return;
            if (kind == PetKind.CinderFox)
            {
                Ornament(r, "Cinder Fox ember collar", PrimitiveType.Cylinder, new Vector3(0, 0.72f, 0.31f), new Vector3(0.39f, 0.08f, 0.39f), 0xD49B58);
                if (level >= 3) Ornament(r, "Cinder Fox tail ember", PrimitiveType.Sphere, new Vector3(0, 1.07f, -1.67f), new Vector3(0.22f, 0.20f, 0.31f), 0xF5C671);
            }
            else
            {
                Ornament(r, "Mossback reinforced shell ridge", PrimitiveType.Cube, new Vector3(0, 1.12f, -0.16f), new Vector3(0.20f, 0.31f, 0.91f), 0x9CB075);
                if (level >= 3) Ornament(r, "Mossback branch antler", PrimitiveType.Capsule, new Vector3(0, 1.41f, 0.52f), new Vector3(0.11f, 0.38f, 0.11f), 0x827251);
            }
        }

        private GameObject CreateMossbackSculpt(Transform parent)
        {
            GameObject root = new GameObject("pet:Mossback"); root.transform.SetParent(parent, false);
            Transform visual = new GameObject("3D model").transform; visual.SetParent(root.transform, false);
            Transform shell = new GameObject("Mossback shell joint").transform; shell.SetParent(visual, false); shell.localPosition = new Vector3(0, 0.72f, -0.13f);
            SculptPart(shell, "Mossback broad shell", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.97f, 0.64f, 1.21f), 0x56785C);
            SculptPart(shell, "Mossback moss canopy", PrimitiveType.Sphere, new Vector3(0, 0.22f, -0.07f), new Vector3(0.77f, 0.40f, 1.02f), 0x7A9A65);
            for (int side = -1; side <= 1; side += 2)
            {
                SculptPart(shell, "Mossback shell plate", PrimitiveType.Cube, new Vector3(side * 0.47f, 0.20f, -0.10f), new Vector3(0.33f, 0.23f, 0.77f), 0xA1AA72, new Vector3(0, 0, side * -20f));
                Transform leg = new GameObject(side < 0 ? "Mossback left leg joint" : "Mossback right leg joint").transform;
                leg.SetParent(visual, false); leg.localPosition = new Vector3(side * 0.58f, 0.47f, 0.04f);
                SculptPart(leg, "Mossback pillar leg", PrimitiveType.Capsule, new Vector3(0, -0.20f, 0), new Vector3(0.29f, 0.39f, 0.35f), 0x485F49);
                SculptPart(leg, "Mossback wide foot", PrimitiveType.Sphere, new Vector3(0, -0.42f, 0.22f), new Vector3(0.38f, 0.15f, 0.48f), 0x354C3C);
            }
            Transform head = new GameObject("Mossback head joint").transform; head.SetParent(visual, false); head.localPosition = new Vector3(0, 0.90f, 0.76f);
            SculptPart(head, "Mossback square muzzle", PrimitiveType.Cube, new Vector3(0, -0.08f, 0.20f), new Vector3(0.62f, 0.48f, 0.73f), 0x6E8662);
            for (int side = -1; side <= 1; side += 2)
                SculptPart(head, "Mossback amber eye", PrimitiveType.Sphere, new Vector3(side * 0.28f, 0.12f, 0.53f), Vector3.one * 0.09f, 0xD7BD78);
            Debug.Log("HEARTHHOLD_ART_PROTOTYPE_MOSSBACK_3D: shell, head and pillar legs");
            return root;
        }
    }

    internal sealed class MossbackArtAnimator : MonoBehaviour
    {
        private Transform shell, head, leftLeg, rightLeg;
        private bool moving, dying;
        private float phase, strike, death;
        private void Awake()
        {
            Transform visual = transform.Find("3D model");
            shell = visual.Find("Mossback shell joint"); head = visual.Find("Mossback head joint");
            leftLeg = visual.Find("Mossback left leg joint"); rightLeg = visual.Find("Mossback right leg joint");
        }
        public void SetMoving(bool value) { moving = value; }
        public void Attack() { strike = 0.45f; }
        public void Hit() { strike = Mathf.Max(strike, 0.16f); }
        public void Die() { dying = true; }
        private void Update()
        {
            if (shell == null || head == null) return;
            if (dying)
            {
                death += Time.deltaTime;
                shell.localPosition = new Vector3(0, Mathf.Max(0.34f, 0.72f - death * 0.35f), -0.13f);
                if (death > 0.9f) gameObject.SetActive(false);
                return;
            }
            phase += Time.deltaTime * (moving ? 6.5f : 1.8f);
            strike = Mathf.Max(0, strike - Time.deltaTime);
            float shove = strike > 0 ? Mathf.Sin((1f - strike / 0.45f) * Mathf.PI) : 0;
            shell.localPosition = new Vector3(0, 0.72f + Mathf.Sin(phase * 2f) * 0.025f, -0.13f + shove * 0.14f);
            shell.localRotation = Quaternion.Euler(-shove * 9f, 0, 0);
            head.localRotation = Quaternion.Euler(shove * 19f + Mathf.Sin(phase) * 2f, 0, 0);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(moving ? Mathf.Sin(phase) * 17f : 0, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(moving ? -Mathf.Sin(phase) * 17f : 0, 0, 0);
        }
    }
}
