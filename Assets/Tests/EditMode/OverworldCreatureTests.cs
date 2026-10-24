#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DigiPurity.World;

namespace DigiPurity.Tests
{
    public class OverworldCreatureTests
    {
        private GameObject _creatureObj;
        private OverworldCreature _creature;

        [SetUp]
        public void SetUp()
        {
            _creatureObj = new GameObject("Test_Creature");
            _creature = _creatureObj.AddComponent<OverworldCreature>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_creatureObj != null) Object.DestroyImmediate(_creatureObj);
        }

        [Test]
        public void EnsureCollider_AddsSolidBoxCollider2D_WhenMissing()
        {
            var col = _creature.EnsureCollider();

            Assert.IsNotNull(col, "Creature must have a BoxCollider2D.");
            Assert.IsFalse(col.isTrigger, "Creature collider must be solid (isTrigger = false) to prevent Owen phasing through.");
        }

        [Test]
        public void EnsureCollider_ForcesNonTrigger_WhenColliderWasTrigger()
        {
            var col = _creature.EnsureCollider();
            col.isTrigger = true;

            _creature.EnsureCollider();

            Assert.IsFalse(col.isTrigger, "EnsureCollider must force isTrigger to false.");
        }

        [Test]
        public void AutoFitCollider_FootprintMode_AlignsToFeet()
        {
            var sr = _creatureObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = _creatureObj.AddComponent<SpriteRenderer>();

            // Create a 2x4 test sprite (Pivot center: min Y = -2, max Y = +2)
            Texture2D tex = new Texture2D(32, 64);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0.5f), 16f); // 2 units wide, 4 units tall

            _creature.Mode = OverworldCreature.ColliderFitMode.Footprint;
            _creature.FootprintHeightRatio = 0.50f; // Bottom 50% = 2 units tall
            _creature.FootprintWidthRatio = 0.80f;  // 80% width = 1.6 units wide

            _creature.AutoFitCollider(false);

            var col = _creature.BoxCollider;
            Assert.AreEqual(1.6f, col.size.x, 0.01f);
            Assert.AreEqual(2.0f, col.size.y, 0.01f);

            // Sprite min Y is -2.0f. Half height is 1.0f. Center offset Y should be -2.0 + 1.0 = -1.0f.
            Assert.AreEqual(-1.0f, col.offset.y, 0.01f);

            Object.DestroyImmediate(tex);
        }

        [Test]
        public void AutoFitCollider_FootprintOffsets_ShiftsColliderByExactOffsets()
        {
            var sr = _creatureObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = _creatureObj.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(32, 64);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 64), new Vector2(0.5f, 0.5f), 16f); // 2x4 units

            _creature.Mode = OverworldCreature.ColliderFitMode.Footprint;
            _creature.FootprintHeightRatio = 0.50f;
            _creature.FootprintWidthRatio = 0.80f;
            _creature.FootprintOffsetX = 0.75f;
            _creature.FootprintOffsetY = -0.25f;

            _creature.AutoFitCollider(false);

            var col = _creature.BoxCollider;
            Assert.AreEqual(0.75f, col.offset.x, 0.01f, "Offset X should match FootprintOffsetX.");
            Assert.AreEqual(-1.25f, col.offset.y, 0.01f, "Offset Y should include FootprintOffsetY (-1.0 + -0.25 = -1.25).");

            Object.DestroyImmediate(tex);
        }

        [Test]
        public void AutoFitCollider_FullBodyMode_MatchesSpriteBounds()
        {
            var sr = _creatureObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = _creatureObj.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(32, 32);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f); // 2x2 units

            _creature.Mode = OverworldCreature.ColliderFitMode.FullBody;
            _creature.AutoFitCollider(true);

            var col = _creature.BoxCollider;
            Assert.AreEqual(2.0f, col.size.x, 0.01f);
            Assert.AreEqual(2.0f, col.size.y, 0.01f);
            Assert.AreEqual(0.0f, col.offset.x, 0.01f);
            Assert.AreEqual(0.0f, col.offset.y, 0.01f);

            Object.DestroyImmediate(tex);
        }

        [Test]
        public void DepthSorting_LowerYPosition_ProducesHigherSortingOrder()
        {
            var sr = _creatureObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = _creatureObj.AddComponent<SpriteRenderer>();

            _creatureObj.transform.position = new Vector3(0f, 5f, 0f);
            _creature.UpdateDepthSorting();
            int northOrder = sr.sortingOrder;

            _creatureObj.transform.position = new Vector3(0f, -5f, 0f);
            _creature.UpdateDepthSorting();
            int southOrder = sr.sortingOrder;

            Assert.Greater(southOrder, northOrder, "Creatures positioned further south (lower Y) must have a higher sorting order to render in front.");
        }

        [Test]
        public void PhysicalCollider_StopsPhysicsRaycast()
        {
            // Position creature at (3, 0) with a 2x2 solid box
            _creatureObj.transform.position = new Vector3(3f, 0f, 0f);
            var col = _creature.EnsureCollider();
            col.size = new Vector2(2f, 2f);
            col.offset = Vector2.zero;

            // Cast from (0, 0) towards creature along +X
            RaycastHit2D hit = Physics2D.Raycast(Vector2.zero, Vector2.right, 10f);

            Assert.IsNotNull(hit.collider, "Raycast should hit creature's solid BoxCollider2D.");
            Assert.AreEqual(col, hit.collider);
            // Distance should be 3.0 - 1.0 (half width) = 2.0 units
            Assert.AreEqual(2.0f, hit.distance, 0.05f);
        }
    }
}
#endif
