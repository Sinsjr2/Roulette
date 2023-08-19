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
        readonly IReadOnlyList<SlotModel> slots;

        /// <summary>
        /// スロットに表示する要素
        /// </summary>
        readonly IReadOnlyList<string> slotElements = new[] {
            "1", "5", "20", "100", "111"
        };

        public SlotPage() {
            var random = new Random();

            // ルーレットが何回回転するか
            var finalSlotItemPositions = new[] {
                4, 20, 10
            };

            slots = finalSlotItemPositions.Select(
                pos => new SlotModel(
                    elementHeight: 30,
                    targetPositions: SlotModel.RandomStopPositions(
                        random,
                        elementHeight: 30,
                        slotRotateMaxSpeed: 150, 0, 2, 5, pos).ToArray(),
                    slotElements)
            ).ToArray();

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

        [ParameterAttribute]
        public SlotPageModel State { get; set; } = SlotPageModel.Default;

        protected override void OnInitialized() {
            InvokeAsync(() => StartAnimation().AsTask());
        }

        async ValueTask StartAnimation() {
            try {
                await Task.Delay(100);
                var stopwatch = Stopwatch.StartNew();
                while (true) {
                    var elapsedTime = stopwatch.Elapsed;
                    this.roulettePositions = slots.Select(slot => slot.CalcTimedPos((int)elapsedTime.TotalMilliseconds));
                    this.StateHasChanged();
                    var isCompleted = slots.All(slot => slot.GetFinalTime() <= elapsedTime);
                    if (isCompleted) {
                        return;
                    }
                    await Task.Delay(30);
                }
            }
             catch (Exception ex) {
                Console.Error.WriteLine(ex.ToString());
             }
        }
    }
}
