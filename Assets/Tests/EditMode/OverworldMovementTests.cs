using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class OverworldMovementTests
    {
        [Test]
        public void Overworld_CardinalMovement_MaintainsWalkSpeed()
        {
            float walkSpeed = 4.5f;
            float runMultiplier = 1.6f;
            float diagonalMultiplier = 1.5f;

            Vector2 rightInput = new Vector2(1f, 0f);
            Vector2 upInput = new Vector2(0f, 1f);

            Vector2 rightVel = OverworldPlayerController.CalculateVelocity(rightInput, walkSpeed, false, runMultiplier, diagonalMultiplier);
            Vector2 upVel = OverworldPlayerController.CalculateVelocity(upInput, walkSpeed, false, runMultiplier, diagonalMultiplier);

            Assert.AreEqual(walkSpeed, rightVel.magnitude, 0.001f);
            Assert.AreEqual(walkSpeed, upVel.magnitude, 0.001f);
        }

        [Test]
        public void Overworld_DiagonalMovement_MovesAtOnePointFiveSpeed()
        {
            float walkSpeed = 4.5f;
            float runMultiplier = 1.6f;
            float diagonalMultiplier = 1.5f;

            Vector2 diagonalInput = new Vector2(1f, 1f);

            Vector2 diagVel = OverworldPlayerController.CalculateVelocity(diagonalInput, walkSpeed, false, runMultiplier, diagonalMultiplier);

            // Total speed magnitude should be 1.5x walk speed = 6.75f
            Assert.AreEqual(walkSpeed * 1.5f, diagVel.magnitude, 0.001f, "Diagonal speed magnitude must be 1.5x walkSpeed");

            // Individual axis components should be (walkSpeed * 1.5) / sqrt(2) ≈ 4.773f
            // which is greater than cardinal walkSpeed (4.5f), completely preventing any slowdown sensation!
            Assert.Greater(diagVel.x, walkSpeed, "Horizontal progress during diagonal move must not be slower than cardinal");
            Assert.Greater(diagVel.y, walkSpeed, "Vertical progress during diagonal move must not be slower than cardinal");
        }

        [Test]
        public void Overworld_DiagonalRunning_MovesAtOnePointFiveTimesRunningSpeed()
        {
            float walkSpeed = 4.5f;
            float runMultiplier = 1.6f;
            float diagonalMultiplier = 1.5f;

            Vector2 diagonalInput = new Vector2(-1f, 1f);

            Vector2 diagRunVel = OverworldPlayerController.CalculateVelocity(diagonalInput, walkSpeed, true, runMultiplier, diagonalMultiplier);

            float expectedSpeed = walkSpeed * runMultiplier * diagonalMultiplier;
            Assert.AreEqual(expectedSpeed, diagRunVel.magnitude, 0.001f, "Diagonal running speed magnitude must be 1.5x running speed");
        }
    }
}
