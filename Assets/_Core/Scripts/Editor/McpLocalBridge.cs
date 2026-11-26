using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Level3Bamboo;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneLastTime.Editor
{
    [InitializeOnLoad]
    [McpForUnityTool("bamboo_character_tool")]
    public static class McpLocalBridge
    {
        private static readonly string ProjectRoot;
        private static readonly string ReqFilePath;
        private static readonly string RespFilePath;
        private static readonly string ReadyFilePath;
        private static bool isProcessingFileReq;
        private static Timer pumpTimer;
        private static MethodInfo dispatchMethod;

        static McpLocalBridge()
        {
            if (IsAssetImportWorker())
            {
                return;
            }

            ProjectRoot = Path.GetDirectoryName(Application.dataPath);
            string tempDir = Path.Combine(ProjectRoot, "Temp");
            ReqFilePath = Path.Combine(tempDir, "mcp_req.json");
            RespFilePath = Path.Combine(tempDir, "mcp_resp.json");
            ReadyFilePath = Path.Combine(tempDir, "mcp_bridge_ready.txt");

            Application.runInBackground = true;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            EditorApplication.delayCall += InitializeBridge;

            // Keep Unity player/editor loop pumping when backgrounded if a request is waiting or in Play Mode
            pumpTimer = new Timer(_ =>
            {
                try
                {
                    if (File.Exists(ReqFilePath))
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
                catch { }
            }, null, 50, 50);
        }

        private static bool IsAssetImportWorker()
        {
            try
            {
                var method = typeof(AssetDatabase).GetMethod(
                    "IsAssetImportWorkerProcess",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null && method.GetParameters().Length == 0)
                {
                    if (method.Invoke(null, null) is bool b && b)
                    {
                        return true;
                    }
                }
            }
            catch { }

            string cmd = Environment.CommandLine ?? string.Empty;
            return cmd.IndexOf("-importWorker", StringComparison.OrdinalIgnoreCase) >= 0
                || cmd.IndexOf("AssetImportWorker", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void InitializeBridge()
        {
            try
            {
                Application.runInBackground = true;
                EditorConfigurationCache.Instance.PinStdioForSession();
                CommandRegistry.Initialize();
                if (!StdioBridgeHost.IsRunning)
                {
                    StdioBridgeHost.StartAutoConnect();
                }

                var dispatcherType = typeof(StdioBridgeHost).Assembly.GetType("MCPForUnity.Editor.Services.Transport.TransportCommandDispatcher");
                if (dispatcherType != null)
                {
                    dispatchMethod = dispatcherType.GetMethod("ExecuteCommandJsonAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }

                File.WriteAllText(ReadyFilePath, $"READY port={StdioBridgeHost.GetCurrentPort()} time={DateTime.UtcNow:O}");
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(ReadyFilePath, $"ERROR {ex}");
                }
                catch { }
            }
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isPlaying)
            {
                if (EditorApplication.isPaused)
                {
                    EditorApplication.isPaused = false;
                }
                if (Time.timeScale <= 0.01f)
                {
                    Time.timeScale = 1f;
                }
                EditorApplication.QueuePlayerLoopUpdate();
            }

            if (isProcessingFileReq || !File.Exists(ReqFilePath))
            {
                return;
            }

            isProcessingFileReq = true;
            ProcessFileRequestAsync();
        }

        private static async void ProcessFileRequestAsync()
        {
            string reqText = null;
            try
            {
                reqText = File.ReadAllText(ReqFilePath);
                File.Delete(ReqFilePath);
            }
            catch
            {
                isProcessingFileReq = false;
                return;
            }

            string respJson;
            try
            {
                if (dispatchMethod == null)
                {
                    var dispatcherType = typeof(StdioBridgeHost).Assembly.GetType("MCPForUnity.Editor.Services.Transport.TransportCommandDispatcher");
                    dispatchMethod = dispatcherType?.GetMethod("ExecuteCommandJsonAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }

                CommandRegistry.Initialize();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var task = (Task<string>)dispatchMethod.Invoke(null, new object[] { reqText, cts.Token });
                respJson = await task.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                respJson = JsonConvert.SerializeObject(new
                {
                    status = "error",
                    error = ex.ToString()
                });
            }

            try
            {
                string tmpPath = RespFilePath + ".tmp";
                File.WriteAllText(tmpPath, respJson);
                if (File.Exists(RespFilePath))
                {
                    File.Delete(RespFilePath);
                }
                File.Move(tmpPath, RespFilePath);
            }
            catch { }
            finally
            {
                isProcessingFileReq = false;
            }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.ToString() ?? "status";
            try
            {
                switch (action)
                {
                    case "build_all":
                        return BuildAllCharacterAssetsAndScene();
                    case "player_status":
                        return GetPlayerStatus();
                    case "test_controller":
                        return TestController();
                    case "player_control":
                        return ControlPlayer(@params);
                    case "camera_status":
                        return GetCameraStatus();
                    case "camera_zoom":
                        return SetCameraZoom(@params);
                    default:
                        return new SuccessResponse("McpLocalBridge status", new
                        {
                            stdioPort = StdioBridgeHost.GetCurrentPort(),
                            isPlaying = EditorApplication.isPlaying,
                            activeScene = SceneManager.GetActiveScene().path
                        });
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"bamboo_character_tool ({action}) failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static object TestController()
        {
            string path = "Assets/Level3_Bamboo/Animations/Character/Player_Ryu.controller";
            var rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var mainObj = AssetDatabase.LoadMainAssetAtPath(path);

            var p = GameObject.Find("Player_Ryu");
            var anim = p != null ? p.GetComponent<Animator>() : null;
            var prevCtrl = anim != null ? anim.runtimeAnimatorController : null;

            if (anim != null && rac != null)
            {
                anim.runtimeAnimatorController = rac;
                EditorUtility.SetDirty(anim);
                if (p != null) EditorUtility.SetDirty(p);
            }

            return new SuccessResponse("TestController result", new
            {
                racFound = rac != null,
                racType = rac != null ? rac.GetType().FullName : "null",
                acFound = ac != null,
                acType = ac != null ? ac.GetType().FullName : "null",
                mainObjFound = mainObj != null,
                mainObjType = mainObj != null ? mainObj.GetType().FullName : "null",
                prevCtrlName = prevCtrl != null ? prevCtrl.name : "null",
                newCtrlName = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null"
            });
        }

        private static object BuildAllCharacterAssetsAndScene()
        {
            if (EditorApplication.isPlaying)
            {
                return new ErrorResponse("Cannot run build_all in Play Mode.");
            }

            const string spriteSheetPath = "Assets/Level3_Bamboo/Art/Character/Character 120x120.png";
            const string animDir = "Assets/Level3_Bamboo/Animations/Character";
            const string prefabDir = "Assets/Level3_Bamboo/Prefabs";
            const string scenePath = "Assets/Level3_Bamboo/Scenes/L3_A_BambooTrail.unity";

            EnsureFolderExists("Assets/Level3_Bamboo/Animations");
            EnsureFolderExists(animDir);
            EnsureFolderExists(prefabDir);

            // 1. Force reimport of Character 120x120.png
            AssetDatabase.ImportAsset(spriteSheetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
            var spritesByName = allAssets
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);

            if (spritesByName.Count < 60)
            {
                return new ErrorResponse($"Expected 60 sprites in {spriteSheetPath}, loaded {spritesByName.Count}.");
            }

            Sprite[] GetSprites(int startIdx, int endIdxInclusive)
            {
                var list = new List<Sprite>(endIdxInclusive - startIdx + 1);
                for (int i = startIdx; i <= endIdxInclusive; i++)
                {
                    string key = $"Character 120x120_{i}";
                    if (!spritesByName.TryGetValue(key, out Sprite sp))
                    {
                        throw new InvalidOperationException($"Missing sprite '{key}' in {spriteSheetPath}");
                    }
                    list.Add(sp);
                }
                return list.ToArray();
            }

            // 2. Create Animation Clips
            var idleClip = CreateOrUpdateSpriteClip($"{animDir}/Idle.anim", GetSprites(12, 21), 10f, loop: true);
            var runClip = CreateOrUpdateSpriteClip($"{animDir}/Run.anim", GetSprites(34, 49), 14f, loop: true);
            var jumpClip = CreateOrUpdateSpriteClip($"{animDir}/Jump.anim", GetSprites(22, 33), 10f, loop: false);
            var jumpRiseClip = CreateOrUpdateSpriteClip($"{animDir}/JumpRise.anim", GetSprites(22, 25), 10f, loop: false);
            var jumpFallClip = CreateOrUpdateSpriteClip($"{animDir}/JumpFall.anim", GetSprites(26, 33), 10f, loop: false);
            var attack1Clip = CreateOrUpdateSpriteClip($"{animDir}/Attack1.anim", GetSprites(0, 5), 12f, loop: false);
            var attack2Clip = CreateOrUpdateSpriteClip($"{animDir}/Attack2.anim", GetSprites(6, 11), 12f, loop: false);
            var deathClip = CreateOrUpdateSpriteClip($"{animDir}/Death.anim", GetSprites(50, 59), 10f, loop: false);

            // 3. Create or replace Animator Controller
            string controllerPath = $"{animDir}/Player_Ryu.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            // Set default parameter values
            var parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == "IsGrounded")
                {
                    parameters[i].defaultBool = true;
                }
            }
            controller.parameters = parameters;

            var rootSm = controller.layers[0].stateMachine;

            var idleState = rootSm.AddState("Idle", new Vector3(260, 110, 0));
            idleState.motion = idleClip;
            rootSm.defaultState = idleState;

            var runState = rootSm.AddState("Run", new Vector3(520, 110, 0));
            runState.motion = runClip;

            var jumpRiseState = rootSm.AddState("JumpRise", new Vector3(260, 260, 0));
            jumpRiseState.motion = jumpRiseClip;

            var jumpFallState = rootSm.AddState("JumpFall", new Vector3(520, 260, 0));
            jumpFallState.motion = jumpFallClip;

            var attack1State = rootSm.AddState("Attack1", new Vector3(260, -40, 0));
            attack1State.motion = attack1Clip;

            var attack2State = rootSm.AddState("Attack2", new Vector3(520, -40, 0));
            attack2State.motion = attack2Clip;

            var deathState = rootSm.AddState("Death", new Vector3(40, -140, 0));
            deathState.motion = deathClip;

            // Transitions:
            // Death (AnyState -> Death, uninterruptible, holds last frame until IsDead == false)
            var toDeath = rootSm.AddAnyStateTransition(deathState);
            toDeath.hasExitTime = false;
            toDeath.duration = 0f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

            var deathToIdle = deathState.AddTransition(idleState);
            deathToIdle.hasExitTime = false;
            deathToIdle.duration = 0f;
            deathToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

            // Attacks (AnyState -> Attack1 / Attack2 when not dead)
            var toAttack1 = rootSm.AddAnyStateTransition(attack1State);
            toAttack1.hasExitTime = false;
            toAttack1.duration = 0f;
            toAttack1.canTransitionToSelf = false;
            toAttack1.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            toAttack1.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack1.AddCondition(AnimatorConditionMode.Equals, 0f, "AttackIndex");

            var toAttack2 = rootSm.AddAnyStateTransition(attack2State);
            toAttack2.hasExitTime = false;
            toAttack2.duration = 0f;
            toAttack2.canTransitionToSelf = false;
            toAttack2.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            toAttack2.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack2.AddCondition(AnimatorConditionMode.Equals, 1f, "AttackIndex");

            ConfigureAttackExitTransitions(attack1State, idleState, runState, jumpFallState);
            ConfigureAttackExitTransitions(attack2State, idleState, runState, jumpFallState);

            // Grounded Movement: Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Grounded -> Airborne (JumpRise / JumpFall)
            var idleToJumpRise = idleState.AddTransition(jumpRiseState);
            idleToJumpRise.hasExitTime = false;
            idleToJumpRise.duration = 0f;
            idleToJumpRise.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            idleToJumpRise.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

            var runToJumpRise = runState.AddTransition(jumpRiseState);
            runToJumpRise.hasExitTime = false;
            runToJumpRise.duration = 0f;
            runToJumpRise.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            runToJumpRise.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

            var idleToJumpFall = idleState.AddTransition(jumpFallState);
            idleToJumpFall.hasExitTime = false;
            idleToJumpFall.duration = 0f;
            idleToJumpFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            idleToJumpFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            var runToJumpFall = runState.AddTransition(jumpFallState);
            runToJumpFall.hasExitTime = false;
            runToJumpFall.duration = 0f;
            runToJumpFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            runToJumpFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            // JumpRise -> JumpFall
            var riseToFall = jumpRiseState.AddTransition(jumpFallState);
            riseToFall.hasExitTime = false;
            riseToFall.duration = 0f;
            riseToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            // Airborne -> Grounded (Landing to Idle or Run)
            var riseToIdle = jumpRiseState.AddTransition(idleState);
            riseToIdle.hasExitTime = false;
            riseToIdle.duration = 0f;
            riseToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            riseToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var riseToRun = jumpRiseState.AddTransition(runState);
            riseToRun.hasExitTime = false;
            riseToRun.duration = 0f;
            riseToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            riseToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var fallToIdle = jumpFallState.AddTransition(idleState);
            fallToIdle.hasExitTime = false;
            fallToIdle.duration = 0f;
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            fallToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var fallToRun = jumpFallState.AddTransition(runState);
            fallToRun.hasExitTime = false;
            fallToRun.duration = 0f;
            fallToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            fallToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            EditorUtility.SetDirty(controller);

            // 4. Create zero-friction PhysicsMaterial2D
            string physMatPath = $"{animDir}/PlayerZeroFriction.physicsMaterial2D";
            var physMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(physMatPath);
            if (physMat == null)
            {
                physMat = new PhysicsMaterial2D("PlayerZeroFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                AssetDatabase.CreateAsset(physMat, physMatPath);
            }
            else
            {
                physMat.friction = 0f;
                physMat.bounciness = 0f;
                EditorUtility.SetDirty(physMat);
            }

            // 5. Open Scene L3_A_BambooTrail and configure Ground Collider + Player_Ryu + Prefab
            var activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.path, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Find Ground_Visual and ensure static BoxCollider2D is attached so player stands on grassy ground
            GameObject groundVisualGo = GameObject.Find("Ground_Visual");
            if (groundVisualGo == null)
            {
                throw new InvalidOperationException("Could not find Ground_Visual in L3_A_BambooTrail.");
            }

            var groundBox = groundVisualGo.GetComponent<BoxCollider2D>();
            if (groundBox == null)
            {
                groundBox = groundVisualGo.AddComponent<BoxCollider2D>();
            }
            // Ground_Visual has Tiled SpriteRenderer of size (124.025154, 1) and scale 1.3859 at y = -6.57.
            // With size (124.025154, 0.92) and offset (0, -0.04), top surface is at localY = +0.42 -> worldY = -6.57 + 0.42 * 1.3859 = -5.9879f
            groundBox.size = new Vector2(124.025154f, 0.92f);
            groundBox.offset = new Vector2(0f, -0.04f);
            groundBox.isTrigger = false;
            EditorUtility.SetDirty(groundBox);

            float groundTopWorldY = groundVisualGo.transform.TransformPoint(new Vector3(0f, groundBox.offset.y + groundBox.size.y * 0.5f, 0f)).y;

            // Find CHARACTERS group and Player_Ryu
            GameObject charactersGroup = null;
            foreach (var rootGo in activeScene.GetRootGameObjects())
            {
                if (rootGo.name.Trim() == "CHARACTERS")
                {
                    charactersGroup = rootGo;
                    break;
                }
            }

            GameObject playerGo = GameObject.Find("Player_Ryu");
            if (playerGo != null && PrefabUtility.IsPartOfPrefabInstance(playerGo))
            {
                PrefabUtility.UnpackPrefabInstance(playerGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            if (playerGo == null)
            {
                playerGo = new GameObject("Player_Ryu");
                if (charactersGroup != null)
                {
                    playerGo.transform.SetParent(charactersGroup.transform, false);
                }
            }
            else if (charactersGroup != null && playerGo.transform.parent != charactersGroup.transform)
            {
                playerGo.transform.SetParent(charactersGroup.transform, true);
            }

            playerGo.tag = "Player";
            playerGo.layer = 0; // Default layer
            playerGo.transform.localScale = Vector3.one;
            playerGo.transform.localRotation = Quaternion.identity;
            playerGo.transform.position = new Vector3(-4.5f, groundTopWorldY, 0f);

            var sr = GetOrAddComponent<SpriteRenderer>(playerGo);
            sr.sprite = spritesByName["Character 120x120_12"]; // Idle frame 0
            sr.color = Color.white;
            sr.flipX = false;
            sr.flipY = false;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;

            var rb = GetOrAddComponent<Rigidbody2D>(playerGo);
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.mass = 1f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.sharedMaterial = physMat;

            var col = GetOrAddComponent<BoxCollider2D>(playerGo);
            col.size = new Vector2(0.75f, 1.05f);
            col.offset = new Vector2(0f, 0.525f);
            col.isTrigger = false;
            col.sharedMaterial = physMat;

            var anim = GetOrAddComponent<Animator>(playerGo);
            var soAnim = new SerializedObject(anim);
            var propAnim = soAnim.FindProperty("m_Controller");
            if (propAnim != null)
            {
                propAnim.objectReferenceValue = controller;
                soAnim.ApplyModifiedProperties();
            }
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(anim);

            var playerCtrl = GetOrAddComponent<BambooPlayerController>(playerGo);
            var soPlayerCtrl = new SerializedObject(playerCtrl);
            var propAnimCtrl = soPlayerCtrl.FindProperty("animatorController");
            if (propAnimCtrl != null)
            {
                propAnimCtrl.objectReferenceValue = controller;
                soPlayerCtrl.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(playerCtrl);

            // Configure Main Camera with CameraFollow2D
            GameObject mainCamGo = GameObject.Find("Main Camera");
            if (mainCamGo != null)
            {
                var camFollow = GetOrAddComponent<CameraFollow2D>(mainCamGo);
                var soCam = new SerializedObject(camFollow);
                var propTarget = soCam.FindProperty("target");
                if (propTarget != null)
                {
                    propTarget.objectReferenceValue = playerGo.transform;
                }
                var propOffset = soCam.FindProperty("offset");
                if (propOffset != null)
                {
                    propOffset.vector3Value = new Vector3(0f, 3.38f, -10f);
                }
                var propSmooth = soCam.FindProperty("smoothTime");
                if (propSmooth != null)
                {
                    propSmooth.floatValue = 0.15f;
                }
                var propDefZoom = soCam.FindProperty("defaultZoom");
                if (propDefZoom != null)
                {
                    propDefZoom.floatValue = 5f;
                }
                var propMinZoom = soCam.FindProperty("minZoom");
                if (propMinZoom != null)
                {
                    propMinZoom.floatValue = 2.5f;
                }
                var propMaxZoom = soCam.FindProperty("maxZoom");
                if (propMaxZoom != null)
                {
                    propMaxZoom.floatValue = 5.8f;
                }
                var propZoomSpeed = soCam.FindProperty("zoomSpeed");
                if (propZoomSpeed != null)
                {
                    propZoomSpeed.floatValue = 3f;
                }
                soCam.ApplyModifiedProperties();
                EditorUtility.SetDirty(camFollow);
                EditorUtility.SetDirty(mainCamGo);
            }

            // 6. Save Prefab and connect scene instance
            string prefabPath = $"{prefabDir}/Player_Ryu.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(playerGo, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            return new SuccessResponse("Character animations, controller, prefab, and scene setup completed.", new
            {
                spriteCount = spritesByName.Count,
                groundTopWorldY,
                playerPosition = new { playerGo.transform.position.x, playerGo.transform.position.y, playerGo.transform.position.z },
                clips = new
                {
                    idleLength = idleClip.length,
                    runLength = runClip.length,
                    jumpLength = jumpClip.length,
                    jumpRiseLength = jumpRiseClip.length,
                    jumpFallLength = jumpFallClip.length,
                    attack1Length = attack1Clip.length,
                    attack2Length = attack2Clip.length,
                    deathLength = deathClip.length
                },
                controllerPath,
                prefabPath
            });
        }

        private static void ConfigureAttackExitTransitions(
            AnimatorState attackState,
            AnimatorState idleState,
            AnimatorState runState,
            AnimatorState jumpFallState)
        {
            var toIdle = attackState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1.0f;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            toIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var toRun = attackState.AddTransition(runState);
            toRun.hasExitTime = true;
            toRun.exitTime = 1.0f;
            toRun.duration = 0f;
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            toRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var toAir = attackState.AddTransition(jumpFallState);
            toAir.hasExitTime = true;
            toAir.exitTime = 1.0f;
            toAir.duration = 0f;
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string assetPath, Sprite[] sprites, float fps, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            bool isNew = false;
            if (clip == null)
            {
                clip = new AnimationClip();
                isNew = true;
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = fps;

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = sprites.Length / fps;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew)
            {
                AssetDatabase.CreateAsset(clip, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
                string name = Path.GetFileName(folderPath);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolderExists(parent);
                }
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        private static object GetPlayerStatus()
        {
            var playerGo = GameObject.Find("Player_Ryu");
            if (playerGo == null)
            {
                return new ErrorResponse("Player_Ryu not found in scene.");
            }

            var sr = playerGo.GetComponent<SpriteRenderer>();
            var rb = playerGo.GetComponent<Rigidbody2D>();
            var anim = playerGo.GetComponent<Animator>();
            var ctrl = playerGo.GetComponent<BambooPlayerController>();

            string currentStateName = "Unknown";
            float normalizedTime = 0f;
            int shortHash = 0;
            if (anim != null && anim.runtimeAnimatorController != null && anim.gameObject.activeInHierarchy)
            {
                var stInfo = anim.GetCurrentAnimatorStateInfo(0);
                normalizedTime = stInfo.normalizedTime;
                shortHash = stInfo.shortNameHash;
                string[] candidates = { "Idle", "Run", "JumpRise", "JumpFall", "Jump", "Attack1", "Attack2", "Death" };
                foreach (var c in candidates)
                {
                    if (stInfo.IsName(c) || stInfo.IsName("Base Layer." + c) || stInfo.shortNameHash == Animator.StringToHash(c))
                    {
                        currentStateName = c;
                        break;
                    }
                }
            }

            return new SuccessResponse("Player status", new
            {
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                timeScale = Time.timeScale,
                animControllerName = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null",
                animEnabled = anim != null && anim.enabled,
                animSpeed = anim != null ? anim.speed : -1f,
                animIsInitialized = anim != null && anim.isInitialized,
                stInfoShortHash = shortHash,
                position = new { x = playerGo.transform.position.x, y = playerGo.transform.position.y, z = playerGo.transform.position.z },
                velocity = rb != null ? new { x = rb.velocity.x, y = rb.velocity.y } : null,
                spriteName = sr != null && sr.sprite != null ? sr.sprite.name : "null",
                flipX = sr != null && sr.flipX,
                sortingLayer = sr != null ? sr.sortingLayerName : "",
                sortingOrder = sr != null ? sr.sortingOrder : 0,
                animatorState = currentStateName,
                stateNormalizedTime = normalizedTime,
                speedParam = anim != null ? anim.GetFloat("Speed") : 0f,
                isGroundedParam = anim != null && anim.GetBool("IsGrounded"),
                verticalVelocityParam = anim != null ? anim.GetFloat("VerticalVelocity") : 0f,
                attackIndexParam = anim != null ? anim.GetInteger("AttackIndex") : 0,
                isDeadParam = anim != null && anim.GetBool("IsDead"),
                ctrlIsGrounded = ctrl != null && ctrl.IsGrounded,
                ctrlIsAttacking = ctrl != null && ctrl.IsAttacking,
                ctrlIsDead = ctrl != null && ctrl.IsDead,
                ctrlLastAttackIndex = ctrl != null ? ctrl.LastAttackIndex : -1
            });
        }

        private static object ControlPlayer(JObject @params)
        {
            if (!EditorApplication.isPlaying)
            {
                return new ErrorResponse("Player control requires Play Mode.");
            }

            var playerGo = GameObject.Find("Player_Ryu");
            if (playerGo == null)
            {
                return new ErrorResponse("Player_Ryu not found in scene.");
            }

            var ctrl = playerGo.GetComponent<BambooPlayerController>();
            if (ctrl == null)
            {
                return new ErrorResponse("BambooPlayerController not found on Player_Ryu.");
            }

            string cmd = @params?["command"]?.ToString() ?? "";
            switch (cmd)
            {
                case "move":
                    float dir = @params?["direction"]?.Value<float>() ?? 0f;
                    ctrl.SetSimulatedHorizontal(dir);
                    break;
                case "stop_move":
                    ctrl.SetSimulatedHorizontal(0f);
                    break;
                case "clear_input":
                    ctrl.ClearSimulatedInput();
                    break;
                case "jump":
                    ctrl.SimulateJump();
                    break;
                case "attack":
                    ctrl.SimulateAttack();
                    break;
                case "die":
                    ctrl.TriggerDeath();
                    break;
                case "respawn":
                    ctrl.Respawn();
                    break;
                default:
                    return new ErrorResponse($"Unknown player_control command: {cmd}");
            }

            EditorApplication.QueuePlayerLoopUpdate();
            return GetPlayerStatus();
        }

        private static object GetCameraStatus()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null)
            {
                return new ErrorResponse("Main Camera not found.");
            }

            var cam = camGo.GetComponent<Camera>();
            var camFollow = camGo.GetComponent<CameraFollow2D>();
            var pGo = GameObject.Find("Player_Ryu");

            return new SuccessResponse("Camera status", new
            {
                position = new { x = camGo.transform.position.x, y = camGo.transform.position.y, z = camGo.transform.position.z },
                orthographicSize = cam != null ? cam.orthographicSize : 0f,
                targetZoom = camFollow != null ? camFollow.TargetZoom : (cam != null ? cam.orthographicSize : 0f),
                hasFollowScript = camFollow != null,
                targetName = camFollow != null && camFollow.Target != null ? camFollow.Target.name : "null",
                playerPos = pGo != null ? (object)new { x = pGo.transform.position.x, y = pGo.transform.position.y, z = pGo.transform.position.z } : null
            });
        }

        private static object SetCameraZoom(JObject @params)
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null)
            {
                return new ErrorResponse("Main Camera not found.");
            }

            var cam = camGo.GetComponent<Camera>();
            var camFollow = camGo.GetComponent<CameraFollow2D>();
            float zoom = @params?["zoom"]?.Value<float>() ?? 5f;

            if (camFollow != null)
            {
                camFollow.SetZoom(zoom);
            }
            else if (cam != null)
            {
                cam.orthographicSize = zoom;
            }

            EditorApplication.QueuePlayerLoopUpdate();
            return GetCameraStatus();
        }
    }
}
