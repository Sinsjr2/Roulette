using System.ComponentModel.Design;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Roulette.Messages.SlotPageMessage;
using Roulette.Models;
using TEA;

namespace Roulette.Pages {

    public partial class SlotPage {

        /// <summary>
        /// スロットに表示する内容とその表示する座標
        /// </summary>
        public (int pos, string content)[] SlotPositionAndContents { get;  set; } = Array.Empty<(int pos, string content)>();
        readonly int ElementHeight = 30;
        readonly int DisplayHeightPX;

        /// <summary>
        /// 停止する目標の位置位置とその目標に到達する時刻(ミリ秒)
        /// </summary>
        readonly (int t, int pos)[] targetPositions;

        // スロットの1要素が切り替わるときの最大速度
        readonly int slotRotateMaxSpeed = 150;


        /// <summary>
        /// スロットに表示する要素
        /// </summary>
        readonly IReadOnlyList<string> slotElements = new[] {
            "1", "5", "20", "100", "111"
        };

        // /// <summary>
        // /// 相対時間から絶対時間に変更します。
        // /// </summary>
        // static IEnumerable<(int t, int pos)> DeltaTimeToAbsTime(int beginPos, IEnumerable<(int deltaTime, int pos)> xs) {
        //     int total = 0;
        //     yield return (total, beginPos);
        //     foreach (var (deltaTime, pos) in xs) {
        //         total += deltaTime;
        //         yield return (total, pos);
        //     }
        // }

        // /// <summary>
        // /// スロットの表示インデックスを座標に変換します。
        // /// </summary>
        // static IEnumerable<(int t, int pos)> SlotIndexPosToPxPos(IEnumerable<int> indexPositions) {
        //     var slotRotateSpeed = 150;
        //     // 変化する
        //     int CalsDeltaTimeMS(int prevIndex, int targetIndex) {

        //     }
        //     indexPositions.

        // }

        public SlotPage() {
            DisplayHeightPX = ElementHeight * 3;
            // targetPositions = new[] {
            //     (0, 0), (10000, DisplayHeightPX * 2), (15000, DisplayHeightPX * 23), (16000, DisplayHeightPX * 26)
            // };
            var random = new Random();
            // ルーレットが何回回転するか

            var finalSlotItemPos = 1;
            
            targetPositions = RandomSlopPositions(random, ElementHeight, this.slotRotateMaxSpeed, 0, 2, 5, finalSlotItemPos)
                .ToArray();
            // DeltaTimeToAbsTime(0, new[] {
            //     (10000, DisplayHeightPX * 2), (5000, DisplayHeightPX * 23), ()
            // })
        }

        static IReadOnlyList<(int t, int pos)> RandomSlopPositions(
            Random random, int elementHeight, int slotRotateMaxSpeed, int startPos, int minNumOfRandomStopPositions, int maxNumOfRandomStopPositions, int finalSlotItemPos) {

            // 最終の停止位置にとまるまでに最大で何回停止するか
            var numOfStopPositions = random.Next(minNumOfRandomStopPositions, maxNumOfRandomStopPositions);

            // ランダムに停止する位置を決定する
            var stopPositions = Enumerable.Range(0, numOfStopPositions)
                .Select(i => random.Next(startPos, finalSlotItemPos))
                .Append(finalSlotItemPos)
                .Append(startPos)
                .OrderBy(pos => pos)
                .Distinct();

            return stopPositions
                .Select(i => (slotRotateMaxSpeed * i, i * elementHeight))
                .ToArray();
        }

        void OnChange(ChangeEventArgs e) {
            if (!int.TryParse(e.Value?.ToString(), out var pos)) {
                return;
            }
            ApplyPosition(pos);
        }

        // 負の値の時、床関数の様に振る舞います。
        static int DivFloor(int x, int a) {
            return x < 0
                ? (x - a) / a
                : x / a;
        }

        void ApplyPosition(int posPX) {
            // int Trace(string str) {
            //     Console.WriteLine(str);
            //     return 0;
            // }
            // // 中心位置を0点とする
            var displayOffset = (DisplayHeightPX - ElementHeight) / 2;
            var offsetedPos = posPX - displayOffset;
            // 見きれないように要素を表示する
            var count = DisplayHeightPX / ElementHeight + 1;
            var beginIndex = DivFloor(offsetedPos, ElementHeight);
            Console.WriteLine(beginIndex);
            var end = beginIndex + count;
            SlotPositionAndContents = (
                from i in Enumerable.Range(beginIndex, count)
                let i2 = i % slotElements.Count
                let elementIndex = i2 < 0 ? slotElements.Count + i2 : i2
                let nextScrollPos = DisplayHeightPX - ElementHeight - i * ElementHeight + offsetedPos
                select (CalcPos(nextScrollPos), slotElements[elementIndex]))
                .ToArray();
        }

        int CalcPos(int px) {
            var cycle = DisplayHeightPX + ElementHeight;
            // 座標が0よりも小さくなった場合に、見きれないようにするためのオフセット
            var offset = ElementHeight;
            var mod = (px  + offset) % cycle;
            var x2 = mod < 0 ? cycle + mod : mod;
            return x2 - offset;
        }

        [ParameterAttribute]
        public IDispatcher<ISlotPageMessage> Dispatcher { get; set; } = new BufferDispatcher<ISlotPageMessage>();

        [ParameterAttribute]
        public SlotPageModel State { get; set; } = SlotPageModel.Default;

        protected override void OnInitialized() {
            //InvokeAsync(() => StartAnimation().AsTask());
        }

        int CalcTimedPos(float elapsedMS) {
            // targetPositions は時刻0からスタートしていることが前提
            var foundPos = LowerBound(targetPositions, (t:(int)elapsedMS, pos:0), static (a, b) =>  a.t - b.t);
            var i = !(foundPos < targetPositions.Length) || !(elapsedMS < targetPositions[foundPos].t) ? foundPos
                // まだ、期待する数値に到達していないので一つ前の値を参照する
                : Math.Max(0, foundPos - 1); // アンダーフロー対策

            if (!(i < targetPositions.Length - 1)) {
                return targetPositions[targetPositions.Length - 1].pos;
            }
            var x1 = targetPositions[i];
            var x2 = targetPositions[i + 1];
            // 変化するのにかかる合計時間
            var totalTime = x2.t - x1.t;

            return x2.t <= elapsedMS
                ? x2.pos
                : SineInOut(elapsedMS - x1.t, totalTime, x1.pos, x2.pos);
        }

        // static int BoundLower<T>(IReadOnlyList<T> xs, T target, Func<T, T, int> compare) {
        //     // 要素数が0のときは-1
        //     if (xs.Count <= 0) {
        //         return -1;
        //     }

        //     int begin = 0;
        //     int end = xs.Count;
        //     while (true) {
        //         // 割る2
        //         int i = begin + ((end - begin) >> 2);
        //         var result = compare(xs[i], target);
        //         // TODO 見直し
        //         if (0 <= result) {
        //             begin = i;
        //         }
        //         else {
        //             end = i;
        //         }
        //         if (begin == end) {
        //             return i;
        //         }
        //     }
        // }

        static int LowerBound<T>(IReadOnlyList<T> xs, T v, Func<T, T, int> compare) {
            return BoundCore(xs, v, compare, -1);
        }

        static int UpperBound<T>(IReadOnlyList<T> xs, T v, Func<T, T, int> compare) {
            return BoundCore(xs, v, compare, 0);
        }

        static int BoundCore<T>(IReadOnlyList<T> xs, T v, Func<T, T, int> compare, int boundValue) {
            // TODO 空の配列の場合のエラーチェックを行う かそれとも0をかえすか
            var l = 0;
            var r = xs.Count - 1;
            while (l <= r) {
                var mid = l + ((r - l) >> 1);
                var res = compare(xs[mid], v);
                if (res <= boundValue) {
                    l = mid + 1;
                } else {
                    r = mid - 1;
                }
            }
            return l;
        }

        public static int SineInOut(float t, float totaltime, int min, int max) {
            var delta = max - min;
            return (int)(-delta / 2 * (Math.Cos(t * Math.PI / totaltime) - 1) + min);
        }

        async ValueTask StartAnimation() {
            try {
                await Task.Delay(100);
                var stopwatch = Stopwatch.StartNew();
                while (true) {
                    ApplyPosition(CalcTimedPos(stopwatch.ElapsedMilliseconds));
                    this.StateHasChanged();
                    await Task.Delay(30);
                }
            }
             catch (Exception ex) {
                Console.Error.WriteLine(ex.ToString());
             }   
        }
    }
}
