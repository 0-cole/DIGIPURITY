#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class InvisibleWallTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Test_InvisibleWall");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void EnsureCollider_AddsBoxCollider2D_WhenMissing()
        {
            var wall = _go.AddComponent<InvisibleWall>();
            var col = wall.EnsureCollider();

            Assert.IsNotNull(col);
            Assert.IsFalse(col.isTrigger, "Solid wall collider must NOT be a trigger.");
            Assert.AreEqual(new Vector2(2f, 2f), col.size);
        }

        [Test]
        public void EnsureCollider_ForcesNonTrigger_WhenColliderWasTrigger()
        {
            var col = _go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var wall = _go.AddComponent<InvisibleWall>();
            wall.EnsureCollider();

            Assert.IsFalse(col.isTrigger, "InvisibleWall should ensure collider is not a trigger.");
        }

        [Test]
        public void SnapToGrid_SnapsPositionAndColliderSize()
        {
            _go.transform.position = new Vector3(1.3f, 3.8f, 0f);
            var wall = _go.AddComponent<InvisibleWall>();
            var col = wall.EnsureCollider();
            col.size = new Vector2(3.1f, 1.9f);

            wall.SnapToGrid();

            Assert.AreEqual(2.0f, _go.transform.position.x, 0.01f);
            Assert.AreEqual(4.0f, _go.transform.position.y, 0.01f);
            Assert.AreEqual(4.0f, col.size.x, 0.01f);
            Assert.AreEqual(2.0f, col.size.y, 0.01f);
        }
    }
}
#endif
