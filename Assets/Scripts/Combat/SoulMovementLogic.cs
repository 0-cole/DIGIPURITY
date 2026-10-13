using UnityEngine;

namespace DigiPurity.Combat
{
    public class SoulMovementLogic
    {
        public float baseSpeed;
        public Vector2 boundsMin;
        public Vector2 boundsMax;
        public float borderMargin;

        public SoulMovementLogic(float speed = 4f, Vector2? min = null, Vector2? max = null, float margin = 0.5f)
        {
            baseSpeed = speed;
            boundsMin = min ?? new Vector2(-5f, -5f);
            boundsMax = max ?? new Vector2(5f, 5f);
            borderMargin = margin;
        }

        public Vector2 CalculateNextPosition(Vector2 currentPos, Vector2 inputDir, bool slowHeld, float deltaTime)
        {
            float effectiveSpeed = baseSpeed;
            if (slowHeld)
            {
                effectiveSpeed = baseSpeed / 2.0f;
            }

            Vector2 nextPos = currentPos + (inputDir.normalized * effectiveSpeed * deltaTime);

            float clampedX = Mathf.Clamp(nextPos.x, boundsMin.x + borderMargin, boundsMax.x - borderMargin);
            float clampedY = Mathf.Clamp(nextPos.y, boundsMin.y + borderMargin, boundsMax.y - borderMargin);

            return new Vector2(clampedX, clampedY);
        }

        public static int DecrementInvulnerability(int currentFrames)
        {
            return Mathf.Max(0, currentFrames - 1);
        }
    }
}
