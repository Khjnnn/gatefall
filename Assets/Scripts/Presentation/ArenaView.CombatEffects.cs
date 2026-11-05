using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class ArenaView
    {
        private Material defaultEffectMaterial, enemyFlashMaterial, silhouetteMaterial;
        private readonly int[] shotEffectCounts = new int[TowerCatalog.All.Length], hitEffectCounts = new int[TowerCatalog.All.Length];
        private int wardBursts, tetherLines, markFollowUps;
        public int EffectRecycleCount { get; private set; }
        public string CombatEffectCensus { get { return "shots=" + string.Join(",", shotEffectCounts) + " hits=" + string.Join(",", hitEffectCounts) + " recycled=" + EffectRecycleCount + " wardBursts=" + wardBursts + " tetherLines=" + tetherLines + " markFollowUps=" + markFollowUps; } }

        private void InitializeCombatEffects()
        {
            defaultEffectMaterial = new Material(Shader.Find("Sprites/Default")); owned.Add(defaultEffectMaterial);
            Shader flash = Shader.Find("Gatefall/CombatFlash");
            if (flash == null) { Debug.LogError("Gatefall CombatFlash shader missing"); flash = Shader.Find("Sprites/Default"); }
            enemyFlashMaterial = new Material(flash); enemyFlashMaterial.SetFloat("_Flash", 0); owned.Add(enemyFlashMaterial);
            silhouetteMaterial = new Material(flash); silhouetteMaterial.SetFloat("_Flash", 1); owned.Add(silhouetteMaterial);
        }

        private void CountCombatEffect(EventKind kind, int source)
        {
            if (source < 0 || source >= shotEffectCounts.Length) return;
            if (kind == EventKind.TowerShot) shotEffectCounts[source]++;
            else if (kind == EventKind.Hit) hitEffectCounts[source]++;
        }

        // Reuse our existing alpha masks with a shared white-silhouette shader.
        // This gives each role its own colour without tinting a baked gold texture muddy green.
        private Fx Neutral(string shape, Vector3 p, Vector3 velocity, float life, float from, float to, Color color)
        {
            Fx f = Spawn(shape, p, velocity, life, from, to, color);
            f.Sprite.sharedMaterial = silhouetteMaterial;
            return f;
        }

        private Fx Line(Vector3 a, Vector3 b, float life, float width, Color color)
        {
            Vector3 direction = b - a;
            Fx f = Spawn(null, (a + b) * .5f, Vector3.zero, life, 1, .78f, color);
            f.Sprite.sprite = square;
            f.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            f.Stretch = new Vector2(direction.magnitude, width);
            f.Sprite.transform.localScale = new Vector3(f.Stretch.x, f.Stretch.y, 1);
            f.FadePower = 1;
            return f;
        }

        private void Pulse(Vector3 p, float diameter, float life, Color color, bool ground)
        {
            float scale = diameter / art.Sprite("fx-ring").bounds.size.x;
            Fx f = Neutral("fx-ring", p, Vector3.zero, life, scale * .25f, scale, color);
            f.Stretch = new Vector2(1, ground ? .55f : 1);
        }

        // A new reaction must reach its first displayed frame before it decays.
        private static float DecayAfterEventFrame(float strength, int eventFrame, float decay)
        {
            return eventFrame == Time.frameCount ? strength : Mathf.Max(0, strength - decay);
        }

        private static float EnemyHitStrength(float previousHealth, Enemy enemy)
        {
            // Several simulation steps may have elapsed since impact in this render frame.
            return enemy.HitFlash <= 0 && enemy.Health < previousHealth ? 1 : Mathf.Clamp01(enemy.HitFlash / .065f);
        }

        private void TowerShotEffect(CombatEvent e, Vector3 p)
        {
            // Each binding projectile gets a tether. The muzzle and recoil still play once per volley.
            if (e.Source == (int)TowerKind.Binding && effects.Count < 96) {
                Color jade = GatefallArt.RoleColor(TowerKind.Binding);
                Vector3 muzzle = p + Vector3.up * .83f, target = Point(e.Target) + Vector3.up * .30f;
                Line(muzzle, target, .16f, .09f, GatefallArt.Alpha(jade, .86f));
                tetherLines++;
            }
            for (int i = 0; i < towers.Length; i++) {
                if (V2.DistanceSquared(board.BoardSockets[i], e.Position) >= .001f) continue;
                // The first shot can arrive before SyncTowers creates the newly placed sprite.
                TowerVisual tower = towers[i] ?? (towers[i] = new TowerVisual());
                // Each target keeps its projectile; one volley shares a single muzzle flash.
                if (tower.VolleyFrame == Time.frameCount) return;
                tower.VolleyFrame = Time.frameCount; tower.Recoil = 1; tower.Kick = (p - Point(e.Target)).normalized;
                break;
            }
            MuzzleEffect(e);
        }

        private void FootstepEffect(Vector3 p, Vector2 direction)
        {
            if (effects.Count >= 96) return;
            Fx dust = Neutral("fx-disc", p + Vector3.up * .05f, new Vector3(-direction.x, -direction.y * .5f, 0), .19f, .42f, .95f,
                GatefallArt.Alpha(GatefallArt.Hex("B9A990"), .5f));
            dust.Stretch = new Vector2(1, .44f);
            dust.Sprite.sortingOrder = Order(p.y) - 2;
        }

        private void MuzzleEffect(CombatEvent e)
        {
            TowerKind kind = (TowerKind)Mathf.Clamp(e.Source, 0, TowerCatalog.All.Length - 1);
            bool hunterShot = e.Source < 0;
            Color tint = hunterShot ? GatefallArt.Ally : GatefallArt.RoleColor(kind);
            Vector3 muzzle = Point(e.Position) + Vector3.up * (hunterShot ? .9f : 1.05f);
            Vector3 direction = (Point(e.Target) + Vector3.up * .38f - muzzle).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector3.right;
            float length = hunterShot ? .92f : kind == TowerKind.Rupture ? .67f : .50f;
            Line(muzzle, muzzle + direction * length, .085f, hunterShot ? .15f : .12f, tint);
            if (hunterShot && effects.Count < 140) {
                Vector3 cross = new Vector3(-direction.y, direction.x, 0);
                Line(muzzle + cross * .23f, muzzle + direction * .69f + cross * .07f, .09f, .055f, Color.white);
            }
            if (effects.Count < 96 || kind == TowerKind.Rupture)
                Neutral("fx-burst", muzzle, Vector3.zero, .065f, .30f, .08f, Color.white);
            if (kind == TowerKind.Binding || kind == TowerKind.Gravity || kind == TowerKind.Marker || kind == TowerKind.Observer || kind == TowerKind.Beacon)
                Pulse(muzzle, .75f, .16f, GatefallArt.Alpha(tint, .8f), false);
        }

        private void WardBurstEffect(Vector3 p, float radius)
        {
            wardBursts++;
            Color blue = GatefallArt.RoleColor(TowerKind.Barrier);
            Pulse(p + Vector3.up * .08f, radius * 2, .40f, blue, true);
            Pulse(p + Vector3.up * .16f, radius * 1.55f, .26f, GatefallArt.Alpha(Color.white, .78f), true);
            Burst(p + Vector3.up * .57f, "fx-shard", blue, 8, 3.8f, .26f, .18f);
        }

        private void MarkFollowUpEffect(Vector3 p)
        {
            if (effects.Count >= 96) return;
            markFollowUps++;
            p += Vector3.up * .45f;
            Color violet = GatefallArt.RoleColor(TowerKind.Marker);
            Line(p + new Vector3(-.38f, -.24f, 0), p + new Vector3(.38f, .24f, 0), .15f, .09f, violet);
            Line(p + new Vector3(-.38f, .24f, 0), p + new Vector3(.38f, -.24f, 0), .15f, .09f, violet);
        }

        private void HitEffect(CombatEvent e)
        {
            Vector3 p = Point(e.Position) + Vector3.up * .38f;
            if (e.Source == -2) {
                Neutral("fx-burst", p, Vector3.zero, .18f, .5f, 2.6f, GatefallArt.Threat);
                Pulse(p, 4.4f, .28f, GatefallArt.Threat, true);
                Burst(p, "fx-shard", GatefallArt.Threat, 8, 4, .26f, .24f); return;
            }
            TowerKind kind = (TowerKind)Mathf.Clamp(e.Source, 0, TowerCatalog.All.Length - 1);
            Color tint = e.Source < 0 ? GatefallArt.Ally : GatefallArt.RoleColor(kind);
            Vector3 direction = (Point(e.Position) - Point(e.Target)).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector3.right;
            if (kind == TowerKind.Arc && e.Source >= 0)
                Lightning(Point(e.Target) + Vector3.up * .38f, p);
            if (kind == TowerKind.Binding && e.Source >= 0) {
                Pulse(p - Vector3.up * .23f, 1.15f, .22f, tint, true);
                if (effects.Count >= 96) return;
                Line(p + new Vector3(-.34f, -.20f, 0), p + new Vector3(.34f, .20f, 0), .12f, .065f, tint);
                Line(p + new Vector3(-.34f, .20f, 0), p + new Vector3(.34f, -.20f, 0), .12f, .065f, tint);
            } else if ((kind == TowerKind.Marker || kind == TowerKind.Observer || kind == TowerKind.Beacon) && e.Source >= 0) {
                Pulse(p, .85f, .18f, tint, false);
            } else {
                Fx flare = Neutral("fx-burst", p, Vector3.zero, .10f, e.Source < 0 ? .68f : .49f, .12f, tint);
                flare.Sprite.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 90));
                if (effects.Count < 112) Neutral("fx-disc", p, Vector3.zero, .065f, .36f, .04f, Color.white);
                if (effects.Count < 140 && kind != TowerKind.Rupture && kind != TowerKind.Gravity)
                    Line(p - direction * .19f, p + direction * .51f, .105f, .085f, Color.white);
            }
        }

        private void DeathEffect(CombatEvent e)
        {
            EnemyKind fallen = (EnemyKind)Mathf.Clamp(e.Source, 0, 4);
            bool heavy = fallen == EnemyKind.Brute || fallen == EnemyKind.Siege || fallen == EnemyKind.Boss;
            float scale = EnemyScale(fallen), punch = fallen == EnemyKind.Boss ? 2.0f : heavy ? 1.28f : .70f;
            Vector3 p = Point(e.Position), direction = (p - Point(e.Target)).normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector3.up;
            // Effects arrive before Sync releases the dead sprite or mesh point.
            Vector3 body = p;
            bool left = false;
            if (fallen == EnemyKind.Grunt && swarmPoints.TryGetValue(e.EnemyId, out SwarmPoint point)) {
                body = SwarmBodyPosition(point, e.EnemyId);
                left = point.Left;
            } else if (enemies.TryGetValue(e.EnemyId, out EnemyVisual visual)) {
                body = visual.Body.transform.position;
                left = visual.Left;
            }
            Fx remnant = Neutral(GatefallArt.EnemyKey(fallen), body, direction * (heavy ? .86f : 1.7f), heavy ? .24f : .115f, scale * 1.07f, scale * .23f,
                GatefallArt.Alpha(heavy ? GatefallArt.Hex("C4AAA4") : GatefallArt.Hex("FFDCAB"), heavy ? .91f : .78f));
            remnant.Sprite.flipX = left;
            remnant.Sprite.sortingOrder = Order(body.y) + 1;
            remnant.Spin = direction.x > 0 ? -50 : 50; remnant.FadePower = 1;
            if (heavy || effects.Count < 96)
                Pulse(p + Vector3.up * .13f, punch * (heavy ? 2f : 1.15f), heavy ? .30f : .13f, GatefallArt.Alpha(heavy ? GatefallArt.Threat : GatefallArt.Amber, heavy ? .85f : .60f), true);
            if (heavy || effects.Count < 145)
                Burst(p + Vector3.up * .38f, "fx-shard", GatefallArt.Hex("FFD977"), heavy ? 8 : effects.Count >= 96 ? 1 : 2, 5.4f * punch, heavy ? .29f : .16f, heavy ? .22f : .15f);
            if (heavy) {
                Neutral("fx-burst", p + Vector3.up * .6f, Vector3.zero, .145f, punch * 1.14f, punch * .2f, Color.white);
                if (fallen == EnemyKind.Boss) Pulse(p, 5.9f, .47f, GatefallArt.Amber, true);
            }
        }

        private void BlastEffect(CombatEvent e)
        {
            Vector3 p = Point(e.Position);
            float diameter = Mathf.Max(1, e.Value * 2);
            TowerKind kind = (TowerKind)Mathf.Clamp(e.Source, 0, TowerCatalog.All.Length - 1);
            Color tint = GatefallArt.RoleColor(kind);
            if (kind == TowerKind.Gravity) {
                Fx vortex = Neutral("fx-vortex", p + Vector3.up * .15f, Vector3.zero, .32f, diameter * .65f, diameter * .16f, GatefallArt.Alpha(tint, .8f)); vortex.Spin = -420;
                Pulse(p, diameter, .23f, tint, true);
            } else if (kind == TowerKind.Seal) {
                Neutral("fx-pillar", p + Vector3.up * 1.1f, Vector3.zero, .20f, .65f, .10f, Color.white).Stretch = new Vector2(1, 1.7f);
                Pulse(p, diameter, .25f, tint, true);
            } else {
                Neutral("fx-disc", p + Vector3.up * .20f, Vector3.zero, .065f, diameter * .22f, diameter * .08f, Color.white);
                Pulse(p + Vector3.up * .10f, diameter, .24f, tint, true);
                Neutral("fx-burst", p + Vector3.up * .28f, Vector3.zero, .18f, diameter * .30f, diameter * .55f, GatefallArt.Alpha(tint, .8f));
                Burst(p + Vector3.up * .30f, "fx-shard", tint, 6, 4.5f, .25f, .19f);
            }
        }
    }
}
