using Roulette.Messages.SlotPageMessage;
using Roulette.Models;
using System.Diagnostics;

namespace Roulette.Pages
{
    public partial class Slot
    {
        /// <summary>
        /// 各スロットのルーレットの現在の回転位置
        /// </summary>
        IEnumerable<int> roulettePositions;

        /// <summary>
        /// スロット1つを表します。
        /// </summary>
        IReadOnlyList<SlotModel> slots;

        public Slot()
        {
            slots = Enumerable.Repeat(new SlotModel(SlotPageModel.Default.ElementHeight, new[] { (0, 0) }, new[] { "0" }), 4).ToArray();
            roulettePositions = Enumerable.Repeat(0, slots.Count);
        }

        async ValueTask StartAnimation()
        {
            try
            {
                var random = new Random();
                var candidateNumbers = State.CandidateNumbers.ToArray();

                var winner = State.TargetLotteryNumber;
                if (winner is null)
                {
                    return;
                }

                var targetPositions = LotterySerialCandidate.CreateTargetPositions(
                    random,
                    elementHeight: State.ElementHeight,
                    slotRotateMaxSpeed: 250, 0, 1, 10,
                    minCountOfRotation: 3,
                    maxCountOfRotation: 20,
                    displayHeight: State.ElementHeight * 3,
                    State.OriginalCandidateNumbers,
                    candidateNumbers,
                    winner,
                    "0123456789",
                    "0");

                slots = targetPositions.slotsContent
                    .Zip(targetPositions.targetPositions, (slot, positions) => new SlotModel(State.ElementHeight, positions, slot)).ToArray();

                await Task.Delay(100);
                var stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    var elapsedTime = stopwatch.Elapsed;
                    this.roulettePositions = slots.Select(slot => slot.CalcTimedPos((int)elapsedTime.TotalMilliseconds));
                    this.StateHasChanged();
                    var isCompleted = slots.All(slot => slot.GetFinalTime() <= elapsedTime);
                    if (isCompleted)
                    {
                        break;
                    }
                    await Task.Delay(30);
                }
                Dispatcher.Dispatch(new AddWinner(winner));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }
    }
}
