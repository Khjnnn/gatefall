using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class ArenaView
    {
        // A small shared mesh per depth row replaces eight GameObjects per common enemy.
        // Elite/runner/siege/boss visuals keep their individual silhouettes and warning UI.
        private sealed class SwarmPoint { public Vector3 Position; public float LastX, LastHealth; public bool Left; }
        private struct SwarmShape
        {
            public float X0, X1, Y0, Y1, U0, U1, V0, V1;
            public SwarmShape(Sprite sprite) {
                Rect rect=sprite.rect;Vector2 pivot=sprite.pivot;float ppu=sprite.pixelsPerUnit;
                X0=-pivot.x/ppu;X1=(rect.width-pivot.x)/ppu;Y0=-pivot.y/ppu;Y1=(rect.height-pivot.y)/ppu;
                U0=rect.xMin/sprite.texture.width;U1=rect.xMax/sprite.texture.width;V0=rect.yMin/sprite.texture.height;V1=rect.yMax/sprite.texture.height;
            }
        }
        private sealed class SwarmRow
        {
            public Mesh Mesh; public MeshRenderer Renderer;
            public SwarmShape Shape;public bool Visible;
            public int Count;int previousCount,capacity;bool resized;
            public Vector3[] Positions;public Color32[] Colors;Vector2[] uvs;int[] triangles;
            public void Clear() { Count=0; }
            public int Add()
            {
                if(Count==capacity) {
                    capacity=capacity==0 ? 32 : capacity*2;
                    System.Array.Resize(ref Positions,capacity*4);System.Array.Resize(ref Colors,capacity*4);
                    uvs=new Vector2[capacity*4];triangles=new int[capacity*6];
                    for(int i=0;i<capacity;i++) {
                        int v=i*4,t=i*6;uvs[v]=new Vector2(Shape.U0,Shape.V0);uvs[v+1]=new Vector2(Shape.U1,Shape.V0);uvs[v+2]=new Vector2(Shape.U1,Shape.V1);uvs[v+3]=new Vector2(Shape.U0,Shape.V1);
                        triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
                    }
                    resized=true;
                }
                return Count++*4;
            }
            public void Upload()
            {
                bool visible=Count>0;if(visible!=Visible){Renderer.enabled=visible;Visible=visible;}
                if(!visible)return;
                // Vertex/index capacity stays fixed between growth events; unused quads collapse.
                for(int i=Count*4;i<previousCount*4;i++){Positions[i]=Vector3.zero;Colors[i]=new Color32();}
                if(resized)Mesh.Clear();
                Mesh.SetVertices(Positions);Mesh.SetColors(Colors);
                if(resized){Mesh.SetUVs(0,uvs);Mesh.SetTriangles(triangles,0,false);Mesh.bounds=new Bounds(Vector3.zero,new Vector3(60,36,4));resized=false;}
                previousCount=Count;
            }
        }

        private readonly Dictionary<int, SwarmPoint> swarmPoints = new Dictionary<int, SwarmPoint>(512);
        private readonly Stack<SwarmPoint> swarmPointPool = new Stack<SwarmPoint>(512);
        private readonly List<Enemy> swarmOrder = new List<Enemy>(512);
        private readonly HashSet<int> swarmSeen = new HashSet<int>();
        private readonly List<int> swarmStale = new List<int>(512);
        private SwarmRow[] swarmBodies, swarmStatuses;
        private SwarmRow swarmHealth;
        private Sprite swarmBodySprite, swarmStatusSprite;
        private int lastSwarmSimulationCount=-1;
        public int SwarmRenderedCount { get; private set; }
        public int SwarmBatchCount { get; private set; }
        public int DetailedEnemyCount { get { return enemies.Count; } }
        public string SwarmTextureSize { get { return swarmBodySprite == null ? "-" : swarmBodySprite.texture.width + "x" + swarmBodySprite.texture.height; } }

        private SwarmRow MakeSwarmRow(string name, Material material, int order, Sprite sprite)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root, false);
            var mesh = new Mesh { name = name }; mesh.MarkDynamic(); owned.Add(mesh);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.sortingOrder = order; renderer.enabled = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return new SwarmRow { Mesh = mesh, Renderer = renderer, Shape=new SwarmShape(sprite) };
        }
        private void InitializeSwarm()
        {
            swarmBodySprite = art.Sprite("enemy-grunt"); swarmStatusSprite = art.Sprite("fx-ring");
            var bodyMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Common swarm", mainTexture = swarmBodySprite.texture };
            var statusMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Common status", mainTexture = swarmStatusSprite.texture };
            owned.Add(bodyMaterial); owned.Add(statusMaterial);
            var healthMaterial = new Material(defaultEffectMaterial) { name = "Common health", mainTexture = square.texture };
            owned.Add(healthMaterial);
            swarmHealth = MakeSwarmRow("Swarm health", healthMaterial, 4048, square);
            swarmBodies = new SwarmRow[24]; swarmStatuses = new SwarmRow[24];
            for (int i = 0; i < 24; i++) {
                int order = Order(i - 11.5f);
                swarmBodies[i] = MakeSwarmRow("Swarm row " + i, bodyMaterial, order, swarmBodySprite);
                swarmStatuses[i] = MakeSwarmRow("Swarm status " + i, statusMaterial, order - 1, swarmStatusSprite);
            }
        }
        private static void AddSwarmQuad(SwarmRow row, Vector3 point, float scale, Vector2 stretch, Color32 color, bool flip)
        {
            int n=row.Add();SwarmShape shape=row.Shape;
            float x0=shape.X0*scale*stretch.x,x1=shape.X1*scale*stretch.x,y0=shape.Y0*scale*stretch.y,y1=shape.Y1*scale*stretch.y;
            if(flip){float tmp=x0;x0=x1;x1=tmp;}
            row.Positions[n]=point+new Vector3(x0,y0,0);row.Positions[n+1]=point+new Vector3(x1,y0,0);row.Positions[n+2]=point+new Vector3(x1,y1,0);row.Positions[n+3]=point+new Vector3(x0,y1,0);
            for(int i=0;i<4;i++)row.Colors[n+i]=color;
        }
        private Vector3 SwarmBodyPosition(SwarmPoint point, int id)
        { return point.Position + Vector3.up * (Mathf.Abs(Mathf.Sin(time * 15 + id)) * .045f); }

        private void SyncSwarm(DefenseSimulation game, float dt)
        {
            if (swarmBodies == null) InitializeSwarm();
            if(dt<=0 && game.Enemies.Count==lastSwarmSimulationCount)return;
            lastSwarmSimulationCount=game.Enemies.Count;
            swarmSeen.Clear(); swarmOrder.Clear(); SwarmBatchCount = 0;
            foreach (var row in swarmBodies) row.Clear(); foreach (var row in swarmStatuses) row.Clear();
            swarmHealth.Clear();
            foreach (Enemy enemy in game.Enemies) if (enemy.Kind == EnemyKind.Grunt) swarmOrder.Add(enemy);
            swarmOrder.Sort((a, b) => { int d = b.Position.Y.CompareTo(a.Position.Y); return d != 0 ? d : a.Id.CompareTo(b.Id); });
            float blend = 1 - Mathf.Exp(-32 * Mathf.Max(0, dt));
            foreach (Enemy enemy in swarmOrder) {
                swarmSeen.Add(enemy.Id); SwarmPoint point;
                if (!swarmPoints.TryGetValue(enemy.Id, out point)) {
                    point = swarmPointPool.Count > 0 ? swarmPointPool.Pop() : new SwarmPoint();
                    point.Position = Point(enemy.Position); point.LastX = enemy.Position.X; point.LastHealth = enemy.MaxHealth;
                    point.Left = enemy.Position.X > 0;
                    swarmPoints.Add(enemy.Id, point);
                }
                float dx = enemy.Position.X - point.LastX; point.LastX = enemy.Position.X;
                if (enemy.BlockOwner < 0) point.Left = game.PlayerPosition.X < enemy.Position.X;
                else if (Mathf.Abs(dx) > .0005f) point.Left = dx < 0;
                Vector3 destination = Point(enemy.Position);
                point.Position = (point.Position - destination).sqrMagnitude > 9 ? destination : Vector3.Lerp(point.Position, destination, blend);
                int row = Mathf.Clamp(Mathf.FloorToInt(point.Position.y + 12), 0, 23);
                Vector3 body = SwarmBodyPosition(point, enemy.Id);
                bool hit = EnemyHitStrength(point.LastHealth, enemy) > 0;
                point.LastHealth = enemy.Health;
                AddSwarmQuad(swarmBodies[row], body, EnemyScale(EnemyKind.Grunt), Vector2.one,
                    hit ? new Color32(255, 245, 205, 255) : new Color32(255, 255, 255, 255), point.Left);
                float hp = Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
                if (hp < .999f || swarmOrder.Count <= 18) {
                    const float width = .56f;
                    Vector3 center = body + Vector3.up * .81f;
                    AddSwarmQuad(swarmHealth, center, 1, new Vector2(width + .08f, .14f), new Color32(22, 24, 32, 225), false);
                    AddSwarmQuad(swarmHealth, center + Vector3.left * (width * .5f * (1 - hp)), 1, new Vector2(width * hp, .075f),
                        GatefallArt.Alpha(EnemyHealthColor(hp), hp < .999f ? .96f : .72f), false);
                }
                if (enemy.MarkRemaining > 0 || enemy.SlowFraction > 0 || enemy.BlockOwner != 0) {
                    Color tint = enemy.MarkRemaining > 0 ? GatefallArt.Ally : enemy.SlowFraction > 0 ? GatefallArt.Hex("6796EF") : GatefallArt.Amber;
                    AddSwarmQuad(swarmStatuses[row], point.Position, .6f, new Vector2(1, .5f), tint, false);
                }
            }
            swarmStale.Clear(); foreach (var pair in swarmPoints) if (!swarmSeen.Contains(pair.Key)) swarmStale.Add(pair.Key);
            foreach (int id in swarmStale) { swarmPointPool.Push(swarmPoints[id]); swarmPoints.Remove(id); }
            for (int i = 0; i < 24; i++) { swarmBodies[i].Upload(); swarmStatuses[i].Upload(); if (swarmBodies[i].Visible) SwarmBatchCount++; if (swarmStatuses[i].Visible) SwarmBatchCount++; }
            swarmHealth.Upload();
            if (swarmHealth.Visible) SwarmBatchCount++;
            SwarmRenderedCount = swarmOrder.Count;
        }
    }
}
