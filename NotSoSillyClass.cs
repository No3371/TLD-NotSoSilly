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
                // MelonLogger.Msg("-DisablePhysicsCheck");
                if (toggle)
                {
                    MelonLogger.Msg("Enabled");
                    GameObject go = __instance.m_ObjectToPlaceGearItem.gameObject;
                    var p = go.GetOrAddComponent<TempPhysic>();
                    var mr = go.GetComponentInChildren<MeshRenderer>();
                    if (mr)
                    {
                        // General colliders are possible (ex: BoxCollider)
                        p.Collider = go.GetComponentInChildren<Collider>();
                        if (!p.Collider)
                        {
                            p.TemporalColider = true;
                            p.Collider = mr.gameObject.AddComponent<MeshCollider>();
                            // MelonLogger.Msg("Adding MeshCollider");
                        }
                        else if (p.Collider is MeshCollider mc)
                        {
                            p.PreviousConvex = mc.convex;
                            mc.convex = true;
                            // MelonLogger.Msg("Already got MeshCollider");
                        }

                        p.RigidBody = p.Collider.attachedRigidbody;
                        if (!p.RigidBody)
                        {
                            p.RigidBody = p.Collider.gameObject.AddComponent<Rigidbody>();
                            p.TemporalRigidBody = true;
                            // MelonLogger.Msg("Adding Rigidbody");
                        }

                        p.RigidBody.isKinematic = false;
                        p.RigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                        p.RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        p.RigidBody.solverIterations = 60;
                    }
                }
                // MelonLogger.Msg("DisablePhysicsCheck-");
            }

            // internal static void Postfix (ref PlayerManager __instance)
            // {
            //     GameObject go = __instance.m_ObjectToPlaceGearItem.gameObject;
            //     var p = go.AddComponent<TempPhysic>();
            //     Rigidbody rigidbody = go.GetComponent<Rigidbody>();
            //     go.GetComponent<MeshCollider>().
            // }
        }

        [RegisterTypeInIl2Cpp]
        class TempPhysic : MonoBehaviour
        {
            public TempPhysic(IntPtr intPtr) : base(intPtr) { }
            public bool TemporalColider { get; set; }
            public Collider Collider { get; set; }
            public bool PreviousConvex { get; set; }
            public bool TemporalRigidBody { get; set; }
            public Rigidbody RigidBody { get; set; }
            float bornAt;
            void Start ()
            {
                MelonLogger.Msg("TempPhysics started");
                bornAt = Time.time;
            }

            void Update ()
            {
                if (Time.time - bornAt > 5)
                {
                    Stop();
                    return;
                }
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.retrieveKey))
                {
                    this.RigidBody.velocity = Vector3.zero;
                    this.gameObject.GetComponent<Transform>().position = GameManager.GetPlayerTransform().position + Vector3.up * 0.25f;
                    Stop();
                }
            }

            void Stop ()
            {
                MelonLogger.Msg("TempPhysics destroyed");
                if ((this.gameObject.GetComponent<Transform>().position - GameManager.GetPlayerTransform().position).magnitude > 10f)
                {
                    this.gameObject.GetComponent<Transform>().position = GameManager.GetPlayerTransform().position + Vector3.up;
                }
                this.RigidBody.isKinematic = true;
                if (TemporalColider && Collider != null)
                    MonoBehaviour.Destroy(Collider);
                else
                    if (this.Collider is MeshCollider mc) mc.convex = PreviousConvex;

                if (TemporalRigidBody && RigidBody != null) MonoBehaviour.Destroy(this.RigidBody);

                MonoBehaviour.Destroy(this);
            }
        }

    }
}
