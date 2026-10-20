using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class RoomCameraTests
    {
        [Test]
        public void SmallRoom_CameraStaysStillAtRoomCenter()
        {
            // Room smaller than camera viewport on both axes (e.g. 12 x 8.8 room, 17.78 x 10 viewport)
            Bounds smallRoom = new Bounds(Vector3.zero, new Vector3(12f, 8.8f, 0f));
            float orthoSize = 5f;
            float aspect = 16f / 9f; // view is ~17.78 x 10

            // When Owen is on the far left of the room
            Vector2 owenFarLeft = new Vector2(-4.5f, -1f);
            Vector3 camPos1 = RoomCameraController.CalculateCameraPosition(owenFarLeft, Vector2.zero, smallRoom, orthoSize, aspect);

            // When Owen is on the far right of the room
            Vector2 owenFarRight = new Vector2(4.5f, 2f);
            Vector3 camPos2 = RoomCameraController.CalculateCameraPosition(owenFarRight, Vector2.zero, smallRoom, orthoSize, aspect);

            // Both must remain perfectly still at the center of the room (0, 0)
            Assert.AreEqual(smallRoom.center.x, camPos1.x, 0.001f, "Camera must stay still on X in a small room");
            Assert.AreEqual(smallRoom.center.y, camPos1.y, 0.001f, "Camera must stay still on Y in a small room");
            Assert.AreEqual(camPos1.x, camPos2.x, 0.001f, "Camera position must not change as player moves across small room");
            Assert.AreEqual(camPos1.y, camPos2.y, 0.001f, "Camera position must not change as player moves across small room");
        }

        [Test]
        public void LongHorizontalHallway_FollowsPlayerX_StaysStillOnY()
        {
            // Long horizontal hallway: width = 40, height = 6
            Bounds hallway = new Bounds(new Vector3(10f, 0f, 0f), new Vector3(40f, 6f, 0f));
            float orthoSize = 5f;
            float aspect = 16f / 9f; // viewWidth ≈ 17.778, viewHeight = 10
            float halfWidth = orthoSize * aspect;

            // In hallway, height (6) <= viewHeight (10), so Y must remain locked at hallway center (0)
            Vector2 playerPos = new Vector2(5f, 1f);
            Vector3 camPos = RoomCameraController.CalculateCameraPosition(playerPos, Vector2.zero, hallway, orthoSize, aspect);

            Assert.AreEqual(0f, camPos.y, 0.001f, "Camera must remain vertically locked in a horizontal hallway");
            Assert.AreEqual(5f, camPos.x, 0.001f, "Camera should track player X in middle of hallway");

            // Test clamping at hallway left end
            Vector2 playerAtLeftEnd = new Vector2(-15f, 0f); // hallway min.x is -10
            Vector3 camAtLeft = RoomCameraController.CalculateCameraPosition(playerAtLeftEnd, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMinX = hallway.min.x + halfWidth;
            Assert.AreEqual(expectedMinX, camAtLeft.x, 0.001f, "Camera must clamp to left edge of hallway");

            // Test clamping at hallway right end
            Vector2 playerAtRightEnd = new Vector2(35f, 0f); // hallway max.x is +30
            Vector3 camAtRight = RoomCameraController.CalculateCameraPosition(playerAtRightEnd, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMaxX = hallway.max.x - halfWidth;
            Assert.AreEqual(expectedMaxX, camAtRight.x, 0.001f, "Camera must clamp to right edge of hallway");
        }

        [Test]
        public void TallVerticalCorridor_FollowsPlayerY_StaysStillOnX()
        {
            // Tall vertical corridor: width = 8, height = 50
            Bounds corridor = new Bounds(new Vector3(0f, 20f, 0f), new Vector3(8f, 50f, 0f));
            float orthoSize = 5f;
            float aspect = 16f / 9f; // viewWidth ≈ 17.778, viewHeight = 10
            float halfHeight = orthoSize;

            // In vertical corridor, width (8) <= viewWidth (17.78), so X must stay locked at corridor center (0)
            Vector2 playerPos = new Vector2(1f, 15f);
            Vector3 camPos = RoomCameraController.CalculateCameraPosition(playerPos, Vector2.zero, corridor, orthoSize, aspect);

            Assert.AreEqual(0f, camPos.x, 0.001f, "Camera must remain horizontally locked in a vertical corridor");
            Assert.AreEqual(15f, camPos.y, 0.001f, "Camera should track player Y in middle of corridor");

            // Test clamping at corridor bottom end
            Vector2 playerAtBottom = new Vector2(0f, -10f); // corridor min.y is -5
            Vector3 camAtBottom = RoomCameraController.CalculateCameraPosition(playerAtBottom, Vector2.zero, corridor, orthoSize, aspect);
            float expectedMinY = corridor.min.y + halfHeight;
            Assert.AreEqual(expectedMinY, camAtBottom.y, 0.001f, "Camera must clamp to bottom edge of corridor");

            // Test clamping at corridor top end
            Vector2 playerAtTop = new Vector2(0f, 55f); // corridor max.y is +45
            Vector3 camAtTop = RoomCameraController.CalculateCameraPosition(playerAtTop, Vector2.zero, corridor, orthoSize, aspect);
            float expectedMaxY = corridor.max.y - halfHeight;
            Assert.AreEqual(expectedMaxY, camAtTop.y, 0.001f, "Camera must clamp to top edge of corridor");
        }

        [Test]
        public void WalkableSquare_PerimeterWalls_CentersCameraOnRoomFloor()
        {
            // Simulate WalkableSquare positioned at (4.1813, 2.3647) with 4 perimeter walls
            // First wall is at offset Y = +8.5 (top wall). Old bug grabbed only this first collider!
            GameObject squareObj = new GameObject("WalkableSquare_Test");
            squareObj.transform.position = new Vector3(4.1813f, 2.3647f, 0f);

            var topWall = squareObj.AddComponent<BoxCollider2D>();
            topWall.offset = new Vector2(0f, 8.5f);
            topWall.size = new Vector2(17f, 1f);

            var bottomWall = squareObj.AddComponent<BoxCollider2D>();
            bottomWall.offset = new Vector2(0f, -8.5f);
            bottomWall.size = new Vector2(17f, 1f);

            var leftWall = squareObj.AddComponent<BoxCollider2D>();
            leftWall.offset = new Vector2(-8.5f, 0f);
            leftWall.size = new Vector2(1f, 17f);

            var rightWall = squareObj.AddComponent<BoxCollider2D>();
            rightWall.offset = new Vector2(8.5f, 0f);
            rightWall.size = new Vector2(1f, 17f);

            var roomBounds = squareObj.AddComponent<RoomBounds>();
            roomBounds.ResolveReferences();

            // The room bounds must center on the room (2.3647), NOT the top wall (9.5507)
            Bounds b = roomBounds.Bounds;
            Assert.AreEqual(4.1813f, b.center.x, 0.05f, "Room center X must align with room floor");
            Assert.AreEqual(2.3647f, b.center.y, 0.05f, "Room center Y must align with room floor, NOT the top wall");
            Assert.AreNotEqual(9.5507f, b.center.y, "Room center Y must not be shifted to top wall collider");

            // Camera calculation on this room must center at (4.1813, 2.3647)
            Vector3 camPos = RoomCameraController.CalculateCameraPosition(
                new Vector2(4.1813f, 2.3647f),
                Vector2.zero,
                b,
                5f,
                16f / 9f
            );

            Assert.AreEqual(4.1813f, camPos.x, 0.05f, "Camera must center horizontally on WalkableSquare");
            Assert.AreEqual(2.3647f, camPos.y, 0.05f, "Camera must center vertically on WalkableSquare");

            Object.DestroyImmediate(squareObj);
        }
    }
}
