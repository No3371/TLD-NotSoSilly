using Il2Cpp;
using UnityEngine;
using HarmonyLib;
using MelonLoader;
using System.Collections;
using UnityEngine.Analytics;
using Il2CppVLB;

namespace NotSoSillyMod
{
    public class NotSoSillyClass : MelonMod
    {
        public static bool toggle;
        public override void OnInitializeMelon()
        {
            Settings.OnLoad();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.toggle))
            {
                toggle = !toggle;
                MelonCoroutines.Start(Warning());
                MelonLogger.Msg("NotSoSilly: " + toggle);
            }

            IEnumerator Warning ()
            {
                InterfaceManager.GetPanel<Panel_HUD>().DisplayWarningMessage("NotSoSilly: " + toggle);
                yield return new WaitForSeconds(2);
                InterfaceManager.GetPanel<Panel_HUD>().ClearWarningMessage();
            }
        }




        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitMeshPlacement))]
        private static class DisablePhysicsCheck
        {
            internal static bool Prefix(PlayerManager __instance)
            {
                // damn it we can't do early return in prefixes
                GearItem m_ObjectToPlaceGearItem = __instance.m_ObjectToPlaceGearItem;
                if (toggle && __instance.m_ObjectToPlaceGearItem != null)
                {
                    GameObject go = m_ObjectToPlaceGearItem.gameObject;
                    if (m_ObjectToPlaceGearItem.IsAttachedToPlacePoint())
                    {
                        var p = go.GetComponent<TempPhysic>();
                        if (p != null) MonoBehaviour.Destroy(p);
                        return true;
                    }
                    // if (go == null) MelonLogger.Msg("No gameobject ?????");

                    var rg = go.GetComponentsInChildren<Rigidbody>(true);
                    Renderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);

                    if (rg != null && rg.Count > 0 && rg[0].gameObject != go)
                        MelonLogger.Msg("Gear has rigidbodies in child, unsupported, skipping");
                    else if ((renderers == null || renderers.Length == 0)
                          && ((renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) == null
                          || renderers.Length == 0)) // uuuuuhhhhhhh
                    {
                        
                        MelonLogger.Msg("Gear has no mesh");
                    }
                    else 
                    {
                        var p = go.GetComponent<TempPhysic>();
                        if (p != null) p.enabled = false;
                        else p = go.AddComponent<TempPhysic>();
                        p.Gear = m_ObjectToPlaceGearItem;
                        p.SetAt = Time.time;
                        p.meshes.Clear();
                        p.meshes.AddRange(renderers);
                        p.transform.Translate(Vector3.up * UnityEngine.Random.Range(0.015f, 0.05f));
                        p.transform.Rotate(UnityEngine.Random.Range(-Settings.options.rotationAngle, Settings.options.rotationAngle), UnityEngine.Random.Range(-Settings.options.rotationAngle, Settings.options.rotationAngle), UnityEngine.Random.Range(-Settings.options.rotationAngle, Settings.options.rotationAngle));
                        p.enabled = true;

                        // // General colliders are possible (ex: BoxCollider)
                        // // And if parent collider is availabe, use it
                        // p.Collider = go.GetComponent<Collider>();
                        // if (!p.Collider)
                        // {
                        //     p.Collider = mr[0].gameObject.GetComponent<Collider>();
                        //     if (!p.Collider)
                        //     {
                        //         p.TemporalColider = true;
                        //         p.Collider = mr[0].gameObject.AddComponent<MeshCollider>();
                                // MelonLogger.Msg("Adding MeshCollider");
                        //     }
                        // }

                        // if (p.Collider is MeshCollider mc && !p.TemporalColider)
                        // {
                        //     p.PreviousConvex = mc.convex;
                        //     mc.convex = true;
                        // }

                        // p.RigidBody = p.GetComponent<Rigidbody>();
                        // if (!p.RigidBody)
                        // {
                        //     p.RigidBody = p.gameObject.AddComponent<Rigidbody>();
                        //     p.TemporalRigidbody = true;
                        // }

                        // p.RigidBody.isKinematic = false;
                        // p.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                        // p.RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        // p.RigidBody.solverIterations = 30;
                    }
                }
                return true;
            }
        }

        [RegisterTypeInIl2Cpp]
        class TempPhysic : MonoBehaviour
        {
            internal GearItem Gear { get; set;}
            internal static float lastEnable;
            internal static int layer = LayerMask.NameToLayer("Gear");
            public TempPhysic(IntPtr intPtr) : base(intPtr) { }
            public bool TemporalColider { get; set; }
            public Collider Collider { get; set; }
            public bool PreviousConvex { get; set; }
            public Rigidbody RigidBody { get; set; }
            public bool TemporalRigidbody { get; set; }
            public float SetAt { get; set; }
            internal List<Renderer> meshes = new List<Renderer>();
            bool noOp;
            bool lastToggleOp = false;

            void OnEnable ()
            {
                if (lastToggleOp != false)
                    MelonLogger.Warning("Expecting disabled!");
                lastToggleOp = true;
                noOp = false;
                if ((Time.time - lastEnable) > 10f)
                {
                    // MelonLogger.Msg("Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                    Physics.IgnoreLayerCollision(layer, layer, false);
                    // MelonLogger.Msg("-Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                }
                lastEnable = Time.time;
                // General colliders are possible (ex: BoxCollider)
                // And if parent collider is availabe, use it
                this.Collider = gameObject.GetComponent<Collider>();
                if (!this.Collider)
                {
                    if (this.meshes != null && this.meshes.Count > 1)
                    {
                        if (!this.Collider)
                        {
                            this.TemporalColider = true;
                            this.Collider = meshes[0].gameObject.AddComponent<MeshCollider>();
                            // MelonLogger.Msg("Adding MeshCollider to " + this.gameObject.name);
                        }
                    }
                    else
                    {
                        var colliders = gameObject.GetComponentsInChildren<Collider>();
                        if (colliders == null || colliders.Length == 0)
                        {
                            MelonLogger.Msg("No usable collider, won't add physics");
                            noOp = true;
                            return;
                        }
                    }
                }

                if (this.Collider is MeshCollider mc && !this.TemporalColider)
                {
                    this.PreviousConvex = mc.convex;
                    mc.convex = true;
                }

                this.RigidBody = this.GetComponent<Rigidbody>();
                if (!this.RigidBody)
                {
                    // MelonLogger.Msg("Adding RigidBody to " + this.gameObject.name);
                    this.RigidBody = this.gameObject.AddComponent<Rigidbody>();
                    this.TemporalRigidbody = true;
                }

                this.RigidBody.isKinematic = false;
                this.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                this.RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                this.RigidBody.solverIterations = 60;

                // MelonLogger.Msg(string.Format("TempPhysics enabled: {0}, layer: {1}", this.gameObject.name, this.gameObject.layer));
                
                if (TemporalRigidbody)
                {
                    this.RigidBody.mass = Mathf.Max(1, this.RigidBody.mass * 2);
                    this.RigidBody.useGravity = true;
                }
            }
            
            void OnDisable ()
            {
                if (lastToggleOp != true)
                    MelonLogger.Warning("Expecting enabled!");
                lastToggleOp = false;
                if (noOp) return;
                // MelonLogger.Msg("TempPhysics disabled: " + this.gameObject?.name?? "???");
                if (NotSoSillyClass.toggle && (this.transform.position - GameManager.GetPlayerTransform().position).magnitude > 10f)
                {
                    this.transform.position = GameManager.GetPlayerTransform().position + Vector3.up;
                    MelonCoroutines.Start(Notification());
                }
                this.RigidBody.isKinematic = true;
                if (TemporalColider && Collider != null)
                    MonoBehaviour.DestroyImmediate(Collider);
                else
                    if (this.Collider is MeshCollider mc) mc.convex = PreviousConvex;

                if (TemporalRigidbody && RigidBody != null) 
                    MonoBehaviour.DestroyImmediate(RigidBody);

                if ((Time.time - lastEnable) > 13.5f) 
                {
                    // MelonLogger.Msg("Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                    Physics.IgnoreLayerCollision(layer, layer, true);
                    // MelonLogger.Msg("-Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                }
            }

            void Update ()
            {
                if (noOp) return;
                if (GameManager.GetPlayerManagerComponent().m_InspectModeActive)
                {
                    MelonLogger.Msg("Cancel TempPhysics on inspect");
                    this.enabled = false;
                    MonoBehaviour.DestroyImmediate(this);
                    return;
                }
                if (Time.time - SetAt > 14f)
                {
                    this.enabled = false;
                    MonoBehaviour.DestroyImmediate(this);
                    return;
                }
                else if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.retrieveKey))
                {
                    this.gameObject.GetComponent<Transform>().position = GameManager.GetPlayerTransform().position + Vector3.up * 0.25f;    
                    this.enabled = false;
                    MonoBehaviour.DestroyImmediate(this);
                    return;
                }
            }

            float lastPlayback;
            void OnCollisionEnter (Collision collision)
            {
                if (!Settings.options.audio) return;
                switch (collision.other.gameObject.layer)
                {
                    case 14: // Player
                    case 16: // NPC
                        return;
                }
                if (Time.time - lastPlayback < 0.65f) return;
                var r = UnityEngine.Random.Range(0f, 1f);
                if (Gear?.GearItemData == null) return;
                
                if (Gear.GearItemData.PickupAudio != null && r > 0.5d) GameAudioManager.PlaySound(Gear.GearItemData.PickupAudio, GameManager.GetPlayerObject());
                if (Gear.GearItemData.CookingSlotPlacementAudio != null && r > 0.4d) GameAudioManager.PlaySound(Gear.GearItemData.CookingSlotPlacementAudio, GameManager.GetPlayerObject());
                if (Gear.GearItemData.StowAudio != null && r > 0.3d) GameAudioManager.PlaySound(Gear.GearItemData.StowAudio, GameManager.GetPlayerObject());
                if (Gear.GearItemData.PutBackAudio != null && r > 0.2d) GameAudioManager.PlaySound(Gear.GearItemData.PutBackAudio, GameManager.GetPlayerObject());
                if (Gear.GearItemData.PickupAudio != null) GameAudioManager.PlaySound(Gear.GearItemData.PickupAudio, GameManager.GetPlayerObject());
                else if (Gear.GearItemData.PickupAudio != null) GameAudioManager.PlaySound(Gear.GearItemData.PickupAudio, GameManager.GetPlayerObject());
                else if (Gear.GearItemData.CookingSlotPlacementAudio != null && r > 0.4d) GameAudioManager.PlaySound(Gear.GearItemData.CookingSlotPlacementAudio, GameManager.GetPlayerObject());
                else if (Gear.GearItemData.StowAudio != null) GameAudioManager.PlaySound(Gear.GearItemData.StowAudio, GameManager.GetPlayerObject());
                else if (Gear.GearItemData.PutBackAudio != null) GameAudioManager.PlaySound(Gear.GearItemData.PutBackAudio, GameManager.GetPlayerObject());
                lastPlayback = Time.time;
            }

            IEnumerator Notification ()
            {
                InterfaceManager.GetPanel<Panel_HUD>().DisplayWarningMessage(string.Concat("NotSoSilly: ", this.gameObject.name, " is teleported back to you."));
                yield return new WaitForSeconds(2);
                InterfaceManager.GetPanel<Panel_HUD>().ClearWarningMessage();
            }
        }

    }
}
