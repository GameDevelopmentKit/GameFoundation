namespace DataManager.LocalSave.Tests.Editor
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Handler;
    using NUnit.Framework;

    [TestFixture]
    public class ProfileSaveGateTests
    {
        [Test]
        public void ConcurrentRequests_RunActiveSaveAndOneFollowUpPass()
        {
            var gate = new ProfileSaveGate();
            var firstSaveBlocker = new UniTaskCompletionSource<bool>();
            var savePassCount = 0;

            UniTask SaveOnce()
            {
                savePassCount++;
                return savePassCount == 1 ? firstSaveBlocker.Task : UniTask.CompletedTask;
            }

            var firstRequest = gate.RequestSave(SaveOnce);

            Assert.IsTrue(gate.IsSaving);
            Assert.AreEqual(1, savePassCount);

            var secondRequest = gate.RequestSave(SaveOnce);
            var thirdRequest = gate.RequestSave(SaveOnce);

            Assert.AreEqual(1, savePassCount, "Additional requests should not start parallel save passes.");

            firstSaveBlocker.TrySetResult(true);
            UniTask.WhenAll(firstRequest, secondRequest, thirdRequest).GetAwaiter().GetResult();

            Assert.AreEqual(2, savePassCount, "Requests made during an active save should trigger one follow-up pass.");
            Assert.IsFalse(gate.IsSaving);
        }

        [Test]
        public void RequestDuringFollowUpPass_RunsAnotherPass()
        {
            var gate = new ProfileSaveGate();
            var firstSaveBlocker = new UniTaskCompletionSource<bool>();
            var secondSaveBlocker = new UniTaskCompletionSource<bool>();
            var savePassCount = 0;

            UniTask SaveOnce()
            {
                savePassCount++;
                return savePassCount switch
                {
                    1 => firstSaveBlocker.Task,
                    2 => secondSaveBlocker.Task,
                    _ => UniTask.CompletedTask
                };
            }

            var firstRequest = gate.RequestSave(SaveOnce);

            var secondRequest = gate.RequestSave(SaveOnce);
            firstSaveBlocker.TrySetResult(true);

            SpinWait.SpinUntil(() => savePassCount == 2, TimeSpan.FromSeconds(1));
            Assert.AreEqual(2, savePassCount, "The second request should start one follow-up pass.");

            var thirdRequest = gate.RequestSave(SaveOnce);
            secondSaveBlocker.TrySetResult(true);

            UniTask.WhenAll(firstRequest, secondRequest, thirdRequest).GetAwaiter().GetResult();

            Assert.AreEqual(3, savePassCount, "A request made during the follow-up pass should not be lost.");
            Assert.IsFalse(gate.IsSaving);
        }

        [Test]
        public void FailedSave_PropagatesExceptionAndClearsSavingState()
        {
            var gate = new ProfileSaveGate();
            var expected = new InvalidOperationException("save failed");

            async UniTask SaveOnce()
            {
                await UniTask.CompletedTask;
                throw expected;
            }

            var request = gate.RequestSave(SaveOnce);
            var actual = Assert.Throws<InvalidOperationException>(() => request.GetAwaiter().GetResult());

            Assert.AreSame(expected, actual);
            Assert.IsFalse(gate.IsSaving);
        }
    }
}
