using NUnit.Framework;
using UnityEngine;
using MechaSurvivor.Core;
using MechaSurvivor.Gameplay;

namespace MechaSurvivor.Tests.EditMode
{
    /// <summary>체력 컴포넌트의 피격/사망 알림 계약 검증.</summary>
    public sealed class HealthTests
    {
        private GameObject _go;
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("HealthTest");
            _health = _go.AddComponent<Health>();
            _health.Init(10f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void TakeDamage_RaisesDamagedWithAmount()
        {
            float received = 0f;
            _health.Damaged += (amount, info) => received = amount;

            _health.TakeDamage(3f);

            Assert.AreEqual(3f, received, 1e-4f, "피격 이벤트가 적용량과 함께 올라와야 한다.");
            Assert.AreEqual(7f, _health.Current, 1e-4f);
        }

        [Test]
        public void LethalDamage_RaisesDamagedThenDied()
        {
            int damagedCount = 0;
            bool died = false;
            _health.Damaged += (a, i) => damagedCount++;
            _health.Died += h => died = true;

            _health.TakeDamage(999f);

            Assert.AreEqual(1, damagedCount);
            Assert.IsTrue(died);
        }

        [Test]
        public void DamageAfterDeath_RaisesNothing()
        {
            _health.TakeDamage(999f);
            int damagedCount = 0;
            _health.Damaged += (a, i) => damagedCount++;

            _health.TakeDamage(1f);

            Assert.AreEqual(0, damagedCount, "죽은 대상은 더 이상 피격 이벤트를 올리지 않는다.");
        }
    }
}
