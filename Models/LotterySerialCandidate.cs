namespace Roulette.Models
{

    /// <summary>
    /// スロットに表示する内容を引数で指定します。
    /// その引数に候補リストの文字が存在しない場合は追加します。
    /// スロットに表示する内容はソートします。
    /// </summary>
    public class LotterySerialCandidate {

        /// <summary>
        /// 抽選の結果になるように回転アニメーションのための停止位置を計算します。
        /// 候補のリストの中に当選者が含まれていることを期待しています。
        ///
        /// 以前に当選した番号も含めて全ての番号が必要です。
        ///
        /// 桁数が揃っていない場合、桁数を揃えるために挿入する文字の種類を設定する必要があります。
        /// </summary>
        public static (IReadOnlyList<(int t, int pos)[]> targetPositions, IReadOnlyList<IReadOnlyList<string>> slotsContent) CreateTargetPositions(
            Random random, int elementHeight, int slotRotateMaxSpeed, int startPos, int minNumOfRandomStopPositions, int maxNumOfRandomStopPositions,
            int minCountOfRotation, int maxCountOfRotation,
            int displayHeight,
            IReadOnlyList<LotteryNumber> allNumbers,
            IReadOnlyList<LotteryNumber> candidateNumbers, LotteryNumber winner,
            string slotChars,
            string paddingChar) {
            if (!candidateNumbers.Any()) {
                return (Array.Empty<(int t, int pos)[]>(), Enumerable.Repeat(new[] { "0" }, 4).ToArray());
            }

            // スロットに表示する文字の種類を抽出する
            var slotCharKinds = candidateNumbers
                .SelectMany(num => num.Number.Chunk(1))
                .Concat(slotChars.Chunk(1))
                .Select(xs => new string(xs))
                .Append(paddingChar)
                .Distinct(StringComparer.Ordinal)
                // 表現のゆらぎがあったとしても、見やすいようにソートする
                .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // スロットの最大桁数
            var maxCandidateNumberLength = allNumbers
                .Select(num => num.Number.Chunk(1).Count())
                .Max();

            // 正解の数字の位置
            // 何回スロットを回転させるのか指定したあとに、オフセットさせる位置
            var winnerSlotOffset =
                // 桁数を揃える
                Enumerable.Repeat(paddingChar, Math.Max(0, maxCandidateNumberLength - winner.Number.Chunk(1).Count()))
                .Concat(winner.Number
                    .Chunk(1)
                    .Select(kind => new string(kind)))
                .Select(kind => slotCharKinds.Select((x, i) => (x, i)).First(t => t.x == kind).i)
                .ToArray();

            // スロットを回転させる量
            var slotRotateCounts = Enumerable.Range(0, maxCandidateNumberLength)
                .Select(_ => random.Next(minCountOfRotation, maxCountOfRotation))
                //下の桁から確定するために、大きい値にするためにソート
                .OrderByDescending(x => x)
                .ToArray();

            // スロットが最終的に停止する位置
            var finalStopPositions =
                slotRotateCounts.Zip(winnerSlotOffset,
                                     (rotateCount, offset) => rotateCount * slotCharKinds.Length + offset)
                .Select(finalStopPos => LotteryUtil.RandomStopPositions(random, elementHeight, slotRotateMaxSpeed, startPos, 1,
                        minNumOfRandomStopPositions, maxNumOfRandomStopPositions, finalStopPos).ToArray())
                .ToArray();

            return (finalStopPositions, Enumerable.Repeat(slotCharKinds, maxCandidateNumberLength).ToArray());
        }
    }
}
