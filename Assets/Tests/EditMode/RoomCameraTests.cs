#if UNITY_EDITOR
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
            // Room (10 x 8) smaller than 4:3 viewport (17.33 x 13) at orthoSize 6.5
            Bounds smallRoom = new Bounds(new Vector3(2f, 3f, 0f), new Vector3(10f, 8f, 0f));
            float orthoSize = 6.5f;
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
            // Horizontal hallway: width = 40, height = 6. Viewport is 17.33 x 13 at orthoSize 6.5.
            Bounds hallway = new Bounds(Vector3.zero, new Vector3(40f, 6f, 0f));
            float orthoSize = 6.5f;
            float aspect = 4f / 3f;
            float halfWidth = orthoSize * aspect; // 8.6667f

            // Height (6) <= viewHeight (13), so Y must remain locked at hallway center (0)
            Vector2 playerPos = new Vector2(5f, 1f);
            Vector3 camPos = RoomCameraController.CalculateCameraPosition(playerPos, Vector2.zero, hallway, orthoSize, aspect);

            Assert.AreEqual(0f, camPos.y, 0.001f, "Camera must remain vertically locked at hallway center");
            Assert.AreEqual(5f, camPos.x, 0.001f, "Camera should track player X in middle of hallway");

            // Starting position at hallway left door (x = -20)
            Vector2 playerAtLeftDoor = new Vector2(-20f, 0f);
            Vector3 camAtLeft = RoomCameraController.CalculateCameraPosition(playerAtLeftDoor, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMinX = hallway.min.x + halfWidth; // -20 + 8.6667 = -11.3333
            Assert.AreEqual(expectedMinX, camAtLeft.x, 0.001f, "Camera must clamp to left edge of hallway");
            Assert.AreEqual(hallway.min.x, camAtLeft.x - halfWidth, 0.001f, "Left edge of camera viewport must align with left wall (no void)");

            // Clamping at hallway right door (x = +20)
            Vector2 playerAtRightDoor = new Vector2(20f, 0f);
            Vector3 camAtRight = RoomCameraController.CalculateCameraPosition(playerAtRightDoor, Vector2.zero, hallway, orthoSize, aspect);
            float expectedMaxX = hallway.max.x - halfWidth; // 20 - 8.6667 = 11.3333
            Assert.AreEqual(expectedMaxX, camAtRight.x, 0.001f, "Camera must clamp to right edge of hallway");
            Assert.AreEqual(hallway.max.x, camAtRight.x + halfWidth, 0.001f, "Right edge of camera viewport must align with right wall (no void)");
        }

        [Test]
        public void BigRoom_Snowdin_FollowsOwenOnBothAxes_OffsetToSideAtStartSoNoVoidShows()
        {
            // Big room (like Snowdin forest): width = 50, height = 40. Viewport is 17.33 x 13 at orthoSize 6.5.
            Bounds bigRoom = new Bounds(Vector3.zero, new Vector3(50f, 40f, 0f));
            float orthoSize = 6.5f;
            float aspect = 4f / 3f;
            float halfWidth = orthoSize * aspect; // 8.6667f
            float halfHeight = orthoSize;          // 6.5f

            // Starting position at entrance door on far left: Owen is at x = -24, y = 0
            Vector2 owenAtLeftEntrance = new Vector2(-24f, 0f);
            Vector3 camAtStart = RoomCameraController.CalculateCameraPosition(owenAtLeftEntrance, Vector2.zero, bigRoom, orthoSize, aspect);

            // The camera must be offset to the side of Owen (cam.x > owen.x) so left edge aligns with left wall
            float expectedMinX = bigRoom.min.x + halfWidth; // -25 + 8.6667 = -16.3333
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
        public void TargetCenter_CentersOnSpriteBodyRatherThanFeet()
        {
            GameObject testOwen = new GameObject("TestOwen");
            testOwen.transform.position = new Vector2(10f, 5f); // Feet are at Y=5

            // Without SpriteRenderer: returns fallback torso height ~1.85 above feet
            Vector2 fallbackCenter = RoomCameraController.GetTargetCenter(testOwen.transform, Vector2.zero);
            Assert.AreEqual(10f, fallbackCenter.x, 0.001f);
            Assert.AreEqual(5f + 1.85f, fallbackCenter.y, 0.01f, "Target center should point at body above feet");

            Object.DestroyImmediate(testOwen);
        }

        [Test]
        public void Camera_DefaultOrthographicSize_ProvidesExpanded24By18Viewport()
        {
            GameObject camObj = new GameObject("TestCam");
            var cam = camObj.AddComponent<Camera>();
            var rcc = camObj.AddComponent<RoomCameraController>();

            // Default orthographic size is 9.0f, providing 24x18 units (12x9 retro tiles)
            Assert.AreEqual(9.0f, rcc.SetOrthographicSize, 0.001f);
            Assert.AreEqual(9.0f, rcc.EffectiveOrthographicSize, 0.001f);

            float halfHeight = rcc.EffectiveOrthographicSize;
            float halfWidth = halfHeight * RoomCameraController.TargetAspect4By3;
            float viewWidth = halfWidth * 2f;
            float viewHeight = halfHeight * 2f;

            Assert.AreEqual(24.0f, viewWidth, 0.001f, "Camera view width should be 24 units (12 tiles at 2.0 PPU)");
            Assert.AreEqual(18.0f, viewHeight, 0.001f, "Camera view height should be 18 units (9 tiles at 2.0 PPU)");

            Object.DestroyImmediate(camObj);
        }

        [Test]
        public void RoomBounds_GetEffectiveBounds_MergesConnectedOverlappingRooms()
        {
            GameObject roomAObj = new GameObject("RoomA");
            roomAObj.transform.position = new Vector3(0f, 0f, 0f);
            var colA = roomAObj.AddComponent<BoxCollider2D>();
            colA.size = new Vector2(16f, 12f);
            var rbA = roomAObj.AddComponent<RoomBounds>();

            GameObject roomBObj = new GameObject("RoomB");
            roomBObj.transform.position = new Vector3(12f, 0f, 0f); // Overlaps with RoomA between X=4 and X=8
            var colB = roomBObj.AddComponent<BoxCollider2D>();
            colB.size = new Vector2(10f, 6f);
            var rbB = roomBObj.AddComponent<RoomBounds>();

            Bounds effA = rbA.GetEffectiveBounds();
            Bounds effB = rbB.GetEffectiveBounds();

            // Total merged width: RoomA min.x (-8) to RoomB max.x (12 + 5 = 17) -> total size 25
            Assert.AreEqual(25f, effA.size.x, 0.05f, "RoomA effective bounds should encapsulate overlapping RoomB");
            Assert.AreEqual(effA.size.x, effB.size.x, 0.05f, "Both connected rooms should share the exact same effective compound bounds");
            Assert.AreEqual(effA.center.x, effB.center.x, 0.05f, "Both connected rooms should share the exact same effective center");

            Object.DestroyImmediate(roomAObj);
            Object.DestroyImmediate(roomBObj);
        }
    }
}
#endif
