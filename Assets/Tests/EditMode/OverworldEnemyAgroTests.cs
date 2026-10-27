#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DigiPurity.Combat;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class OverworldEnemyAgroTests
    {
        [Test]
        public void GolferFacing_WhenPlayerToTheLeft_DoesNotFlipAround()
        {
            // Golfer.png naturally faces LEFT.
            // When Owen approaches from the left (toPlayer.x < 0), Golfer must NOT flipX to true (which would turn his back to Owen).
            Vector2 toPlayerLeft = new Vector2(-1f, 0f);
            bool flipX = OverworldEnemy.CalculateFacingFlip(toPlayerLeft, spriteFacesLeftByDefault: true);

            Assert.IsFalse(flipX, "Golfer should not flipX when running towards player on his left; he must face forward towards Owen.");
        }

        [Test]
        public void GolferFacing_WhenPlayerToTheRight_FlipsToFaceRight()
        {
            // Golfer.png naturally faces LEFT.
            // When Owen is to the right (toPlayer.x > 0), Golfer must flipX to face right.
            Vector2 toPlayerRight = new Vector2(1f, 0f);
            bool flipX = OverworldEnemy.CalculateFacingFlip(toPlayerRight, spriteFacesLeftByDefault: true);

            Assert.IsTrue(flipX, "Golfer must flipX when pursuing player to his right so he faces Owen.");
        }

        [Test]
        public void RightFacingEnemy_FacingFlipsCorrectly()
        {
            // For standard right-facing sprites:
            Vector2 toPlayerLeft = new Vector2(-1f, 0f);
            Vector2 toPlayerRight = new Vector2(1f, 0f);

            bool flipLeft = OverworldEnemy.CalculateFacingFlip(toPlayerLeft, spriteFacesLeftByDefault: false);
            bool flipRight = OverworldEnemy.CalculateFacingFlip(toPlayerRight, spriteFacesLeftByDefault: false);

            Assert.IsTrue(flipLeft, "Right-facing sprite must flipX when player is on the left.");
            Assert.IsFalse(flipRight, "Right-facing sprite should not flipX when player is on the right.");
        }

        [Test]
        public void EnemyAcceleration_RampsSpeedUpToCap()
        {
            float initialSpeed = 4.0f;
            float acceleration = 7.5f;
            float maxSpeed = 13.5f;

            // After 0.5s: 4.0 + 7.5 * 0.5 = 7.75
            float speedAtHalfSec = OverworldEnemy.CalculateAcceleratedSpeed(initialSpeed, acceleration, maxSpeed, 0.5f);
            Assert.AreEqual(7.75f, speedAtHalfSec, 0.001f);

            // After 1.0s: 4.0 + 7.5 * 1.0 = 11.5
            float speedAtOneSec = OverworldEnemy.CalculateAcceleratedSpeed(initialSpeed, acceleration, maxSpeed, 1.0f);
            Assert.AreEqual(11.5f, speedAtOneSec, 0.001f);

            // After 2.0s: capped at maxSpeed = 13.5
            float speedAtTwoSec = OverworldEnemy.CalculateAcceleratedSpeed(initialSpeed, acceleration, maxSpeed, 2.0f);
            Assert.AreEqual(maxSpeed, speedAtTwoSec, 0.001f);
        }

        [Test]
        public void EnemyMaxRushSpeed_IsStrictlyFasterThanOwenMaxDiagonalSprint()
        {
            float owenWalkSpeed = 4.5f;
            float owenRunMultiplier = 1.6f;
            float owenDiagonalMultiplier = 1.5f;

            float owenMaxPossibleSpeed = owenWalkSpeed * owenRunMultiplier * owenDiagonalMultiplier; // 10.8f

            GameObject go = new GameObject("TestEnemy");
            var enemy = go.AddComponent<OverworldEnemy>();

            Assert.Greater(enemy.MaxRushSpeed, owenMaxPossibleSpeed,
                $"Enemy top rush speed ({enemy.MaxRushSpeed}) must be strictly faster than Owen's maximum possible diagonal running speed ({owenMaxPossibleSpeed}) so the player cannot just run away.");

            Object.DestroyImmediate(go);
        }
    }
}
#endif
