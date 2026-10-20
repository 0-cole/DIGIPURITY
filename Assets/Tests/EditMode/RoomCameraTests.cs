using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class RoomCameraTests
    {
        [Test]
        public void Camera_4By3ViewportRect_PillarboxesOnWidescreen()
        {
            // 16:9 Widescreen (1920x1080) -> 4:3 is 1440x1080, centered with 240px pillarbox bars on left/right
            Rect rect169 = RoomCameraController.Calculate4By3ViewportRect(1920, 1080);
            Assert.AreEqual(0.125f, rect169.x, 0.001f, "Pillarbox left inset should be 12.5%");
            Assert.AreEqual(0.75f, rect169.width, 0.001f, "4:3 viewport width should be 75% on 16:9");
            Assert.AreEqual(0f, rect169.y, 0.001f);
            Assert.AreEqual(1f, rect169.height, 0.001f);

            // Native 4:3 Screen (1024x768) -> Full viewport
            Rect rect43 = RoomCameraController.Calculate4By3ViewportRect(1024, 768);
            Assert.AreEqual(0f, rect43.x, 0.001f);
            Assert.AreEqual(0f, rect43.y, 0.001f);
            Assert.AreEqual(1f, rect43.width, 0.001f);
            Assert.AreEqual(1f, rect43.height, 0.001f);

            // 5:4 Screen (1280x1024) -> Letterbox top and bottom
            Rect rect54 = RoomCameraController.Calculate4By3ViewportRect(1280, 1024);
            Assert.AreEqual(0f, rect54.x, 0.001f);
            Assert.AreEqual(1f, rect54.width, 0.001f);
            Assert.Greater(rect54.y, 0f, "Letterbox top/bottom bars should be present on 5:4");
            Assert.Less(rect54.height, 1f);
        }

        [Test]
        public void SmallRoom_CameraStaysStillAtRoomCenter_ShowsRoomInFullWithBlackBorders()
        {
            // Room (10 x 8) smaller than 4:3 viewport (13.333 x 10) at orthoSize 5
            Bounds smallRoom = new Bounds(new Vector3(2f, 3f, 0f), new Vector3(10f, 8f, 0f));
            float orthoSize = 5f;
            float aspect = 4f / 3f;

            // When Owen is on the far left of the room
            Vector2 owenFarLeft = new Vector2(-2.5f, 1f);
            Vector3 camPos1 = RoomCameraController.CalculateCameraPosition(owenFarLeft, Vector2.zero, smallRoom, orthoSize, aspect);

            // When Owen is on the far right of the room
            Vector2 owenFarRight = new Vector2(6.5f, 4f);
            Vector3 camPos2 = RoomCameraController.CalculateCameraPosition(owenFarRight, Vector2.zero, smallRoom, orthoSize, aspect);

            // In Undertale, camera stays completely stationary at the room center (2, 3)
            Assert.AreEqual(smallRoom.center.x, camPos1.x, 0.001f, "Camera must stay still on X in a small room");
            Assert.AreEqual(smallRoom.center.y, camPos1.y, 0.001f, "Camera must stay still on Y in a small room");
            Assert.AreEqual(camPos1.x, camPos2.x, 0.001f, "Camera position must not change as player moves across small room");
            Assert.AreEqual(camPos1.y, camPos2.y, 0.001f, "Camera position must not change as player moves across small room");
        }

        [Test]
        public void NarrowHorizontalHallway_LocksYAtCenter_TracksOwenXClampedToEdges()
        {
            // Horizontal hallway: width = 40, height = 6. Viewport is 13.333 x 10.
            Bounds hallway = new Bounds(Vector3.zero, new Vector3(40f, 6f, 0f));
            float orthoSize = 5f;
            float aspect = 4f / 3f;
            float halfWidth = orthoSize * aspect; // 6.6667f

            // Height (6) <= viewHeight (10), so Y must remain locked at hallway center (0)
            Vector2 playerPos = new Vector2(5f, 1f);
            Vector3 camPos = RoomCameraController.CalculateCameraPosition(playerPos, Vector2.zero, hallway, orthoSize, aspect);

            Assert.AreEqual(0f, camPos.y, 0.001f, "Camera must remain vertically locked at hallway center");
            Assert.AreEqual(5f, camPos.x, 0.001f, "Camera should track player X in middle of hallway");

            // Starting position at hallway left door (x = -20)
            Vector2 playerAtLeftDoor = new Vector2(-20f, 0f);
            Vector3 camAtLeft = RoomCameraController.CalculateCameraPosition(playerAtLeftDoor, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMinX = hallway.min.x + halfWidth; // -20 + 6.6667 = -13.3333
            Assert.AreEqual(expectedMinX, camAtLeft.x, 0.001f, "Camera must clamp to left edge of hallway");
            Assert.AreEqual(hallway.min.x, camAtLeft.x - halfWidth, 0.001f, "Left edge of camera viewport must align with left wall (no void)");

            // Clamping at hallway right door (x = +20)
            Vector2 playerAtRightDoor = new Vector2(20f, 0f);
            Vector3 camAtRight = RoomCameraController.CalculateCameraPosition(playerAtRightDoor, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMaxX = hallway.max.x - halfWidth; // 20 - 6.6667 = 13.3333
            Assert.AreEqual(expectedMaxX, camAtRight.x, 0.001f, "Camera must clamp to right edge of hallway");
            Assert.AreEqual(hallway.max.x, camAtRight.x + halfWidth, 0.001f, "Right edge of camera viewport must align with right wall (no void)");
        }

        [Test]
        public void BigRoom_Snowdin_FollowsOwenOnBothAxes_OffsetToSideAtStartSoNoVoidShows()
        {
            // Big room (like Snowdin forest): width = 50, height = 40. Viewport is 13.333 x 10.
            Bounds bigRoom = new Bounds(Vector3.zero, new Vector3(50f, 40f, 0f));
            float orthoSize = 5f;
            float aspect = 4f / 3f;
            float halfWidth = orthoSize * aspect; // 6.6667f
            float halfHeight = orthoSize;          // 5.0f

            // Starting position at entrance door on far left: Owen is at x = -24, y = 0
            Vector2 owenAtLeftEntrance = new Vector2(-24f, 0f);
            Vector3 camAtStart = RoomCameraController.CalculateCameraPosition(owenAtLeftEntrance, Vector2.zero, bigRoom, orthoSize, aspect);

            // The camera must be offset to the side of Owen (cam.x > owen.x) so left edge aligns with left wall
            float expectedMinX = bigRoom.min.x + halfWidth; // -25 + 6.6667 = -18.3333
            Assert.AreEqual(expectedMinX, camAtStart.x, 0.001f, "Camera must start clamped at left room boundary");
            Assert.Greater(camAtStart.x, owenAtLeftEntrance.x, "Camera must be offset to the side of Owen so no void is shown");
            Assert.AreEqual(bigRoom.min.x, camAtStart.x - halfWidth, 0.001f, "Viewport left edge must not clip past room left wall");

            // In the open center of the big room (x = 5, y = 8): camera follows Owen directly in the center
            Vector2 owenInOpenArea = new Vector2(5f, 8f);
            Vector3 camInOpen = RoomCameraController.CalculateCameraPosition(owenInOpenArea, Vector2.zero, bigRoom, orthoSize, aspect);
            Assert.AreEqual(5f, camInOpen.x, 0.001f, "Camera tracks Owen X in open area");
            Assert.AreEqual(8f, camInOpen.y, 0.001f, "Camera tracks Owen Y in open area");

            // Near top-right corner of big room (x = 24, y = 19): camera clamps on both axes
            Vector2 owenNearCorner = new Vector2(24f, 19f);
            Vector3 camNearCorner = RoomCameraController.CalculateCameraPosition(owenNearCorner, Vector2.zero, bigRoom, orthoSize, aspect);
            float expectedMaxX = bigRoom.max.x - halfWidth;
            float expectedMaxY = bigRoom.max.y - halfHeight;
            Assert.AreEqual(expectedMaxX, camNearCorner.x, 0.001f, "Camera clamps to right room wall");
            Assert.AreEqual(expectedMaxY, camNearCorner.y, 0.001f, "Camera clamps to top room wall");
            Assert.AreEqual(bigRoom.max.x, camNearCorner.x + halfWidth, 0.001f, "Viewport right edge aligns with right wall (no void)");
            Assert.AreEqual(bigRoom.max.y, camNearCorner.y + halfHeight, 0.001f, "Viewport top edge aligns with top wall (no void)");
        }

        [Test]
        public void RoomBounds_PrioritizesColliders_EncapsulatesAllPerimeterWalls()
        {
            // Object with 4 perimeter walls centered at (4.1813, 2.3647)
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

            // Bounds must encapsulate all 4 colliders centered at (4.1813, 2.3647)
            Bounds b = roomBounds.Bounds;
            Assert.AreEqual(4.1813f, b.center.x, 0.05f, "Room center X must align with room center");
            Assert.AreEqual(2.3647f, b.center.y, 0.05f, "Room center Y must align with room center, NOT top wall");
            Assert.AreNotEqual(9.5507f, b.center.y, "Room center Y must never snap to isolated top wall collider");

            Object.DestroyImmediate(squareObj);
        }
    }
}
