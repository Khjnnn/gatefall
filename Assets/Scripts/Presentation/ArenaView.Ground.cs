using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class ArenaView
    {
        private void BuildGround(DefenseSimulation game)
        {
            int stage = game.Rules != null && game.Rules.Stage != null ? game.Rules.Stage.Index : 0;
            int theme = game.Rules != null && game.Rules.Stage != null ? BattlefieldLayouts.Get(game.Rules.Stage).Theme : 0;
            bool market=game.Rules!=null && game.Rules.Stage!=null && game.Rules.Stage.Chapter==2;
            bool rail = game.Rules?.Stage?.Chapter == 3;
            Color[] groundTints = { GatefallArt.Hex("E6DFCE"), GatefallArt.Hex("C9D3D6"), GatefallArt.Hex("E7D8BE"), GatefallArt.Hex("C9CFCA") };
            if(market)groundTints=new[]{GatefallArt.Hex("E4D1AE"),GatefallArt.Hex("DBC9AD"),GatefallArt.Hex("BAC7CD"),GatefallArt.Hex("CABBA5")};
            Texture2D paving = art.Texture(rail ? "rail-concrete-painted" : stage == 10 ? "district-training-ground" : "district-ground");
            if (rail && paving == null) paving = art.Texture("rail-concrete");
            if (paving == null) paving = art.Texture("district-ground");
            if (paving != null) paving.wrapMode = TextureWrapMode.Repeat;
            Mesh plate = new Mesh { name = "Tiled district ground" };
            plate.vertices = new[] { new Vector3(-32,-22,0),new Vector3(32,-22,0),new Vector3(32,22,0),new Vector3(-32,22,0) };
            float tile = rail ? 11 : stage == 10 ? 12 : 24;
            float tileHeight = rail ? tile : tile/2;
            plate.uv = new[] { new Vector2(0,0),new Vector2(64f/tile,0),new Vector2(64f/tile,44f/tileHeight),new Vector2(0,44f/tileHeight) };
            Color groundTint = rail ? Color.white : groundTints[theme];
            plate.colors = new[] { groundTint,groundTint,groundTint,groundTint }; plate.triangles = new[] {0,1,2,0,2,3}; plate.RecalculateBounds(); owned.Add(plate);
            Material groundMaterial = new Material(Shader.Find("Sprites/Default")); groundMaterial.mainTexture = paving; owned.Add(groundMaterial);
            GameObject groundObject = new GameObject("District paving"); groundObject.transform.SetParent(root,false);
            groundObject.AddComponent<MeshFilter>().sharedMesh = plate;
            MeshRenderer groundRenderer = groundObject.AddComponent<MeshRenderer>(); groundRenderer.sharedMaterial = groundMaterial; groundRenderer.sortingOrder = -32000;
            // Quiet painted courts and a curated landmark give each district a composition,
            // while the road mesh, pads and actors remain above all floor decoration.
            if (stage == 10) {
                CourtPaint(new V2(-11.5f, 0), 5, 5.5f, GatefallArt.Hex("5D7774"));
                CourtPaint(new V2(11.5f, -2), 5, 5.5f, GatefallArt.Hex("5D7774"));
            } else if (stage == 11) {
                CourtPaint(new V2(16.5f, 1), 9, 8, GatefallArt.Hex("747B62"));
                CourtPaint(new V2(-13.5f, 0), 4, 5, GatefallArt.Hex("9F9276"));
            }
            Rect landmark = DistrictLandmark(game, stage, theme, rail);
            Texture2D asphalt = rail ? art.Texture("rail-ballast-painted") ?? art.Texture("rail-ballast") : art.Texture("asphalt");
            if (asphalt != null) asphalt.wrapMode = TextureWrapMode.Repeat;
            RoadSurface(game, 1.42f, GatefallArt.Hex(rail ? "31383B" : "727675"), -31300, null);
            RoadSurface(game, 1.25f, GatefallArt.Hex(rail ? "988263" : "CBC8B7"), -31200, null);
            RoadSurface(game, BattlefieldLayouts.RoadHalfWidth, Color.white, -31100, asphalt);
            var railVertices = rail ? new List<Vector3>() : null;
            var railColors = rail ? new List<Color>() : null;
            var railIndices = rail ? new List<int>() : null;
            foreach (int lane in game.OpenLanes) {
                V2[] road = game.Roads[lane]; float length = BattlefieldLayouts.RoadLength(road);
                if (rail) DrawRails(road, railVertices, railColors, railIndices);
                int marking = 0;
                for (float d = 1.6f; d < length - 3.8f; d += 2.4f, marking++) {
                    int next; V2 p = BattlefieldLayouts.Along(road, d, out next);
                    V2 direction = (road[next] - road[next - 1]).Normalized;
                    // Occasional faded chevrons show the real path direction without covering a bend.
                    if (marking % 3 == 1 && V2.Distance(p, road[next]) > .85f && V2.Distance(p, road[next - 1]) > .85f) {
                        V2 side = new V2(-direction.Y, direction.X) * .26f;
                        V2 tip = p + direction * .34f, tail = p - direction * .24f;
                        RoadMark(tail + side, tip); RoadMark(tail - side, tip);
                    } else if (!rail) RoadMark(p - direction * .31f, p + direction * .31f);
                }
            }
            if (rail) {
                var tracks = new Mesh { name = "Raon rails and sleepers" };
                tracks.SetVertices(railVertices);
                tracks.SetColors(railColors);
                tracks.SetTriangles(railIndices, 0);
                tracks.RecalculateBounds();
                var material = new Material(Shader.Find("Sprites/Default")) { mainTexture = Texture2D.whiteTexture };
                owned.Add(tracks);
                owned.Add(material);
                var trackObject = new GameObject("Raon rails and sleepers");
                trackObject.transform.SetParent(root, false);
                trackObject.AddComponent<MeshFilter>().sharedMesh = tracks;
                var renderer = trackObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = -31095;
            }
            // A small public square joins all active streets at the barrier.
            Sprite disc = art.Sprite("fx-disc");
            Make("Barrier plaza edge", disc, Vector3.zero, Vector3.one * (5.8f / DiscDiameter), GatefallArt.Hex(rail ? "8A8980" : "BDBBAB"), -31070);
            Make("Barrier plaza", disc, Vector3.zero, Vector3.one * (5.3f / DiscDiameter), GatefallArt.Hex(rail ? "656B68" : "8E968B"), -31060);
            Color[] tints = { Color.white, new Color(.91f,.96f,1), new Color(1,.96f,.89f), new Color(.91f,.93f,.97f) };
            // Entire transparent sprite cells must stay clear of every road and tower site.
            for (int x = -5; x <= 5; x++) for (int y = -3; y <= 3; y++) {
                int hash = ((x * 37 + y * 61 + stage * 101) % 997 + 997) % 997;
                if (hash % 5 == 0) continue;
                V2 p = new V2(x * 5.3f + (hash % 11 - 5) * .18f, y * 5.2f + (hash % 7 - 3) * .2f);
                float width = 4.0f + hash % 4 * .55f, height = width * .67f;
                if (landmark.width > 0 && landmark.Overlaps(new Rect(p.X - width/2, p.Y - height/2, width, height))) continue;
                if (p.Length < 6) continue;
                bool clear = true;
                for (int ix = -1; ix <= 1 && clear; ix++) for (int iy = -1; iy <= 1; iy++) {
                    V2 sample = p + new V2(ix * width * .5f, iy * height * .5f);
                    foreach (int lane in game.OpenLanes) if (BattlefieldLayouts.RoadDistance(sample, game.Roads[lane]) < 1.8f) clear = false;
                    foreach (V2 pad in game.BoardSockets) if (V2.Distance(sample, pad) < 1.8f) clear = false;
                }
                if (!clear) continue;
                int variety = rail ? 6 : 4;
                int index = ((x * 7 + y * 3 + stage * 5) % variety + variety) % variety;
                Sprite sprite = rail ? art.RailProp(index) : art.Scenery(index,market); if (sprite == null) continue;
                Make("District block " + index, sprite, Point(p), new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1), rail ? Color.white : tints[theme], -30000 + Order(p.Y));
            }
        }

        private void DrawRails(V2[] road, List<Vector3> vertices, List<Color> colors, List<int> indices)
        {
            Color wood = GatefallArt.Hex("685849"), metal = GatefallArt.Hex("7F807A");
            Sprite portal = art.Sprite("rail-tunnel-painted");
            V2 outward = (road[0] - road[1]).Normalized;
            if (portal != null)
                Make("Tunnel behind rift", portal, Point(road[0]+outward*.65f), new Vector3(5.5f/portal.bounds.size.x,4.1f/portal.bounds.size.y,1), Color.white, 110);

            // Round only the paint. Trim each corner within the existing road surface.
            var centerline = new List<V2> { road[0] - outward*.18f };
            for (int i=1; i<road.Length-1; i++) {
                V2 incoming = road[i]-road[i-1], outgoing = road[i+1]-road[i];
                float trim = Mathf.Min(.78f, Mathf.Min(incoming.Length, outgoing.Length)*.28f);
                if (trim < .02f) continue;
                V2 enter = road[i]-incoming.Normalized*trim, leave = road[i]+outgoing.Normalized*trim;
                centerline.Add(enter);
                for (int sample=1; sample<=6; sample++) {
                    float t=sample/6f, rest=1-t;
                    centerline.Add(enter*(rest*rest) + road[i]*(2*t*rest) + leave*(t*t));
                }
            }
            centerline.Add(road[road.Length-1]);

            float nextSleeper = .24f;
            var normals = new V2[centerline.Count];
            for (int i=0; i<centerline.Count; i++) {
                V2 tangent = (centerline[Mathf.Min(i+1,centerline.Count-1)]-centerline[Mathf.Max(0,i-1)]).Normalized;
                normals[i] = new V2(-tangent.Y,tangent.X);
            }
            for (int i=1; i<centerline.Count; i++) {
                V2 start=centerline[i-1], delta=centerline[i]-start;
                if (delta.Length < .001f) continue;
                V2 along=delta.Normalized, across=new V2(-along.Y,along.X);
                while (nextSleeper <= delta.Length) {
                    V2 point=start+along*nextSleeper;
                    TrackMark(point-across*.62f, point+across*.62f, .085f, wood);
                    nextSleeper += .56f;
                }
                nextSleeper -= delta.Length;
            }
            for (int side=-1; side<=1; side+=2) {
                for (int i=1; i<centerline.Count; i++) {
                    V2 a=centerline[i-1], b=centerline[i], na=normals[i-1], nb=normals[i];
                    float inner=side*.43f-.032f, outer=side*.43f+.032f;
                    TrackQuad(a+na*outer, a+na*inner, b+nb*inner, b+nb*outer, metal);
                }
            }

            void TrackMark(V2 from, V2 to, float thickness, Color tint)
            {
                V2 direction = (to-from).Normalized;
                V2 normal = new V2(-direction.Y,direction.X)*(thickness/2);
                TrackQuad(from+normal, from-normal, to-normal, to+normal, tint);
            }

            void TrackQuad(V2 a, V2 b, V2 c, V2 d, Color tint)
            {
                int first = vertices.Count;
                vertices.Add(Point(a)); vertices.Add(Point(b));
                vertices.Add(Point(c)); vertices.Add(Point(d));
                colors.Add(tint); colors.Add(tint); colors.Add(tint); colors.Add(tint);
                indices.Add(first); indices.Add(first+1); indices.Add(first+2);
                indices.Add(first); indices.Add(first+2); indices.Add(first+3);
            }
        }

        private void CourtPaint(V2 center, float width, float height, Color tint)
        {
            Make("District court surface", square, Point(center), new Vector3(width, height, 1), GatefallArt.Alpha(tint, .32f), -31900);
            Color line = GatefallArt.Alpha(GatefallArt.Hex("E0DDD0"), .42f);
            foreach (float y in new[] { center.Y-height*.38f, center.Y+height*.38f })
                Make("Worn court boundary", square, Point(new V2(center.X,y)), new Vector3(width*.8f,.045f,1), line,-31890);
            foreach (float x in new[] { center.X-width*.4f, center.X+width*.4f })
                Make("Worn court boundary", square, Point(new V2(x,center.Y)), new Vector3(.045f,height*.76f,1), line,-31890);
        }

        private Rect DistrictLandmark(DefenseSimulation game, int stage, int theme, bool rail)
        {
            V2[] anchors = { new V2(0,9),new V2(18.5f,8.5f),new V2(-18.5f,8.5f),new V2(-10,9),new V2(10,9),new V2(18.5f,-8.5f),new V2(-18.5f,-8.5f),new V2(0,-9),new V2(-10,-9),new V2(10,-9) };
            for (int size=0;size<2;size++) for(int i=0;i<anchors.Length;i++) {
                V2 center=anchors[(i+stage*3)%anchors.Length]; if(center.Y>7 && Mathf.Abs(center.X)<6)continue; float width=size==0?8:5.5f,height=width*.54f;
                bool clear=true;
                for(int ix=0;ix<=10 && clear;ix++)for(int iy=0;iy<=6 && clear;iy++) {
                    V2 sample=center+new V2((ix/10f-.5f)*width,(iy/6f-.5f)*height);
                    foreach(int lane in game.OpenLanes)if(BattlefieldLayouts.RoadDistance(sample,game.Roads[lane])<1.9f){clear=false;break;}
                    foreach(V2 pad in game.BoardSockets)if(V2.Distance(sample,pad)<1.6f){clear=false;break;}
                }
                if(!clear)continue;
                // Existing original atlas: homes, warehouse, garden, underpass.
                int index=stage==10?2:stage==11?1:theme==1?3:theme==2?1:stage%4;
                Sprite sprite=rail ? art.RailProp(stage%6) : art.Scenery(index,game.Rules?.Stage?.Chapter==2);
                if(sprite==null)return new Rect();
                // Atlas scenery already has a painted footprint; avoid a flat opaque pedestal.
                Make("District landmark "+stage,sprite,Point(center),new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1),Color.white,-30000+Order(center.Y));
                Debug.Log("GATEFALL LANDMARK stage="+(stage+1)+" x="+center.X+" y="+center.Y+" width="+width+" atlas="+index);
                return new Rect(center.X-width/2-.4f,center.Y-height/2-.4f,width+.8f,height+.8f);
            }
            return new Rect();
        }

        private void RoadMark(V2 from, V2 to)
        {
            V2 delta = to - from;
            SpriteRenderer mark = Make("Worn road marking", square, Point((from + to) * .5f),
                new Vector3(delta.Length, .075f, 1), GatefallArt.Alpha(GatefallArt.Hex("D6CDA8"), .60f), -31090);
            mark.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.Y, delta.X) * Mathf.Rad2Deg);
        }

        private void RoadSurface(DefenseSimulation game, float radius, Color tint, int order, Texture2D texture)
        {
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var indices = new List<int>(); var colors = new List<Color>();
            foreach (int lane in game.OpenLanes) {
                V2[] road = game.Roads[lane];
                for (int i = 1; i < road.Length; i++) {
                    V2 a = road[i - 1], b = road[i], dir = (b - a).Normalized, side = new V2(-dir.Y, dir.X) * radius;
                    int n = vertices.Count;
                    GroundVertex(a + side, tint, vertices, uvs, colors); GroundVertex(a - side, tint, vertices, uvs, colors);
                    GroundVertex(b - side, tint, vertices, uvs, colors); GroundVertex(b + side, tint, vertices, uvs, colors);
                    indices.AddRange(new[] { n,n+1,n+2,n,n+2,n+3 });
                }
                foreach (V2 p in road) {
                    int center = vertices.Count; GroundVertex(p, tint, vertices, uvs, colors);
                    const int sides = 16;
                    for (int i = 0; i <= sides; i++) {
                        float a = i * Mathf.PI * 2 / sides;
                        GroundVertex(p + new V2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, vertices, uvs, colors);
                        if (i > 0) { indices.Add(center); indices.Add(center+i); indices.Add(center+i+1); }
                    }
                }
            }
            Mesh mesh = new Mesh { name = "Shared gameplay roads" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds(); owned.Add(mesh);
            Material material = new Material(Shader.Find("Sprites/Default")); material.mainTexture = texture; owned.Add(material);
            GameObject obj = new GameObject("Road surface " + order); obj.transform.SetParent(root, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = order;
        }

        private static void GroundVertex(V2 p, Color color, List<Vector3> vertices, List<Vector2> uvs, List<Color> colors)
        { vertices.Add(Point(p)); uvs.Add(new Vector2(p.X / 7, p.Y / 7)); colors.Add(color); }
    }
}
