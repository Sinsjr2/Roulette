using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Roulette.Messages.SlotPageMessage;
using TEA;

namespace Roulette.Models {

    /// <summary>
    /// 抽選対象の番号とその表示名
    /// </summary>
    public record LotteryNumber {

        /// <summary>
        /// 番号を表示するときの名前
        /// </summary>
        public readonly string DisplayName;

        public readonly string Number;

        public LotteryNumber(string number, string displayName) {
            if (!number.Any()) {
                throw new ArgumentException("1文字以上の番号を設定して下さい");
            }
            this.Number = number;
            DisplayName = displayName;
        }

        /// <summary>
        /// 1桁(0~9)ごとに番号を区切ります。
        /// 戻り値には、先頭から1桁目、2桁目、…のように順番に入っています。
        /// </summary>
        public IEnumerable<char> SplitNumbers() {
            return Number.Reverse();
        }
    }

    public record SlotPageModel : IUpdate<SlotPageModel, ISlotPageMessage> {

        /// <summary>
        /// 抽選対象の番号
        /// 全件数
        /// </summary>
        public IReadOnlyList<LotteryNumber> OriginalCandidateNumbers { get; private init; }

        /// <summary>
        /// 当選者を除いた抽選対象の番号
        /// </summary>
        public IEnumerable<LotteryNumber> CandidateNumbers =>
            OriginalCandidateNumbers
            .Where(number => !Winners.Any(winner => winner.Number == number.Number));

        /// <summary>
        /// 1桁目から確定した番号が入っていく
        /// </summary>
        IReadOnlyList<char> DecidedNumbers { get; init; }


        /// <summary>
        /// 抽選の結果止める番号
        /// </summary>
        public LotteryNumber? TargetLotteryNumber { get; private init; }

        /// <summary>
        /// 抽選中でルーレットが回転しているかどうか
        /// </summary>
        public bool IsRunningSlot { get; private init; } = false;

        /// <summary>
        /// 抽選の結果あった人
        /// 次回の抽選する時に除く人
        /// </summary>
        public IReadOnlyList<LotteryNumber> Winners { get; private init; } = Array.Empty<LotteryNumber>();

        /// <summary>
        /// スロット1つを表します。
        /// </summary>
        public IReadOnlyList<SlotModel> Slots { get; private init; }

        /// <summary>
        /// 現在回転しているスロットはtrue、停止しているスロットはfalseを返します。
        /// </summary>
        public IEnumerable<bool> SlotsRunningStatus {
            get {
                var stopSlotsLength = IsRunningSlot
                    ? CandidateNumbers
                    .Select(number => number.SplitNumbers().Count())
                    .Max()
                    : DecidedNumbers.Count;
                // 範囲外にはみ出さないようにするため
                var numOfDecidedSlots = Math.Min(stopSlotsLength, DecidedNumbers.Count);
                return Enumerable.Repeat(false, numOfDecidedSlots)
                    .Concat(Enumerable.Repeat(true, stopSlotsLength - numOfDecidedSlots));
            }
        }

        /// <summary>
        /// スロットの1要素当たりの高さ
        /// </summary>
        public int ElementHeight { get; private set; }

        public static readonly SlotPageModel Default = new();

        private SlotPageModel() {
            OriginalCandidateNumbers = Array.Empty<LotteryNumber>();
            DecidedNumbers = Array.Empty<char>();
            ElementHeight = 30;
            Slots = Array.Empty<SlotModel>();
        }

        public SlotPageModel(int elementHeight, IReadOnlyList<LotteryNumber> candidateNumbers, IReadOnlyList<char> decidedNumbers) {
            ElementHeight = elementHeight;
            OriginalCandidateNumbers = candidateNumbers;
            DecidedNumbers = decidedNumbers;
            Slots = Array.Empty<SlotModel>();
        }

        /// <summary>
        /// 指定した番号をスロットが止まったときの番号として確定します。
        /// 候補に無い番号であったり、
        /// 全ての番号が確定していた場合は、何もしません。
        /// </summary>
        public SlotPageModel AddDecidedNumber(char number) {
            if (SlotsRunningStatus.All(isRunning => !isRunning)) {
                return this;
            }
            var newDecidedNumbers = this.DecidedNumbers.Append(number).ToArray();
            bool hasCandidateNumber = SearchCandidateNumbers().Any(x => StartWith(x.SplitNumbers(), newDecidedNumbers));
            if (!hasCandidateNumber) {
                return this;
            }
            var newState = this with {
                DecidedNumbers = newDecidedNumbers,
            };

            var isCompleted = newState.SlotsRunningStatus.All(isRunning => !isRunning);
            return newState with {
                IsRunningSlot = !isCompleted,
                    Winners = !isCompleted ? Winners:
                    TargetLotteryNumber is null ? Array.Empty<LotteryNumber>()
                    : Winners.Append(TargetLotteryNumber).ToArray()
            };
        }

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
        public IEnumerable<LotteryNumber> SearchCandidateNumbers() {
            return CandidateNumbers.Where(number => StartWith(number.SplitNumbers(), this.DecidedNumbers));
        }

        /// <summary>
        /// 各桁ごとのリストに変換します。
        /// リストの1番目には、1桁目の値のリストが入っており、
        /// n番目には、2桁目の値のリストが入っています。
        /// </summary>
        public IEnumerable<IEnumerable<char>> ConvertToDigitsList() {
            var numbers = this.SearchCandidateNumbers();
            var splitedNumbers = numbers.Select(x => x.SplitNumbers().ToArray()).ToArray();
            var maxLength = splitedNumbers.MaxBy(xs => xs.Length)?.Length ?? 0;
            return this.DecidedNumbers.Select(x => new[] { x })
                .Concat(
                    Enumerable.Range(DecidedNumbers.Count, Math.Max(0, maxLength - DecidedNumbers.Count))
                    .Select(
                        i => splitedNumbers
                        .Select(xs => i < xs.Length ? xs[i] : '0')));
        }

        /// <summary>
        /// 実行するごとに次のスロットを止めます。
        /// 止めるスロットがない場合は何もしません。
        /// </summary>
        SlotPageModel StopNextSlot() {
            if (TargetLotteryNumber is null) {
                return this;
            }
            var length = TargetLotteryNumber.Number.Length;
            return DecidedNumbers.Count < length
                ? AddDecidedNumber(TargetLotteryNumber.Number[length - DecidedNumbers.Count - 1])
                : this;
        }

        /// <summary>
        /// 抽選を行い、停止させる番号を確定し、スロットの回転を開始します。
        /// 数値は、0から1未満の範囲である必要があります。
        /// </summary>
        SlotPageModel SelectRandomNumberAndStart(double randomNumber) {
            if (!(randomNumber is >= 0.0 and < 1.0)) {
                throw new ArgumentOutOfRangeException(nameof(randomNumber));
            }

            var candidateNumber = CandidateNumbers.ToArray();

            // 全員が既に当選していた場合は、スロットを開始せずに終了とする
            if (!candidateNumber.Any()) {
                return this with {
                    IsRunningSlot = false,
                    TargetLotteryNumber = null,
                    DecidedNumbers = Array.Empty<char>()
                };
            }

            var index = (int)(candidateNumber.Length * randomNumber);
            return this with {
                IsRunningSlot = true,
                TargetLotteryNumber = candidateNumber[index],
                DecidedNumbers = Array.Empty<char>()
            };
        }

        /// <summary>
        /// CSVデータから抽選対象の番号を設定します。
        /// 抽選結果もリセットします。
        /// </summary>
        SlotPageModel SetCandidateNumbersFromCSVText(string csvText) {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                HasHeaderRecord = false,
            };
            var numbers = new List<LotteryNumber>();
            var reader = new CsvReader(new StringReader(csvText), config);
            while (reader.Read()) {
                numbers.Add(
                    new LotteryNumber(
                        reader.TryGetField(0, out string? number) ? (number ?? "0") : "0",
                        reader.TryGetField(1, out string? name) ? (name ?? "") : ""));
            }

            return this with {
                OriginalCandidateNumbers = numbers.Select(x => new LotteryNumber(x.Number, x.DisplayName)).ToArray(),
                IsRunningSlot = false,
                TargetLotteryNumber = null,
                DecidedNumbers = Array.Empty<char>(),
                Winners = Array.Empty<LotteryNumber>()
            };
        }

        public SlotPageModel Update(ISlotPageMessage message) {
            return message switch {
                OnStopSlotWithNumber msg => AddDecidedNumber(msg.Number),
                    AddWinner(var winner) => this with { Winners = Winners.Append(winner).ToArray(), IsRunningSlot = false },
                OnStartSlot(var randomNum) => SelectRandomNumberAndStart(randomNum),
                OnStopSlot => StopNextSlot(),
                OnClickStart => this,
                OnLoadCSVFile(var csvText) => SetCandidateNumbersFromCSVText(csvText),
                _ => throw new ArgumentException(message?.ToString())
            };
        }
    }
}
