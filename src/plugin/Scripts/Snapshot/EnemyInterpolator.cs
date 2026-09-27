using Assets.Scripts.Actors.Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace MegabonkTogether.Scripts.Snapshot
{
    //TODO: Find a way to make abstract class work with IL2CPP because its not working for some reason ¯\_(ツ)_/¯
    public class EnemyInterpolator : MonoBehaviour
    {
        private Enemy enemy;

        private readonly List<EnemySnapshot> snapshotsBuffer = new List<EnemySnapshot>();

        protected float interpolationDelayMs = 0.1f;
        //Was 200, which is ~5s of history for a 0.1s interpolation delay. When the host falls behind,
        //CleanupOldSnapshots drops nothing and FindSnapshotPair rescans the whole buffer every frame
        //for every enemy while finding no pair at all. PlayerInterpolator and ProjectileInterpolator
        //already use 30 for the same delay
        protected int maxBufferSize = 30;

        protected void Update()
        {
            if (enemy == null || !HasEnoughSnapshots())
                return;

            double renderTime = Time.timeAsDouble - interpolationDelayMs;
            PerformInterpolation(renderTime);
            CleanupOldSnapshots(renderTime);
        }

        public void Initialize(Enemy enemy)
        {
            this.enemy = enemy;
        }

        public void AddSnapshot(EnemySnapshot snapshot)
        {
            snapshotsBuffer.Add(snapshot);

            if (snapshotsBuffer.Count > maxBufferSize)
            {
                snapshotsBuffer.RemoveAt(0);
            }
        }

        protected bool HasEnoughSnapshots()
        {
            return snapshotsBuffer.Count >= 2;
        }

        protected void PerformInterpolation(double renderTime)
        {
            if (!FindSnapshotPair(renderTime, out EnemySnapshot older, out EnemySnapshot newer))
                return;

            //This check used to sit after two dereferences of enemy.transform further down
            if (enemy == null || enemy.transform == null)
            {
                return;
            }

            enemy.hp = newer.Hp;

            float dist = Vector3.Distance(older.Position, newer.Position);
            if (dist > 2.0f)
            {
                enemy.transform.position = newer.Position;
                enemy.transform.rotation = newer.Rotation;
                return;
            }

            float t = CalculateInterpolationFactor(renderTime, older.Timestamp, newer.Timestamp);
            t = Mathf.Clamp01(t);

            enemy.transform.position = Vector3.Lerp(older.Position, newer.Position, t);
            enemy.transform.rotation = Quaternion.Slerp(older.Rotation, newer.Rotation, t);
        }

        private bool FindSnapshotPair(double renderTime, out EnemySnapshot older, out EnemySnapshot newer)
        {
            older = null;
            newer = null;

            for (int i = 0; i < snapshotsBuffer.Count - 1; i++)
            {
                if (snapshotsBuffer[i].Timestamp <= renderTime &&
                    snapshotsBuffer[i + 1].Timestamp >= renderTime)
                {
                    older = snapshotsBuffer[i];
                    newer = snapshotsBuffer[i + 1];
                    return true;
                }
            }

            return false;
        }

        private float CalculateInterpolationFactor(double renderTime, double olderTime, double newerTime)
        {
            return (float)((renderTime - olderTime) / (newerTime - olderTime));
        }

        protected void CleanupOldSnapshots(double renderTime)
        {
            int removeCount = 0;
            while (removeCount < snapshotsBuffer.Count - 2 &&
                   snapshotsBuffer[removeCount].Timestamp < renderTime - interpolationDelayMs)
            {
                removeCount++;
            }

            if (removeCount > 0)
            {
                snapshotsBuffer.RemoveRange(0, removeCount);
            }
        }
    }
}
