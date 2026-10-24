#if UNITY_EDITOR
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

        [Test]
        public void Overworld_FloorContainment_PreventsWalkingIntoVoid_InCompoundRooms()
        {
            // Room A: Main room (Center (0, 0), Size (16, 12) -> X [-8, 8], Y [-6, 6])
            GameObject roomA = new GameObject("RoomA");
            var colA = roomA.AddComponent<BoxCollider2D>();
            colA.size = new Vector2(16f, 12f);
            var rbA = roomA.AddComponent<RoomBounds>();

            // Room B: Hallway extending to the right (Center (12, 0), Size (10, 4) -> X [7, 17], Y [-2, 2])
            GameObject roomB = new GameObject("RoomB");
            roomB.transform.position = new Vector3(12f, 0f, 0f);
            var colB = roomB.AddComponent<BoxCollider2D>();
            colB.size = new Vector2(10f, 4f);
            var rbB = roomB.AddComponent<RoomBounds>();

            // 1. Inside hallway: (12, 0)
            Assert.IsTrue(RoomBounds.IsFootprintInAnyRoom(new Vector2(12f, 0f)), "Inside hallway should be walkable");

            // 2. In void above hallway: X=14, Y=4 (above hallway's max Y of 2, and outside RoomA)
            Assert.IsFalse(RoomBounds.IsFootprintInAnyRoom(new Vector2(14f, 4f)), "Void above hallway must NOT be walkable");

            // 3. In void below hallway: X=14, Y=-4
            Assert.IsFalse(RoomBounds.IsFootprintInAnyRoom(new Vector2(14f, -4f)), "Void below hallway must NOT be walkable");

            // 4. In void past hallway end: X=19, Y=0
            Assert.IsFalse(RoomBounds.IsFootprintInAnyRoom(new Vector2(19f, 0f)), "Void past hallway end must NOT be walkable");

            // 5. At overlapping doorway: X=7.5, Y=0 (seamless doorway passage)
            Assert.IsTrue(RoomBounds.IsFootprintInAnyRoom(new Vector2(7.5f, 0f)), "Doorway between rooms must be walkable");

            Object.DestroyImmediate(roomA);
            Object.DestroyImmediate(roomB);
        }
    }
}
#endif
