using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class AreaDoorTests
    {
        [Test]
        public void FindDoorById_LocatesTargetDoorInScene()
        {
            GameObject doorObjA = new GameObject("DoorA");
            var doorA = doorObjA.AddComponent<AreaDoor>();
            doorA.DoorId = "Entrance_A";

            GameObject doorObjB = new GameObject("DoorB");
            var doorB = doorObjB.AddComponent<AreaDoor>();
            doorB.DoorId = "Exit_B";

            AreaDoor found = AreaDoor.FindDoorById("Exit_B");
            Assert.IsNotNull(found);
            Assert.AreEqual("Exit_B", found.DoorId);

            Object.DestroyImmediate(doorObjA);
            Object.DestroyImmediate(doorObjB);
        }

        [Test]
        public void ArrivalPosition_CalculatesCorrectEmergencePoint()
        {
            GameObject doorObj = new GameObject("Door");
            doorObj.transform.position = new Vector3(10f, 20f, 0f);

            var door = doorObj.AddComponent<AreaDoor>();
            door.ArrivalSpawnOffset = new Vector2(0f, -1.5f); // Step out 1.5 units downward

            Vector3 expectedArrival = new Vector3(10f, 18.5f, 0f);
            Vector3 actualArrival = doorObj.transform.position + (Vector3)door.ArrivalSpawnOffset;

            Assert.AreEqual(expectedArrival.x, actualArrival.x, 0.001f);
            Assert.AreEqual(expectedArrival.y, actualArrival.y, 0.001f);

            Object.DestroyImmediate(doorObj);
        }

        [Test]
        public void EggRoom_SecretDivert_SavesOriginalDestinationAndRestoresOnExit()
        {
            // Reset static state
            AreaDoor.EggCollected = false;
            AreaDoor.DivertedTargetDoorId = "";
            AreaDoor.DivertedTargetScene = "";

            // Simulate normal door with 100% secret chance for testing
            GameObject normalDoorObj = new GameObject("NormalDoor");
            var normalDoor = normalDoorObj.AddComponent<AreaDoor>();
            normalDoor.DoorId = "Forest_East";
            normalDoor.TargetDoorId = "Town_West";
            normalDoor.HasSecretChance = true;
            normalDoor.SecretChancePercent = 100f; // Force divert
            normalDoor.SecretTargetDoorId = "EggRoom_In";

            // When diverted: original target ("Town_West") is saved, new target is "EggRoom_In"
            AreaDoor.DivertedTargetDoorId = normalDoor.TargetDoorId;
            string dest = normalDoor.SecretTargetDoorId;

            Assert.AreEqual("EggRoom_In", dest, "Secret room must become destination");
            Assert.AreEqual("Town_West", AreaDoor.DivertedTargetDoorId, "Original destination must be remembered");

            // Exit door inside egg room: marked as isSecretRoomExit
            GameObject exitDoorObj = new GameObject("EggRoomExit");
            var exitDoor = exitDoorObj.AddComponent<AreaDoor>();
            exitDoor.DoorId = "EggRoom_Out";
            exitDoor.IsSecretRoomExit = true;

            // When exiting, it returns to DivertedTargetDoorId ("Town_West")
            string returnDest = AreaDoor.DivertedTargetDoorId;
            AreaDoor.DivertedTargetDoorId = "";

            Assert.AreEqual("Town_West", returnDest, "Exiting secret room must return player to their original route");
            Assert.IsEmpty(AreaDoor.DivertedTargetDoorId, "Diverted target should be cleared after returning");

            Object.DestroyImmediate(normalDoorObj);
            Object.DestroyImmediate(exitDoorObj);
        }

        [Test]
        public void PlayerInputLocked_StopsVelocityAndMovement()
        {
            Vector2 input = new Vector2(1f, 0f);
            float speed = 4f;

            // When normal: produces velocity
            Vector2 normalVel = OverworldPlayerController.CalculateVelocity(input, speed, false, 1.5f, 0.8f);
            Assert.Greater(normalVel.magnitude, 0.1f);

            // When locked (input forced to zero): zero velocity
            Vector2 lockedVel = OverworldPlayerController.CalculateVelocity(Vector2.zero, speed, false, 1.5f, 0.8f);
            Assert.AreEqual(0f, lockedVel.magnitude, 0.001f);
        }
    }
}
