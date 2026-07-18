using System.Collections.Generic;
using UnityEngine;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.UI
{
    /// <summary>
    /// 업그레이드 아이콘 절차 생성기 — 아트 에셋 없이 Id별 고유 글리프를 SDF로 래스터라이즈한다.
    /// SfxRecipes(절차 합성 SFX)와 같은 철학: 코드가 곧 원본이라 에셋 파이프라인이 필요 없다.
    /// UpgradeData.Icon이 지정되어 있으면 그쪽을 우선 사용한다 (추후 아트 교체 대비).
    /// </summary>
    public static class UpgradeIconFactory
    {
        public const int Size = 96;

        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite GetIcon(UpgradeData upgrade)
        {
            return upgrade.Icon != null ? upgrade.Icon : GetGenerated(upgrade.Id, upgrade.Category);
        }

        public static Sprite GetGenerated(string id, UpgradeCategory category)
        {
            string key = id ?? string.Empty;
            // 도메인 리로드 비활성 시 파괴된 스프라이트가 캐시에 남을 수 있어 유효성도 검사한다.
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = Rasterize(key, category, Size);
            texture.name = $"UpgradeIcon_{key}";
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            Cache[key] = sprite;
            return sprite;
        }

        // ---------- 래스터라이즈 ----------

        private static Texture2D Rasterize(string id, UpgradeCategory category, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            Color accent = AccentOf(category);
            Color glyphColor = Color.Lerp(accent, Color.white, 0.6f);
            var tileFill = new Color(0.07f, 0.09f, 0.13f, 0.92f);

            var pixels = new Color32[size * size];
            float aa = 2.5f / size; // 정규화 좌표계에서 ~1.25px 안티앨리어싱 폭

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(
                        (x + 0.5f) / size * 2f - 1f,
                        (y + 0.5f) / size * 2f - 1f);

                    // 배경 타일 (라운드 사각) + 카테고리색 테두리
                    float dTile = RoundedBox(p, new Vector2(0.92f, 0.92f), 0.2f);
                    float tileMask = AaMask(dTile, aa);
                    float dBorder = Mathf.Abs(dTile + 0.05f) - 0.05f;
                    float borderMask = AaMask(dBorder, aa);

                    // 글리프 — 타일 안쪽에 맞게 78%로 축소 평가
                    const float glyphScale = 0.78f;
                    float dGlyph = GlyphSdf(id, p / glyphScale) * glyphScale;
                    float glyphMask = AaMask(dGlyph, aa);

                    var c = new Color(tileFill.r, tileFill.g, tileFill.b, tileFill.a * tileMask);
                    c = Color.Lerp(c, accent, borderMask * 0.9f);
                    c.a = Mathf.Max(c.a, borderMask * tileMask);
                    c = Color.Lerp(c, glyphColor, glyphMask);
                    c.a = Mathf.Max(c.a, glyphMask);

                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        private static Color AccentOf(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Parts => new Color(1f, 0.55f, 0.3f),
            UpgradeCategory.Armor => new Color(0.4f, 0.75f, 1f),
            UpgradeCategory.Energy => new Color(1f, 0.85f, 0.35f),
            _ => Color.white,
        };

        private static float AaMask(float d, float aa)
        {
            return Mathf.Clamp01(0.5f - d / aa);
        }

        // ---------- 글리프 정의 (좌표계: [-1,1], y 위쪽) ----------

        private static float GlyphSdf(string id, Vector2 p)
        {
            switch (id)
            {
                case "part_gatling": return Gatling(p);
                case "part_missile_pod": return Missile(p);
                case "part_beam": return Beam(p);
                case "part_gravity_well": return GravityWell(p);
                case "part_laser_cannon": return LaserCannon(p);
                case "part_shotgun_cannon": return Shotgun(p);
                case "part_cluster_bomb": return ClusterBomb(p);
                case "part_railgun": return Railgun(p);
                case "part_orbital_strike": return OrbitalStrike(p);
                case "part_emp_field": return EmpField(p);
                case "part_slot_expansion": return SlotExpansion(p);
                case "support_drones": return SupportDrones(p);
                case "armor_heavy": return HeavyArmor(p);
                case "armor_composite": return CompositeArmor(p);
                case "energy_reactor": return Reactor(p);
                case "energy_missile_loader": return MissileLoader(p);
                case "energy_thrusters": return Thrusters(p);
                case "energy_landing_gear": return LandingGear(p);
                case "result_heated_gatling": return HeatedGatling(p);
                case "result_overload_shield": return OverloadShield(p);
                case "result_multi_lock": return MultiLock(p);
                default: return Fallback(p);
            }
        }

        private static float Gatling(Vector2 p)
        {
            float d = Box(p, new Vector2(-0.3f, 0.08f), new Vector2(0.09f, 0.4f));
            d = Mathf.Min(d, Box(p, new Vector2(0f, 0.08f), new Vector2(0.09f, 0.5f)));
            d = Mathf.Min(d, Box(p, new Vector2(0.3f, 0.08f), new Vector2(0.09f, 0.4f)));
            d = Mathf.Min(d, Box(p, new Vector2(0f, -0.44f), new Vector2(0.5f, 0.12f)));
            return d;
        }

        private static float Missile(Vector2 p)
        {
            float d = Box(p, new Vector2(0f, -0.05f), new Vector2(0.14f, 0.32f));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.14f, 0.27f), new Vector2(0.14f, 0.27f), new Vector2(0f, 0.6f)));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.14f, -0.37f), new Vector2(-0.14f, -0.05f), new Vector2(-0.38f, -0.5f)));
            d = Mathf.Min(d, Tri(p, new Vector2(0.14f, -0.37f), new Vector2(0.14f, -0.05f), new Vector2(0.38f, -0.5f)));
            return d;
        }

        private static float Beam(Vector2 p)
        {
            float d = Box(p, new Vector2(-0.42f, 0f), new Vector2(0.16f, 0.26f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.2f, 0f), new Vector2(0.55f, 0f), 0.09f));
            d = Mathf.Min(d, Circle(p, new Vector2(0.55f, 0f), 0.14f));
            return d;
        }

        private static float GravityWell(Vector2 p)
        {
            float d = Circle(p, Vector2.zero, 0.1f);
            d = Mathf.Min(d, Ring(p, Vector2.zero, 0.32f, 0.05f));
            d = Mathf.Min(d, Ring(p, Vector2.zero, 0.56f, 0.05f));
            return d;
        }

        private static float LaserCannon(Vector2 p)
        {
            float d = Circle(p, new Vector2(-0.4f, -0.4f), 0.18f);
            d = Mathf.Min(d, Segment(p, new Vector2(-0.3f, -0.3f), new Vector2(0.52f, 0.52f), 0.08f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.38f, 0.68f), new Vector2(0.68f, 0.38f), 0.04f));
            return d;
        }

        private static float Shotgun(Vector2 p)
        {
            var origin = new Vector2(0f, -0.5f);
            float d = Segment(p, origin, new Vector2(-0.42f, 0.25f), 0.055f);
            d = Mathf.Min(d, Segment(p, origin, new Vector2(0f, 0.38f), 0.055f));
            d = Mathf.Min(d, Segment(p, origin, new Vector2(0.42f, 0.25f), 0.055f));
            d = Mathf.Min(d, Circle(p, new Vector2(-0.5f, 0.42f), 0.07f));
            d = Mathf.Min(d, Circle(p, new Vector2(0f, 0.56f), 0.07f));
            d = Mathf.Min(d, Circle(p, new Vector2(0.5f, 0.42f), 0.07f));
            return d;
        }

        private static float ClusterBomb(Vector2 p)
        {
            float d = Circle(p, new Vector2(0f, -0.12f), 0.3f);
            d = Mathf.Min(d, Segment(p, new Vector2(0f, 0.18f), new Vector2(0.18f, 0.42f), 0.05f));
            d = Mathf.Min(d, Circle(p, new Vector2(-0.45f, 0.35f), 0.08f));
            d = Mathf.Min(d, Circle(p, new Vector2(0.45f, 0.3f), 0.08f));
            d = Mathf.Min(d, Circle(p, new Vector2(0.05f, 0.58f), 0.08f));
            return d;
        }

        private static float Railgun(Vector2 p)
        {
            float d = Box(p, new Vector2(0f, 0.18f), new Vector2(0.55f, 0.07f));
            d = Mathf.Min(d, Box(p, new Vector2(0f, -0.18f), new Vector2(0.55f, 0.07f)));
            d = Mathf.Min(d, Circle(p, new Vector2(0.3f, 0f), 0.11f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.45f, 0f), new Vector2(0.05f, 0f), 0.03f));
            return d;
        }

        private static float OrbitalStrike(Vector2 p)
        {
            float d = Segment(p, new Vector2(0f, 0.6f), new Vector2(0f, 0f), 0.09f);
            d = Mathf.Min(d, Tri(p, new Vector2(-0.28f, 0f), new Vector2(0.28f, 0f), new Vector2(0f, -0.32f)));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.32f, -0.36f), new Vector2(-0.14f, -0.48f), 0.04f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.32f, -0.36f), new Vector2(0.14f, -0.48f), 0.04f));
            return d;
        }

        private static float EmpField(Vector2 p)
        {
            float d = Ring(p, Vector2.zero, 0.55f, 0.05f);
            d = Mathf.Min(d, Segment(p, new Vector2(0.12f, 0.42f), new Vector2(-0.15f, 0.05f), 0.06f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.15f, 0.05f), new Vector2(0.12f, 0.02f), 0.06f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.12f, 0.02f), new Vector2(-0.1f, -0.42f), 0.06f));
            return d;
        }

        private static float SlotExpansion(Vector2 p)
        {
            float d = Box(p, new Vector2(-0.28f, 0.28f), new Vector2(0.18f, 0.18f));
            d = Mathf.Min(d, Box(p, new Vector2(0.28f, 0.28f), new Vector2(0.18f, 0.18f)));
            d = Mathf.Min(d, Box(p, new Vector2(-0.28f, -0.28f), new Vector2(0.18f, 0.18f)));
            d = Mathf.Min(d, Segment(p, new Vector2(0.28f, -0.44f), new Vector2(0.28f, -0.12f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.12f, -0.28f), new Vector2(0.44f, -0.28f), 0.05f));
            return d;
        }

        private static float SupportDrones(Vector2 p)
        {
            float d = Tri(p, new Vector2(0f, 0.55f), new Vector2(-0.16f, 0.2f), new Vector2(0.16f, 0.2f));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.35f, -0.05f), new Vector2(-0.51f, -0.4f), new Vector2(-0.19f, -0.4f)));
            d = Mathf.Min(d, Tri(p, new Vector2(0.35f, -0.05f), new Vector2(0.19f, -0.4f), new Vector2(0.51f, -0.4f)));
            return d;
        }

        private static float ShieldShape(Vector2 p)
        {
            float d = Box(p, new Vector2(0f, 0.15f), new Vector2(0.4f, 0.32f));
            return Mathf.Min(d, Tri(p, new Vector2(-0.4f, -0.17f), new Vector2(0.4f, -0.17f), new Vector2(0f, -0.55f)));
        }

        private static float HeavyArmor(Vector2 p)
        {
            return ShieldShape(p);
        }

        private static float CompositeArmor(Vector2 p)
        {
            float d = Mathf.Abs(ShieldShape(p)) - 0.05f;
            d = Mathf.Min(d, Segment(p, new Vector2(-0.26f, 0.18f), new Vector2(0.26f, 0.18f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.24f, -0.04f), new Vector2(0.24f, -0.04f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.16f, -0.26f), new Vector2(0.16f, -0.26f), 0.045f));
            return d;
        }

        private static float Reactor(Vector2 p)
        {
            float d = Circle(p, Vector2.zero, 0.14f);
            d = Mathf.Min(d, Ring(p, Vector2.zero, 0.42f, 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.16f, 0.16f), new Vector2(0.44f, 0.44f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.16f, 0.16f), new Vector2(-0.44f, 0.44f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.16f, -0.16f), new Vector2(-0.44f, -0.44f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.16f, -0.16f), new Vector2(0.44f, -0.44f), 0.05f));
            return d;
        }

        private static float MissileLoader(Vector2 p)
        {
            float d = Box(p, new Vector2(-0.28f, -0.02f), new Vector2(0.11f, 0.26f));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.39f, 0.24f), new Vector2(-0.17f, 0.24f), new Vector2(-0.28f, 0.5f)));
            d = Mathf.Min(d, Segment(p, new Vector2(0.1f, 0.05f), new Vector2(0.3f, 0.25f), 0.055f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.3f, 0.25f), new Vector2(0.5f, 0.05f), 0.055f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.1f, -0.3f), new Vector2(0.3f, -0.1f), 0.055f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.3f, -0.1f), new Vector2(0.5f, -0.3f), 0.055f));
            return d;
        }

        private static float Thrusters(Vector2 p)
        {
            float d = Box(p, new Vector2(0f, 0.32f), new Vector2(0.3f, 0.18f));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.3f, 0.14f), new Vector2(0.3f, 0.14f), new Vector2(0f, -0.2f)));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.2f, -0.1f), new Vector2(-0.28f, -0.45f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(0f, -0.16f), new Vector2(0f, -0.55f), 0.05f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.2f, -0.1f), new Vector2(0.28f, -0.45f), 0.05f));
            return d;
        }

        private static float LandingGear(Vector2 p)
        {
            float d = Segment(p, new Vector2(0f, 0.5f), new Vector2(0f, -0.25f), 0.07f);
            d = Mathf.Min(d, Box(p, new Vector2(0f, -0.35f), new Vector2(0.34f, 0.07f)));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.45f, -0.55f), new Vector2(-0.2f, -0.55f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.2f, -0.55f), new Vector2(0.45f, -0.55f), 0.045f));
            return d;
        }

        private static float HeatedGatling(Vector2 p)
        {
            float d = Box(p, new Vector2(-0.22f, -0.2f), new Vector2(0.08f, 0.3f));
            d = Mathf.Min(d, Box(p, new Vector2(0f, -0.2f), new Vector2(0.08f, 0.36f)));
            d = Mathf.Min(d, Box(p, new Vector2(0.22f, -0.2f), new Vector2(0.08f, 0.3f)));
            d = Mathf.Min(d, Tri(p, new Vector2(-0.18f, 0.24f), new Vector2(0.18f, 0.24f), new Vector2(0f, 0.6f)));
            return d;
        }

        private static float OverloadShield(Vector2 p)
        {
            float d = Mathf.Abs(ShieldShape(p)) - 0.05f;
            d = Mathf.Min(d, Segment(p, new Vector2(0.1f, 0.32f), new Vector2(-0.12f, 0.02f), 0.055f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.12f, 0.02f), new Vector2(0.1f, -0.02f), 0.055f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.1f, -0.02f), new Vector2(-0.08f, -0.34f), 0.055f));
            return d;
        }

        private static float MultiLock(Vector2 p)
        {
            float d = Ring(p, Vector2.zero, 0.3f, 0.05f);
            d = Mathf.Min(d, Circle(p, Vector2.zero, 0.07f));
            d = Mathf.Min(d, Segment(p, new Vector2(0f, 0.38f), new Vector2(0f, 0.55f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(0f, -0.38f), new Vector2(0f, -0.55f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.38f, 0f), new Vector2(0.55f, 0f), 0.045f));
            d = Mathf.Min(d, Segment(p, new Vector2(-0.38f, 0f), new Vector2(-0.55f, 0f), 0.045f));
            d = Mathf.Min(d, Circle(p, new Vector2(0.48f, 0.48f), 0.07f));
            d = Mathf.Min(d, Circle(p, new Vector2(-0.48f, -0.48f), 0.07f));
            return d;
        }

        private static float Fallback(Vector2 p)
        {
            float d = Ring(p, Vector2.zero, 0.4f, 0.06f);
            return Mathf.Min(d, Circle(p, Vector2.zero, 0.12f));
        }

        // ---------- SDF 프리미티브 ----------

        private static float Circle(Vector2 p, Vector2 center, float radius)
        {
            return (p - center).magnitude - radius;
        }

        private static float Ring(Vector2 p, Vector2 center, float radius, float thickness)
        {
            return Mathf.Abs((p - center).magnitude - radius) - thickness;
        }

        private static float Box(Vector2 p, Vector2 center, Vector2 half)
        {
            var d = new Vector2(Mathf.Abs(p.x - center.x) - half.x, Mathf.Abs(p.y - center.y) - half.y);
            Vector2 outside = Vector2.Max(d, Vector2.zero);
            return outside.magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        private static float RoundedBox(Vector2 p, Vector2 half, float radius)
        {
            return Box(p, Vector2.zero, half - new Vector2(radius, radius)) - radius;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - thickness;
        }

        private static float Tri(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(
                new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }
    }
}
