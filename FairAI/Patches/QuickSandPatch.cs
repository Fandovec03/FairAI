﻿using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;

namespace FairAI.Patches
{
    internal class QuickSandPatch
    {
        public static bool OnTriggerStayPatch(ref QuicksandTrigger __instance, Collider other)
        {
            if (__instance.isWater)
            {
                if (!other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<EnemyAICollisionDetect>() == null)
                {
                    return false;
                }
                if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                {
                    EnemyAICollisionDetect enemyAI = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (enemyAI != null)
                    {
                        EnemyAI ai = enemyAI.mainScript;
                        if (ai != null)
                        {
                            if (ai.agent != null)
                            {
                                NavMeshAgent agent = ai.agent;
                                float[] speeds = Plugin.SpeedAndAccelerationEnemyList(enemyAI);
                                float newSpeed = Plugin.GetInt("Quick Sand Config", "Slowing Speed") * 0.01f;
                                if (speeds[0] * newSpeed != agent.speed)
                                {
                                    agent.speed = speeds[0] * newSpeed;
                                }
                                if (speeds[1] * newSpeed != agent.acceleration)
                                {
                                    agent.acceleration = speeds[1] * newSpeed;
                                }
                            }
                            SetSinking(enemyAI);
                        }
                    }
                    return false;
                }
                else
                {
                    PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
                    if (component != GameNetworkManager.Instance.localPlayerController && component != null && component.underwaterCollider != __instance)
                    {
                        component.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                        return false;
                    }
                }
            }

            if (!__instance.isWater && !other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<EnemyAICollisionDetect>() == null)
            {
                return false;
            }

            PlayerControllerB component2 = other.gameObject.GetComponent<PlayerControllerB>();
            if (component2 != null)
            {
                if (component2 != GameNetworkManager.Instance.localPlayerController)
                {
                    return false;
                }

                if ((__instance.isWater && component2.isInsideFactory != __instance.isInsideWater) || component2.isInElevator)
                {
                    if (__instance.sinkingLocalPlayer)
                    {
                        __instance.StopSinkingLocalPlayer(component2);
                    }

                    return false;
                }

                if (__instance.isWater && !component2.isUnderwater)
                {
                    component2.underwaterCollider = __instance.gameObject.GetComponent<Collider>();
                    component2.isUnderwater = true;
                }

                component2.statusEffectAudioIndex = __instance.audioClipIndex;
                if (component2.isSinking)
                {
                    return false;
                }

                if (__instance.sinkingLocalPlayer)
                {
                    if (!component2.CheckConditionsForSinkingInQuicksand())
                    {
                        __instance.StopSinkingLocalPlayer(component2);
                    }
                }
                else if (component2.CheckConditionsForSinkingInQuicksand())
                {
                    Debug.Log("Set local player to sinking!");
                    __instance.sinkingLocalPlayer = true;
                    component2.sourcesCausingSinking++;
                    component2.isMovementHindered++;
                    component2.hinderedMultiplier *= __instance.movementHinderance;
                    if (__instance.isWater)
                    {
                        component2.sinkingSpeedMultiplier = 0f;
                    }
                    else
                    {
                        component2.sinkingSpeedMultiplier = __instance.sinkingSpeedMultiplier;
                    }
                }
            }
            else
            {
                if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null)
                {
                    EnemyAICollisionDetect enemyAI = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                    if (enemyAI != null)
                    {
                        EnemyAI ai = enemyAI.mainScript;
                        if (ai != null)
                        {
                            if (ai.agent != null)
                            {
                                NavMeshAgent agent = ai.agent;
                                float[] speeds = Plugin.SpeedAndAccelerationEnemyList(enemyAI);
                                float newSpeed = Plugin.GetInt("Quick Sand Config", "Slowing Speed") * 0.01f;
                                if (speeds[0] * newSpeed != agent.speed)
                                {
                                    agent.speed = speeds[0] * newSpeed;
                                }
                                if (speeds[1] * newSpeed != agent.acceleration)
                                {
                                    agent.acceleration = speeds[1] * newSpeed;
                                }
                            }
                            SetSinking(enemyAI);
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        public static void OnTriggerExitPatch(ref QuicksandTrigger __instance, Collider other)
        {
            if (other.gameObject.GetComponent<EnemyAICollisionDetect>() != null)
            {
                EnemyAICollisionDetect enemyAI = other.gameObject.GetComponent<EnemyAICollisionDetect>();
                EnemyAI ai = enemyAI.mainScript;
                float[] speeds = Plugin.SpeedAndAccelerationEnemyList(enemyAI);
                ai.agent.speed = speeds[0];
                ai.agent.acceleration = speeds[1];
                enemyAI.transform.position = new Vector3(enemyAI.transform.position.x, Plugin.positions[enemyAI.GetInstanceID()].y, enemyAI.transform.position.z);
            }
        }

        public static void SetSinking(EnemyAICollisionDetect enemy)
        {
            if (!Plugin.positions.ContainsKey(enemy.GetInstanceID()))
            {
                Plugin.positions.Add(enemy.GetInstanceID(), enemy.transform.position);
            }

            if (!Plugin.sinkingValues.ContainsKey(enemy.GetInstanceID()))
            {
                Plugin.sinkingValues.Add(enemy.GetInstanceID(), 0);
            }
            float sinkingValue = Plugin.sinkingValues[enemy.GetInstanceID()];
            // Estimate height
            float height = enemy.GetComponent<Collider>().bounds.size.y;

            // Use height to scale sink distance or time
            float sinkSpeed = height * 0.5f;
            float sinkStep = Time.deltaTime * sinkSpeed;

            // Sink toward a target offset (e.g., fully underground)
            Vector3 targetPos = enemy.transform.position - Vector3.up * height;
            enemy.transform.position = Vector3.Lerp(enemy.transform.position, targetPos, sinkStep);

            sinkingValue = Mathf.Clamp(sinkingValue + Time.deltaTime * 0.75f, 0f, Plugin.GetFloat("Quick Sand Config", "Sink Time"));
            if (Plugin.sinkingValues[enemy.GetInstanceID()] >= Plugin.GetFloat("Quick Sand Config", "Sink Time") - 0.01f)
            {
                enemy.mainScript.KillEnemy();
            }
            if (enemy != null)
            {
                if (enemy.mainScript != null)
                {
                    if (!enemy.mainScript.isEnemyDead)
                    {
                        Plugin.sinkingValues[enemy.GetInstanceID()] = sinkingValue;
                    }
                }
            }
        }
    }
}
