namespace Roulette.Models {

    public record SlotModel {
        /// <summary>
        /// 表示するスロットの1つの要素の高さ
        /// </summary>
        public readonly int ElementHeight;

        /// <summary>
        /// スロットの要素を複数表示するときの高さ
        /// </summary>
        public readonly int DisplayHeightPX;

        /// <summary>
        /// 停止する目標の位置位置とその目標に到達する時刻(ミリ秒)
        /// </summary>
        readonly (int t, int pos)[] targetPositions;

        /// <summary>
        /// スロットに表示する要素
        /// </summary>
        readonly IReadOnlyList<string> slotElements;

        public SlotModel(int elementHeight, (int t, int pos)[] targetPositions, IReadOnlyList<string> slotElements) {
            ElementHeight = elementHeight;
            DisplayHeightPX = elementHeight * 3;
            this.targetPositions = targetPositions;
            this.slotElements = slotElements;
        }

        /// <summary>
        /// スロットの最終の停止位置に到達するまでにランダムで停止する位置を計算します。
        /// 停止する時刻と位置(ピクセル)のペアを返します。
        /// </summary>
        public static IReadOnlyList<(int t, int pos)> RandomStopPositions(
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

        public (int pos, string content)[] GetNewPosition(int posPX) {
            // // 中心位置を0点とする
            var displayOffset = (DisplayHeightPX - ElementHeight) / 2;
            var offsetedPos = posPX - displayOffset;
            // 見きれないように要素を表示する
            var count = DisplayHeightPX / ElementHeight + 1;
            var beginIndex = DivFloor(offsetedPos, ElementHeight);
            var end = beginIndex + count;
            return (
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

        /// <summary>
        /// 指定した時間の時どの位置かを計算します。
        /// </summary>
        public int CalcTimedPos(float elapsedMS) {
            // targetPositions は時刻0からスタートしていることが前提
            var foundPos = targetPositions.LowerBound((t:(int)elapsedMS, pos:0), static (a, b) =>  a.t - b.t);
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
                : SinAnimation.SineInOut(elapsedMS - x1.t, totalTime, x1.pos, x2.pos);
        }

        /// <summary>
        /// アニメーションが終了し、スロットの回転が停止する時刻を返します。
        /// アニメーションするための位置が設定されていない場合は0を返します。
        /// </summary>
        public TimeSpan GetFinalTime() {
            return targetPositions.Any()
                ? TimeSpan.FromMilliseconds(targetPositions[targetPositions.Length - 1].t)
                : TimeSpan.Zero;
        }

        // 負の値の時、床関数の様に振る舞います。
        static int DivFloor(int x, int a) {
            return x < 0
                ? (x - a) / a
                : x / a;
        }

    }
}
