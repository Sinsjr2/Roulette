// using MoreLinq;

namespace Roulette.Models {

    /// <summary>
    /// 抽選を行うための処理
    /// </summary>
    public class LotteryUtil {

        /// <summary>
        /// 対象の値が検索対象の値から始まっていることを判定します。
        /// 対象の番号の方が検索対象の値のリストよりも短い場合はfalseになります。
        /// </summary>
        static bool StartWith<T>(IEnumerable<T> target, IEnumerable<T> search, IEqualityComparer<T>? comparer = default) {
            var comp = comparer ?? EqualityComparer<T>.Default;
            using var it = target.GetEnumerator();
            return search.All(x => it.MoveNext() && comp.Equals(it.Current, x));
        }

        /// <summary>
        /// 現在確定している番号から導きだされる候補
        /// 確定している番号がない場合は、候補全てを返します。
        /// </summary>
        static IEnumerable<LotteryNumber> SearchCandidateNumbers(IEnumerable<LotteryNumber> candidateNumbers, IEnumerable<char> decidedNumbers) {
            return candidateNumbers.Where(number => StartWith(number.SplitNumbers(), decidedNumbers));
        }

        /// <summary>
        /// 各桁ごとのリストに変換します。
        /// リストの1番目には、1桁目の値のリストが入っており、
        /// n番目には、2桁目の値のリストが入っています。
        /// </summary>
        static IEnumerable<IEnumerable<char>> ConvertToDigitsList(IEnumerable<LotteryNumber> candidateNumbers, IReadOnlyList<char> decidedNumbers) {
            var numbers = SearchCandidateNumbers(candidateNumbers, decidedNumbers);
            var splitedNumbers = numbers.Select(x => x.SplitNumbers().ToArray()).ToArray();
            var maxLength = splitedNumbers.MaxBy(xs => xs.Length)?.Length ?? 0;
            return decidedNumbers.Select(x => new[] { x })
                .Concat(
                    Enumerable.Range(decidedNumbers.Count, Math.Max(0, maxLength - decidedNumbers.Count))
                    .Select(
                        i => splitedNumbers
                        .Select(xs => i < xs.Length ? xs[i] : '0')));
        }

        /// <summary>
        /// スロットの最終の停止位置に到達するまでにランダムで停止する位置を計算します。
        /// 停止する時刻と位置(ピクセル)のペアを返します。
        /// </summary>
        public static IReadOnlyList<(int t, int pos)> RandomStopPositions(
            Random random, int elementHeight, int slotRotateMaxSpeed, int startPos, int randomStopMin, int minNumOfRandomStopPositions, int maxNumOfRandomStopPositions, int finalSlotItemPos) {

            // 最終の停止位置にとまるまでに最大で何回停止するか
            var numOfStopPositions = random.Next(minNumOfRandomStopPositions, maxNumOfRandomStopPositions);

            // ランダムに停止する位置を決定する
            var stopPositions = Enumerable.Range(0, numOfStopPositions)
                .Select(i => random.Next(randomStopMin, finalSlotItemPos))
                .Append(finalSlotItemPos)
                .Append(startPos)
                .OrderBy(pos => pos)
                .Distinct();

            return stopPositions
                .Select(i => (slotRotateMaxSpeed * i, i * elementHeight))
                .ToArray();
        }

        /// <summary>
        /// 無限回同じ値を返します。
        /// </summary>
        static IEnumerable<T> RepeatForever<T>(T value) {
            while (true) {
                yield return value;
            }
        }


        /// <summary>
        /// 抽選の結果になるように回転アニメーションのための停止位置を計算します。
        /// 候補のリストの中に当選者が含まれていることを期待しています。
        /// </summary>
        public static (IReadOnlyList<(int t, int pos)[]> targetPositions, IReadOnlyList<IReadOnlyList<string>> slotsContent) CreateTargetPositions(
            Random random, int elementHeight, int slotRotateMaxSpeed, int startPos, int minNumOfRandomStopPositions, int maxNumOfRandomStopPositions,
            int minCountOfRotation, int maxCountOfRotation,
            int displayHeight,
            IReadOnlyList<LotteryNumber> candidateNumbers, LotteryNumber winner
        ) {
            // スロットがとまる寸前になると初めの要素が表示されることにより、
            // そろそろ停止しそうなことがわかる可能性があるので
            // それを防ぐために余分にスロットの要素を表示させる目的で使用する
            var slotCountPlusAlfa = displayHeight / elementHeight + 1;

            // TODO 空文字の場合の考慮が必要かも
            var maxNumberLength = candidateNumbers.Select(num => num.SplitNumbers().Count()).Max();
            // それぞれのスロット停止するまで何回回転するか
            var countsOfRotation = Enumerable.Range(0, maxNumberLength)
                .Select(_ => random.Next(minCountOfRotation, maxCountOfRotation))
                .ToArray();

            var winnerNumbers = winner.SplitNumbers().ToArray();

            var slotsList =
                Enumerable.Range(0, maxNumberLength)
                .Select(i => ConvertToDigitsList(candidateNumbers, winnerNumbers.Take(i).ToArray())
                        .Select(xs => xs.ToArray())
                        .ToArray())
                .ToArray();

            // 各桁を1つ確定したときのスロットの要素数
            var numOfSlotElement = Enumerable.Range(0, maxNumberLength)
                .Select(i => slotsList[i][i].Length * countsOfRotation[i] + slotCountPlusAlfa)
                .ToArray();

            // 表示用のスロット
            var resultSlotsList =
                (from i in Enumerable.Range(0, maxNumberLength)
                 select (from j in Enumerable.Range(0, maxNumberLength)
                        from result in j <= i
                            ? RepeatForever(slotsList[j][i]).SelectMany(xs => xs).Take(numOfSlotElement[j]).Select(c => c.ToString())
                            : Enumerable.Empty<string>()
                        select result)
                        .ToArray())
                .ToArray();

            var finalStopPositions = resultSlotsList
                // ルーレットの最後からとまるインデックスを検索する
                .Select((xs, i) => Enumerable.Range(0, xs.Length - slotCountPlusAlfa)
                        .Select(j => xs.Length - j - 1)
                        .First(j => xs[j] == winnerNumbers[i].ToString()))
                .Reverse()
                .ToArray();

            var randomFinalStopPositions = finalStopPositions
                .Select((finalStopPos, i) =>
                    RandomStopPositions(random, elementHeight, slotRotateMaxSpeed, startPos,
                        i == 0 ? 0 : numOfSlotElement[i - 1],
                        minNumOfRandomStopPositions, maxNumOfRandomStopPositions, finalStopPos).ToArray())
                .ToArray();

            return (randomFinalStopPositions, resultSlotsList.Reverse().ToArray());
        }
    }

}
