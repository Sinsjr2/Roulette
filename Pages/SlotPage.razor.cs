using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Roulette.Messages.SlotPageMessage;
using Roulette.Models;
using TEA;

namespace Roulette.Pages {

    public partial class SlotPage {

        /// <summary>
        /// 各スロットのルーレットの現在の回転位置
        /// </summary>
        IEnumerable<int> roulettePositions;

        /// <summary>
        /// スロット1つを表します。
        /// </summary>
        IReadOnlyList<SlotModel> slots;

        public SlotPage() {
            slots = Enumerable.Repeat(new SlotModel(SlotPageModel.Default.ElementHeight, new[] { (0, 0) }, new[] { "0" }), 4).ToArray();
            roulettePositions = Enumerable.Repeat(0, slots.Count);
        }

        void OnChange(ChangeEventArgs e) {
            if (!int.TryParse(e.Value?.ToString(), out var pos)) {
                return;
            }
            roulettePositions = Enumerable.Repeat(pos, slots.Count);
        }

        [ParameterAttribute]
        public IDispatcher<ISlotPageMessage> Dispatcher { get; set; } = new BufferDispatcher<ISlotPageMessage>();

        SlotPageModel state = SlotPageModel.Default;

        ValueTask slotAnimationTask;

        [ParameterAttribute]
        public SlotPageModel State
        {
            get => state;
            set
            {
                var prevState = state;
                state = value;
                if (!slotAnimationTask.IsCompleted || !state.IsRunningSlot) {
                    return;
                }
                slotAnimationTask = StartAnimation();
            }
        }

        protected override void OnInitialized() {
             InvokeAsync(() => StartAnimation().AsTask());
        }

        async ValueTask StartAnimation() {
            try {
                var random = new Random();
                var candidateNumbers = State.CandidateNumbers.ToArray();

                var winner = State.TargetLotteryNumber;
                if (winner is null) {
                    return;
                }

                var targetPositions = LotteryUtil.CreateTargetPositions(
                    random,
                    elementHeight: State.ElementHeight,
                    slotRotateMaxSpeed: 150, 0, 2, 5,
                    minCountOfRotation: 10,
                    maxCountOfRotation: 20,
                    displayHeight: State.ElementHeight * 3,
                    candidateNumbers,
                    winner);

                slots = targetPositions.slotsContent
                    .Zip(targetPositions.targetPositions, (slot, positions) => new SlotModel(State.ElementHeight, positions, slot)).ToArray();

                await Task.Delay(100);
                var stopwatch = Stopwatch.StartNew();
                while (true) {
                    var elapsedTime = stopwatch.Elapsed;
                    this.roulettePositions = slots.Select(slot => slot.CalcTimedPos((int)elapsedTime.TotalMilliseconds));
                    this.StateHasChanged();
                    var isCompleted = slots.All(slot => slot.GetFinalTime() <= elapsedTime);
                    if (isCompleted) {
                        break;
                    }
                    await Task.Delay(30);
                }
                Dispatcher.Dispatch(new AddWinner(winner));
            }
             catch (Exception ex) {
                Console.Error.WriteLine(ex.ToString());
             }
        }
    }
}
