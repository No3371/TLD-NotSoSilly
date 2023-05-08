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
        static bool toggle;
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
                MelonLogger.Msg("NotSoSilly: " + toggle);
            }
        }


        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.ExitMeshPlacement))]
        private static class DisablePhysicsCheck
        {
            internal static void Prefix(ref PlayerManager __instance)
            {
                // damn it we can't do early return in prefixes
                if (toggle)
                {
                    GameObject go = __instance.m_ObjectToPlaceGearItem.gameObject;
                    if (go == null) MelonLogger.Msg("No gameobject ?????");

                    var rg = go.GetComponentsInChildren<Rigidbody>();
                    var mr = go.GetComponentsInChildren<MeshRenderer>(true);

                    if (rg != null && rg.Count > 0 && rg[0].gameObject != go)
                        MelonLogger.Msg("Gear has rigidbodies in child, unsupported, skipping");
                    else if (mr == null || mr.Count == 0)
                        MelonLogger.Msg("Gear has no mesh");
                    else 
                    {

                        var p = go.GetComponent<TempPhysic>();
                        if (p != null) p.enabled = false;
                        else p = go.AddComponent<TempPhysic>();
                        p.SetAt = Time.time;
                        p.meshes = mr;
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
                        //         // MelonLogger.Msg("Adding MeshCollider");
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
            }
        }

        [RegisterTypeInIl2Cpp]
        class TempPhysic : MonoBehaviour
        {
            internal static float lastEnable;
            internal static int layer = LayerMask.NameToLayer("Gear");
            public TempPhysic(IntPtr intPtr) : base(intPtr) { }
            public bool TemporalColider { get; set; }
            public Collider Collider { get; set; }
            public bool PreviousConvex { get; set; }
            public Rigidbody RigidBody { get; set; }
            public bool TemporalRigidbody { get; set; }
            public float SetAt { get; set; }
            internal MeshRenderer[] meshes;
            bool noOp;

            void OnEnable ()
            {

                if ((Time.time - lastEnable) > 10.5f)
                {
                    MelonLogger.Msg("Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                    Physics.IgnoreLayerCollision(layer, layer, false);
                    MelonLogger.Msg("-Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                }
                lastEnable = Time.time;
                // General colliders are possible (ex: BoxCollider)
                // And if parent collider is availabe, use it
                this.Collider = gameObject.GetComponent<Collider>();
                if (!this.Collider)
                {
                    if (this.meshes != null && this.meshes.Length > 1)
                    {
                        if (!this.Collider)
                        {
                            this.TemporalColider = true;
                            this.Collider = meshes[0].gameObject.AddComponent<MeshCollider>();
                            // MelonLogger.Msg("Adding MeshCollider");
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
                    this.RigidBody = this.gameObject.AddComponent<Rigidbody>();
                    this.TemporalRigidbody = true;
                }

                this.RigidBody.isKinematic = false;
                this.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                this.RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                this.RigidBody.solverIterations = 60;

                MelonLogger.Msg(string.Format("TempPhysics enabled: {0}, layer: {1}", this.gameObject.name, this.gameObject.layer));
                
                if (TemporalRigidbody)
                {
                    this.RigidBody.mass = Mathf.Max(1, this.RigidBody.mass * 2);
                    this.RigidBody.useGravity = true;
                }
            }
            
            void OnDisable ()
            {
                if (noOp) return;
                MelonLogger.Msg("TempPhysics disabled: " + this.gameObject?.name?? "???");
                if ((this.transform.position - GameManager.GetPlayerTransform().position).magnitude > 10f)
                {
                    this.transform.position = GameManager.GetPlayerTransform().position + Vector3.up;
                }
                this.RigidBody.isKinematic = true;
                if (TemporalColider && Collider != null)
                    MonoBehaviour.DestroyImmediate(Collider);
                else
                    if (this.Collider is MeshCollider mc) mc.convex = PreviousConvex;

                if (TemporalRigidbody && RigidBody != null) 
                    MonoBehaviour.DestroyImmediate(RigidBody);

                if ((Time.time - lastEnable) > 9.5f) 
                {
                    
                    MelonLogger.Msg("Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                    Physics.IgnoreLayerCollision(layer, layer, true);
                    MelonLogger.Msg("-Layer2<->2: " + Physics.GetIgnoreLayerCollision(layer, layer));
                }
            }

            void Update ()
            {
                if (noOp) return;
                if (Time.time - SetAt > 10)
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
        }

    }
}
