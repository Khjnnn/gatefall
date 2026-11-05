using System;
using System.IO;
using Gatefall.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gatefall.Editor
{
    public static class PrototypeTools
    {
        public const string ScenePath = "Assets/Scenes/Gatefall.unity";

        // Batch-mode audit of the real swarm mesh, individual sprite and effect pool paths.
        public static void AuditDeathRemnant()
        {
            var parent = new GameObject("Death remnant audit");
            try {
                var game = new DefenseSimulation(new GameBalance { SpawnEnemies = false }, 7);
                var art = new GatefallArt();
                var view = new ArenaView(parent.transform, game, art);
                var grunt = new Enemy { Id = 40, Kind = EnemyKind.Grunt, Position = new V2(8, 4), Health = 60, MaxHealth = 60 };
                var brute = new Enemy { Id = 41, Kind = EnemyKind.Brute, Position = new V2(10, 4), Health = 220, MaxHealth = 220 };
                game.Enemies.Add(grunt);
                game.Enemies.Add(brute);
                view.Sync(game, 0, .1f, new ArenaSelection());

                SpriteRenderer FindSprite(EnemyKind kind, bool effect)
                {
                    Sprite sprite = art.Sprite(GatefallArt.EnemyKey(kind));
                    return Array.Find(parent.GetComponentsInChildren<SpriteRenderer>(), r => r.sprite == sprite && (r.name == "Fx") == effect);
                }

                Vector3 bruteBody = FindSprite(brute.Kind, false).transform.position;
                foreach (Enemy enemy in game.Enemies)
                    view.Effect(new CombatEvent(EventKind.Kill, enemy.Position + new V2(-1, 0), new V2(), 1, (int)enemy.Kind, enemyId: enemy.Id));
                SpriteRenderer gruntRemnant = FindSprite(grunt.Kind, true), bruteRemnant = FindSprite(brute.Kind, true);
                if (gruntRemnant == null || bruteRemnant == null || !gruntRemnant.flipX || !bruteRemnant.flipX ||
                    !Mathf.Approximately(gruntRemnant.transform.position.x, 8) || (bruteRemnant.transform.position - bruteBody).sqrMagnitude > .00001f)
                    throw new BuildFailedException("Death remnant lost its visible position or facing");

                view.Effect(new CombatEvent(EventKind.Kill, new V2(-4, 5), new V2(), 1, (int)EnemyKind.Runner, enemyId: 99));
                SpriteRenderer fallback = FindSprite(EnemyKind.Runner, true);
                if (fallback == null || fallback.flipX || !Mathf.Approximately(fallback.transform.position.x, -4))
                    throw new BuildFailedException("Unrendered death did not fall back to the event");
                for (int i = 0; i < 240; i++) view.Effect(new CombatEvent(EventKind.Build, new V2(), new V2(), 0));
                if (gruntRemnant.flipX || bruteRemnant.flipX)
                    throw new BuildFailedException("A reused effect inherited death facing");
                Debug.Log("GATEFALL DEATH AUDIT left grunt and brute preserve visible location/facing; unrendered fallback and recycled facing reset pass.");
            }
            finally {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        public static void AuditExpiredEnemyHit()
        {
            ExpiredEnemyHitFixture();
            Debug.Log("GATEFALL ENEMY HIT AUDIT beforeHP=220 afterHP=160 simHitFlash=0 dt=.20 emittedHit=true");
        }

        private static Enemy ExpiredEnemyHitFixture()
        {
            // One render frame at 2× may simulate up to 0.20 seconds in 60 Hz substeps.
            var game = new DefenseSimulation(new GameBalance { SpawnEnemies = false, OfferCards = false, PlayerRange = .1f }, 7);
            var enemy = new Enemy { Id = 42, Kind = EnemyKind.Brute, Position = new V2(8, 4), Health = 220, MaxHealth = 220 };
            game.Enemies.Add(enemy);
            game.Projectiles.Add(new Projectile { TargetId = enemy.Id, Position = enemy.Position, Speed = 28, Damage = 60 });
            game.Start();
            game.Tick(.20f, new V2());
            if (enemy.Health != 160 || enemy.HitFlash != 0 || !game.Events.Exists(e => e.Kind == EventKind.Hit))
                throw new BuildFailedException("Long-frame enemy hit fixture did not reproduce");
            return enemy;
        }

        public static void AuditHudCommandAllocations()
        {
            const int batches = 512, pairs = 64;
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var hud = new MobileHud(null, null);
            Type type = typeof(MobileHud);
            var items = (System.Collections.IList)type.GetField("items", flags).GetValue(hud);
            var fill = (Action<Rect, Color>)type.GetMethod("Fill", flags).CreateDelegate(typeof(Action<Rect, Color>), hud);
            var text = (Action<Rect, string, int, Color, TextAnchor, int, bool>)type.GetMethod("Text", flags)
                .CreateDelegate(typeof(Action<Rect, string, int, Color, TextAnchor, int, bool>), hud);
            var rect = new Rect(20, 60, 240, 28);
            Color color = Color.white;

            void Rebuild()
            {
                items.Clear();
                for (int i = 0; i < pairs; i++) {
                    fill(rect, color);
                    text(rect, "전투 준비", 16, color, TextAnchor.MiddleLeft, 0, false);
                }
            }

            for (int i = 0; i < 8; i++) Rebuild();
            const Unity.Profiling.ProfilerRecorderOptions options =
                Unity.Profiling.ProfilerRecorderOptions.StartImmediately | Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread;
            int samples;
            long allocated = 0;
            using (var recorder = new Unity.Profiling.ProfilerRecorder(Unity.Profiling.ProfilerCategory.Memory, "GC.Alloc", 2 * pairs * batches + 16, options)) {
                for (int i = 0; i < batches; i++) Rebuild();
                recorder.Stop();
                samples = recorder.Count;
                for (int i = 0; i < samples; i++) allocated += recorder.GetSample(i).Value;
            }
            if (items.Count != 2 * pairs) throw new BuildFailedException("HUD command count changed");
            bool valueType = type.GetNestedType("Item", System.Reflection.BindingFlags.NonPublic).IsValueType;
            Debug.Log($"GATEFALL HUD COMMAND AUDIT version={Application.version} valueType={valueType} commands={2 * pairs * batches} samples={samples} allocatedBytes={allocated}");
        }

        [MenuItem("Gatefall/Mobile/Apply Android and iOS Settings")]
        public static void ConfigureMobile()
        {
            ImportArtwork();
            IncludeRuntimeHudShader();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gatefall.prototype");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.gatefall.prototype");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            // Swappy's Vulkan refresh-cycle initialization crashes in the API35 ARM translation test environment.
            // Use Unity frame pacing for this prototype; revisit on physical-device performance testing.
            PlayerSettings.Android.optimizedFramePacing = false;
            // API35's ARM translation intermittently crashes in Vulkan debug object labels.
            // This 2D renderer needs GLES3 only; keep the Android backend deterministic.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            // Keep release versions in ProjectSettings; configuring mobile must not reset them.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("GATEFALL MOBILE SETTINGS: landscape, ARM64/IL2CPP Android, iOS, identifiers configured. Device builds require installed modules.");
        }

        // The HUD is created entirely at runtime, so no scene material keeps this shader in a player build.
        private static void IncludeRuntimeHudShader()
        {
            Shader shader = Shader.Find("UI/Default");
            if (shader == null) throw new BuildFailedException("uGUI's UI/Default shader is unavailable.");
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets.Length != 1) throw new BuildFailedException("Graphics settings could not be loaded.");
            var settings = new SerializedObject(assets[0]);
            SerializedProperty shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null || !shaders.isArray) throw new BuildFailedException("Always included shader list is unavailable.");
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            int index = shaders.arraySize++;
            shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("GATEFALL HUD SHADER: UI/Default retained for runtime-created canvas.");
        }

        public static void PrepareMobileProject() { ConfigureMobile(); ValidateProject(); }
        // v0.5 art (Tools/art/generate_all.py). Non-power-of-two sizes must be kept: sprites use pixel PPU.
        private static void ImportArtwork()
        {
            string folder = "Assets/Resources/Art/V5";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            if (guids.Length < 70) throw new BuildFailedException("Missing generated v0.5 artwork in " + folder + " (" + guids.Length + "). Run Tools/art/generate_all.py.");
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool large = name.StartsWith("stage-") || name == "district-map";
                bool crisp = name.StartsWith("ui-") || name.StartsWith("icon-");
                int size = large ? 4096 : 1024;
                TextureImporterCompression compression = crisp ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                if (importer.maxTextureSize != size || importer.mipmapEnabled || importer.alphaIsTransparency == large || importer.npotScale != TextureImporterNPOTScale.None ||
                    importer.wrapMode != TextureWrapMode.Clamp || importer.textureCompression != compression || importer.textureType != TextureImporterType.Default) {
                    importer.textureType = TextureImporterType.Default; importer.textureShape = TextureImporterShape.Texture2D;
                    importer.maxTextureSize = size; importer.mipmapEnabled = false; importer.alphaIsTransparency = !large;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear; importer.textureCompression = compression;
                    importer.SaveAndReimport();
                }
            }
            string[] v6guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art/V6" });
            if (v6guids.Length < guids.Length) throw new BuildFailedException("Incomplete v0.6 art replacement: " + v6guids.Length + " of " + guids.Length);
            foreach (string guid in v6guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool large = name.StartsWith("stage-") || name == "district-map";
                int size = name == "enemy-grunt" || name == "enemy-runner" ? 64 : name == "enemy-brute" || name == "enemy-siege" ? 128 : name == "enemy-boss" ? 256 : large ? 4096 : 1024;
                if (importer.maxTextureSize != size || importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None || importer.alphaIsTransparency == large) {
                    importer.textureType = TextureImporterType.Default; importer.maxTextureSize = size;
                    importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                    importer.alphaIsTransparency = !large; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                }
            }
            string[] v12guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art/V12" });
            foreach (string name in new[] { "district-ground", "district-blocks", "asphalt", "district-training-ground" })
                if (Resources.Load<Texture2D>("Art/V12/" + name) == null) throw new BuildFailedException("Missing terrain art: " + name);
            foreach (string guid in v12guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Default; importer.maxTextureSize = name == "district-training-ground" ? 512 : 2048;
                importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            Texture2D trainingGround = Resources.Load<Texture2D>("Art/V12/district-training-ground");
            if (trainingGround.width > 512 || trainingGround.height > 512) throw new BuildFailedException("Training ground import exceeds512");
            Debug.Log("GATEFALL TERRAIN district-training-ground width=" + trainingGround.width + " height=" + trainingGround.height + " max=512");
            foreach (string guid in guids) {
                string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (Resources.Load<Texture2D>("Art/V6/" + name) == null) throw new BuildFailedException("Missing replacement art: " + name);
            }
            foreach (string name in new[] { "stage-0", "stage-5", "tower-spear-1", "enemy-boss", "hunter-0", "ui-panel", "icon-stone", "district-map" }) {
                Texture2D texture = Resources.Load<Texture2D>("Art/V5/" + name);
                if (texture == null) throw new BuildFailedException("Artwork is not loadable as Texture2D: " + name);
                if (name == "stage-0" && texture.width != 2592) throw new BuildFailedException("Stage art was rescaled on import: " + texture.width);
            }
            foreach (string name in new[] { "stage-0", "stage-1", "stage-2", "stage-3", "stage-4", "stage-5", "tower-spear-1", "tower-arc-1", "enemy-boss", "hunter-0", "core" })
                if (Resources.Load<Texture2D>("Art/V6/" + name) == null) throw new BuildFailedException("Missing v0.6 artwork: " + name);
        }

        [MenuItem("Gatefall/Mobile/Build Android APK")]
        public static void BuildAndroidApk()
        {
            RequireTarget(BuildTargetGroup.Android, BuildTarget.Android, "Android Build Support + SDK/NDK + OpenJDK");
            PrepareMobileProject();
            bool previousBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                BuildAt("Builds/Android/Gatefall.apk", BuildTarget.Android, BuildOptions.Development);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previousBundle; }
        }

        [MenuItem("Gatefall/Mobile/Export iOS Xcode Project")]
        public static void ExportIos()
        {
            RequireTarget(BuildTargetGroup.iOS, BuildTarget.iOS, "iOS Build Support");
            PrepareMobileProject();
            // A new timestamped directory preserves any manually edited earlier Xcode export.
            BuildAt("Builds/iOS/Gatefall-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), BuildTarget.iOS, BuildOptions.None);
        }

        [MenuItem("Gatefall/Mobile/Build Desktop Touch Preview")]
        public static void BuildMobilePreview()
        {
            PrepareMobileProject();
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            BuildAt("Builds/MobilePreview/Gatefall.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        private static void RequireTarget(BuildTargetGroup group, BuildTarget target, string modules)
        {
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new BuildFailedException("Install " + modules + " for this Editor in Unity Hub > Installs > Add modules.");
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new BuildFailedException("Switch to " + target + " in File > Build Profiles first, then repeat this build menu. CLI: use -buildTarget.");
        }
        private static void BuildAt(string destination, BuildTarget target, BuildOptions options)
        {
            string path = Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = path, target = target, options = options
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + report.summary.result);
            Debug.Log("GATEFALL BUILD: " + path);
        }

        [MenuItem("Gatefall/Open Prototype")]
        public static void OpenPrototype()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Gatefall/Validate Project")]
        public static void ValidateProject()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before validation.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) throw new Exception("Prototype scene is missing.");
            Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
            if (font == null) throw new Exception("Bundled Korean font failed to import.");
            font.RequestCharactersInTexture("헌터결계방어마석", 20);
            if (!font.HasCharacter('헌')) throw new Exception("Korean glyph unavailable.");
            foreach (string weight in new[] { "Fonts/NanumGothic-Bold", "Fonts/NanumGothic-ExtraBold" })
                if (Resources.Load<Font>(weight) == null) throw new Exception("Missing bundled font weight: " + weight);
            ValidateBaseTowerArt();
            ValidateWaveHeaderFont();
            ValidateBattleModeFont();
            ValidateObserverMenuFont();
            ValidateGrowthHintFont();
            AuditCityBriefingFont();
            ValidateFirstCombatReactionFrame();
            ValidateSplashStatFont(font);
            if (Shader.Find("Sprites/Default") == null) throw new Exception("Built-in sprite shader unavailable. Use built-in rendering.");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!alreadyOpen) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                int controllers = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    controllers += root.GetComponentsInChildren<GatefallGame>(true).Length;
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                            throw new Exception("Missing script on " + child.name);
                }
                if (controllers != 1) throw new Exception("Expected exactly one GatefallGame in prototype scene.");
            }
            finally { if (!alreadyOpen) EditorSceneManager.CloseScene(scene, true); }

            DefenseSimulation model = new DefenseSimulation(new GameBalance { SpawnEnemies = false, OfferCards = false }, 7);
            model.Start(); string reason;
            if (!model.TryBuild(4, out reason)) throw new Exception("Construction integration failed: " + reason);
            var lab = new DefenseSimulation(new GameBalance { SpawnEnemies = false, OfferCards = false }, 7, new CampaignBattle());
            if (!lab.TryBuild(7, TowerKind.Autocannon, "remote", out reason)) throw new Exception("Remote pad construction failed: " + reason);
            model.Tick(180, new V2());
            if (model.State != RunState.Won) throw new Exception("Core simulation failed in Unity runtime.");
            var campaign=new CampaignService(new CampaignProfile(),p=>CampaignStore.Decode(CampaignStore.Encode(p)));
            string run;
            if(!campaign.Begin(0,false,out run))throw new Exception("Campaign preparation integration failed");
            var raon=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false},7,campaign.Battle(0,false));
            // Reward integration fixture has no scheduled combat; campaign wins now require completed waves.
            raon.Schedule.Clear();
            raon.Start();raon.Tick(180,new V2());int reward;
            if(!campaign.Complete(run,raon,out reward) || reward!=10 || !campaign.Profile.IsStageOpen(1))throw new Exception("Campaign reward integration failed");
            Debug.Log("GATEFALL VALIDATION PASSED: scene, script references, Korean font, sprite shader, and combat model. Play mode still needs manual inspection.");
        }

        [Serializable] private sealed class WaveFontReport {
            public string[] labels = {"웨이브 6/6","잔여 적 1088","잔여 적 9999"};
            public float[] widths = new float[3], heights = new float[3];
            public float compactWidth = 150;
        }
        private static void ValidateBaseTowerArt()
        {
            var art = new GatefallArt();
            foreach (TowerKind kind in new[] { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Barrier, TowerKind.Marker }) {
                for (int level = 1; level <= 2; level++) {
                    string key = GatefallArt.TowerKeys[(int)kind];
                    Texture2D expected = Resources.Load<Texture2D>("Art/V12/tower-" + key + "-" + level);
                    Sprite field = art.Tower(kind, level);
                    if (expected == null || expected.width != 256 || expected.height != 320 || field == null ||
                        field.texture != expected || art.TowerIcon(kind, level) != expected ||
                        !Mathf.Approximately(field.pixelsPerUnit, 86) || Mathf.Abs(field.pivot.y - .181f * 320) > .01f)
                        throw new BuildFailedException("Base tower art mismatch: " + kind + " level " + level);
                }
            }
            if (art.TowerIcon(TowerKind.Beacon, 1) != Resources.Load<Texture2D>("Art/V6/tower-marker-1"))
                throw new BuildFailedException("Beacon must retain its distinct texture");
            Debug.Log("GATEFALL TOWER ART: 5 families x 2 levels use the new field/menu textures; pivot, scale and Beacon checked.");
        }

        private static void ValidateWaveHeaderFont()
        {
            var report=new WaveFontReport();
            var style=new GUIStyle {font=Resources.Load<Font>("Fonts/NanumGothic-ExtraBold"),fontSize=24,wordWrap=true,padding=new RectOffset()};
            for(int i=0;i<report.labels.Length;i++) {
                style.font.RequestCharactersInTexture(report.labels[i],24,FontStyle.Normal);
                var content=new GUIContent(report.labels[i]);
                report.widths[i]=style.CalcSize(content).x;report.heights[i]=style.CalcHeight(content,150);
                if(report.widths[i]+1.5f>150 || report.heights[i]>32)throw new BuildFailedException("Compact wave label wraps: "+report.labels[i]);
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/wave-header-font.json",JsonUtility.ToJson(report,true));
            Debug.Log("GATEFALL WAVE HEADER FONT PASSED:24pt labels including4-digit remaining count fit150x32.");
        }

        [Serializable] private sealed class BattleModeFontReport {
            public string version;
            public string[] labels = {"도시 결계", "위기 · 도시 결계"};
            public float[] widths = new float[2], heights = new float[2];
        }
        private static void ValidateBattleModeFont()
        {
            var report = new BattleModeFontReport { version = Application.version };
            var style = new GUIStyle { font = Resources.Load<Font>("Fonts/NanumGothic-Regular"), fontSize = 13, wordWrap = true, padding = new RectOffset() };
            for (int i = 0; i < report.labels.Length; i++) {
                style.font.RequestCharactersInTexture(report.labels[i], 13, FontStyle.Normal);
                var content = new GUIContent(report.labels[i]);
                report.widths[i] = style.CalcSize(content).x; report.heights[i] = style.CalcHeight(content, 120);
                if (report.widths[i] + 1.5f > 120 || report.heights[i] > 18)
                    throw new BuildFailedException("Battle mode label wraps: " + report.labels[i]);
            }
            File.WriteAllText("TestResults/battle-mode-font.json", JsonUtility.ToJson(report, true));
            Debug.Log("GATEFALL BATTLE MODE FONT PASSED: normal/crisis core labels fit existing120x18.");
        }

        private static void ValidateSplashStatFont(Font font)
        {
            var method = typeof(MobileHud).GetMethod("StatLine",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null) throw new BuildFailedException("Tower stat formatter is missing.");
            var format = (Func<TowerKind, TowerStats, TowerStats, string>)method.CreateDelegate(
                typeof(Func<TowerKind, TowerStats, TowerStats, string>));
            var style = new GUIStyle { font = font, fontSize = 14, wordWrap = true, padding = new RectOffset() };
            var game = new DefenseSimulation(new GameBalance(), 7,
                new CampaignBattle { Stage = CampaignCatalog.Stages[13], Research = new[] { 3, 3, 3, 3 } });
            const int width = 374;
            float widest = 0;
            int labels = 0;
            foreach (int copies in new[] { 0, 2 }) {
                game.Cards[(int)CardKind.Ballistic] = copies;
                game.Cards[(int)CardKind.Blast] = copies;
                game.Cards[(int)CardKind.Range] = copies;
                foreach (TowerKind kind in new[] { TowerKind.Shatter, TowerKind.Seal }) {
                    int cap = game.TowerLevelCap(kind);
                    for (int level = 1; level <= cap; level++) {
                        TowerStats current = game.Stats(kind, level), next = level < cap ? game.Stats(kind, level + 1) : null;
                        string stats = format(kind, current, next);
                        string damage = current.Damage.ToString("0"), splash = current.Secondary.ToString("0");
                        if (next != null) { damage += "→" + next.Damage.ToString("0"); splash += "→" + next.Secondary.ToString("0"); }
                        if (!stats.StartsWith("직격 " + damage + " · 폭발 " + splash))
                            throw new BuildFailedException("Splash stat differs from the combat model: " + stats);
                        string text = stats + " · 사거리 " + current.Range.ToString("0.#");
                        font.RequestCharactersInTexture(text, style.fontSize, FontStyle.Normal);
                        var label = new GUIContent(text);
                        float measured = style.CalcSize(label).x;
                        if (measured + 1.5f > width || style.CalcHeight(label, width) > 22)
                            throw new BuildFailedException("Splash stat wraps: " + text);
                        widest = Mathf.Max(widest, measured); labels++;
                    }
                }
            }
            Debug.Log("GATEFALL SPLASH FONT: " + labels + " labels, max " + widest.ToString("F2") + "/" + width + ".");
        }

        private static void ValidateFirstCombatReactionFrame()
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var method = typeof(ArenaView).GetMethod("DecayAfterEventFrame", flags);
            if (method == null) throw new BuildFailedException("Shared reaction step is missing.");
            var advance = (Func<float, int, float, float>)method.CreateDelegate(typeof(Func<float, int, float, float>));
            int frame = Time.frameCount;
            // A 100 ms render frame at 2× used to consume all of a just-created recoil.
            if (!Mathf.Approximately(advance(1, frame, .2f * 7), 1) ||
                !Mathf.Approximately(advance(1, frame - 1, .05f * 7), .65f) ||
                !Mathf.Approximately(advance(.65f, frame - 1, 0), .65f) ||
                advance(1, frame - 1, .2f * 7) != 0)
                throw new BuildFailedException("Recoil first frame, normal decay or pause changed.");
            Debug.Log("GATEFALL RECOIL FRAME: new 0.20s=1.00; existing 0.05s=0.65; paused=0.65; existing 0.20s=0.00.");
            foreach (float duration in new[] { .15f, .18f })
                if (advance(duration, frame, .2f) != duration || advance(duration, frame - 1, .2f) != 0 ||
                    advance(duration, frame - 1, 0) != duration)
                    throw new BuildFailedException("Hunter or core hit flash first frame changed.");

            var strength = (Func<float, Enemy, float>)typeof(ArenaView).GetMethod("EnemyHitStrength", flags)
                .CreateDelegate(typeof(Func<float, Enemy, float>));
            Enemy enemy = ExpiredEnemyHitFixture();
            if (strength(220, enemy) != 1 || strength(160, enemy) != 0 || strength(100, enemy) != 0)
                throw new BuildFailedException("Enemy hit did not reach exactly one fallback frame.");
            enemy.HitFlash = .0325f;
            if (!Mathf.Approximately(strength(220, enemy), .5f))
                throw new BuildFailedException("Normal enemy hit decay changed.");
            Debug.Log("GATEFALL HIT FRAME: expired-timer HP220->160 visible=1, next=0; ordinary=.5; hunter/core new .20s retained, old expired, pause retained.");
        }

        [Serializable] private sealed class CityBriefingFontRow
        {
            public string stage, text;
            public float height;
            public float[] lineWidths;
            public string deploymentLesson, alternateLesson;
            public float storyHeight, deploymentHeight, alternateHeight;
        }

        [Serializable] private sealed class CityBriefingFontReport
        {
            public string version;
            public CityBriefingFontRow[] rows;
        }

        public static void AuditCityBriefingFont()
        {
            const int size = 15, width = 344, height = 50;
            var style = new GUIStyle {
                font = Resources.Load<Font>("Fonts/NanumGothic-Bold"),
                fontSize = size, wordWrap = true, padding = new RectOffset()
            };
            var notes = new GUIStyle(style) { font = Resources.Load<Font>("Fonts/NanumGothic-Regular") };
            var standard = new CampaignProfile();
            var tracking = new CampaignProfile { Loadout = (1 << (int)TowerKind.Marker) | (1 << (int)TowerKind.Binding) };
            tracking.FirstCleared[9] = true;
            int stageCount = CampaignCatalog.Stages.Length;
            var rows = new CityBriefingFontRow[stageCount + 2];
            for (int i = 0; i < stageCount; i++) {
                CampaignStage stage = CampaignCatalog.Stages[i];
                var row = MeasureCityBriefing(style, CampaignCatalog.StageId(i), stage.CityBriefing(standard), width);
                rows[i] = row;
                if (stage.Index >= 3) {
                    string story = stage.Story.Replace('\n', ' ');
                    notes.fontSize = 15;
                    notes.font.RequestCharactersInTexture(story, 15, FontStyle.Normal);
                    row.storyHeight = notes.CalcHeight(new GUIContent(story), width);
                    notes.fontSize = 14;
                    row.deploymentLesson = stage.DeploymentLesson(standard).Replace(". ", ".\n");
                    notes.font.RequestCharactersInTexture(row.deploymentLesson, 14, FontStyle.Normal);
                    row.deploymentHeight = notes.CalcHeight(new GUIContent(row.deploymentLesson), width);
                    if (stage.Index == 10) {
                        row.alternateLesson = stage.DeploymentLesson(tracking).Replace(". ", ".\n");
                        notes.font.RequestCharactersInTexture(row.alternateLesson, 14, FontStyle.Normal);
                        row.alternateHeight = notes.CalcHeight(new GUIContent(row.alternateLesson), width);
                    }
                }
            }
            var replay = standard.Copy();
            for (int i = 0; i <= 13; i++) { replay.FirstCleared[i] = true; replay.BestStars[i] = 3; }
            CampaignStage final = CampaignCatalog.Stages[13];
            rows[stageCount] = MeasureCityBriefing(style, "1-14 replay without materials", final.CityBriefing(replay), width);
            replay.Loadout = (1 << (int)TowerKind.Marker) | (1 << (int)TowerKind.Autocannon);
            rows[stageCount + 1] = MeasureCityBriefing(style, "1-14 replay with materials", final.CityBriefing(replay), width);
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/city-briefing-font-v" + Application.version.Replace(".", "") + ".json",
                JsonUtility.ToJson(new CityBriefingFontReport { version = Application.version, rows = rows }, true));
            foreach (CityBriefingFontRow row in rows) {
                if (row.lineWidths.Length > 2 || row.height > height)
                    throw new BuildFailedException("City briefing exceeds two lines: " + row.stage);
                if (row.storyHeight > 80 || row.deploymentHeight > 70 || row.alternateHeight > 70)
                    throw new BuildFailedException("City story or deployment lesson exceeds its panel: " + row.stage);
                foreach (float lineWidth in row.lineWidths)
                    if (lineWidth + 1.5f > width) throw new BuildFailedException("City briefing wraps a planned line: " + row.stage);
            }
            Debug.Log("GATEFALL CITY BRIEFING FONT: " + rows.Length + " briefings measured at 344x50.");
        }

        private static CityBriefingFontRow MeasureCityBriefing(GUIStyle style, string stage, string text, int width)
        {
            string[] lines = text.Split('\n');
            style.font.RequestCharactersInTexture(text, style.fontSize, FontStyle.Normal);
            var row = new CityBriefingFontRow {
                stage = stage, text = text, height = style.CalcHeight(new GUIContent(text), width),
                lineWidths = new float[lines.Length]
            };
            for (int i = 0; i < lines.Length; i++)
                row.lineWidths[i] = style.CalcSize(new GUIContent(lines[i])).x;
            return row;
        }

        [Serializable] private sealed class GrowthHintFontReport {
            public int labels;
            public float width=344,height=22,maxWidth,maxHeight;
            public string widest;
        }
        private static void ValidateGrowthHintFont()
        {
            var report=new GrowthHintFontReport();
            var style=new GUIStyle {font=Resources.Load<Font>("Fonts/NanumGothic-Bold"),fontSize=13,wordWrap=true,padding=new RectOffset()};
            style.font.RequestCharactersInTexture("권장 성장 · 창 봉인 붕괴 방벽 연구 (현재 / )0123456공격형결계주를함께편성하세요",13,FontStyle.Normal);
            Action<string> measure=text=>{
                var content=new GUIContent(text);float width=style.CalcSize(content).x,height=style.CalcHeight(content,report.width);report.labels++;
                if(width>report.maxWidth){report.maxWidth=width;report.widest=text;}report.maxHeight=Mathf.Max(report.maxHeight,height);
                if(width>report.width || height>report.height)throw new BuildFailedException("Growth hint wraps: "+text);
            };
            for(int stage=6;stage<14;stage++)for(int hunter=0;hunter<3;hunter++){
                int growth=CampaignCatalog.RecommendedHunterGrowth(stage,hunter);
                measure("권장 성장 "+growth+" · 공격형 결계주를 함께 편성하세요");
                for(int currentGrowth=0;currentGrowth<=6;currentGrowth++)for(int focus=0;focus<4;focus++)for(int current=0;current<=3;current++)
                    measure("권장 성장 "+growth+" · "+CampaignCatalog.ResearchNames[focus].Split('·')[0].Trim()+" 연구 "+CampaignCatalog.Stages[stage].RecommendedResearch[focus]+"  (현재 "+currentGrowth+" / "+current+")");
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/growth-hint-font-v"+Application.version.Replace(".","")+".json",JsonUtility.ToJson(report,true));
            Debug.Log("GATEFALL GROWTH HINT FONT PASSED: "+report.labels+" labels fit344x22.");
        }

        [Serializable] private sealed class ObserverFontReport {
            public string[] labels = {"피해 58→77 · 사거리 5.2","피해 999→999 · 사거리 9.9","표식 우선 3초 · 범위 적 9999 · 공명 +5%","표식 우선 3초"};
            public float[] widths = new float[4], heights = new float[4];
            public float availableWidth = 240;
        }
        private static void ValidateObserverMenuFont()
        {
            var report=new ObserverFontReport();
            for(int i=0;i<report.labels.Length;i++) {
                int size=i<2?14:13;
                var style=new GUIStyle {font=Resources.Load<Font>(i<2?"Fonts/NanumGothic-Regular":"Fonts/NanumGothic-Bold"),fontSize=size,wordWrap=true,padding=new RectOffset()};
                style.font.RequestCharactersInTexture(report.labels[i],size,FontStyle.Normal);
                var content=new GUIContent(report.labels[i]);
                report.widths[i]=style.CalcSize(content).x;report.heights[i]=style.CalcHeight(content,240);
                if(report.widths[i]>240 || report.heights[i]>(i<2?22:18))throw new BuildFailedException("Observer menu label wraps: "+report.labels[i]);
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/observer-menu-font.json",JsonUtility.ToJson(report,true));
            Debug.Log("GATEFALL OBSERVER MENU FONT PASSED:damage/upgrade and compact role lines fit240px.");
        }

        [MenuItem("Gatefall/Build Windows")]
        public static void BuildWindows()
        {
            ValidateProject();
            PlayerSettings.companyName = "Gatefall Lab";
            PlayerSettings.productName = "Gatefall - Last District";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            string target = Path.GetFullPath("Builds/Windows/Gatefall.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = target,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("GATEFALL BUILD: " + target);
        }
    }
}
