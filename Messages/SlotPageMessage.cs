using Roulette.Models;

namespace Roulette.Messages.SlotPageMessage {

    public interface ISlotPageMessage { }

    /// <summary>
    /// 抽選対象の番号をCSVファイルから読み出します。
    /// CSVファイルには、1列目に番号、2列目に表示名が並んでいる必要があります。
    /// </summary>
    public record OnLoadCSVFile(string CSVText) : ISlotPageMessage;

    /// <summary>
    /// スロットを止めて、番号が確定したことを通知します。
    /// </summary>
    public record OnStopSlotWithNumber(char Number) : ISlotPageMessage;

    /// <summary>
    /// 目標の番号に従って、順番にスロットを止めます。
    /// すべてのスロットが止まっており、とめる番号がない場合は、何もしません。
    /// </summary>
    public record OnStopSlot : ISlotPageMessage;

    /// <summary>
    /// 当選者を追加します。
    /// </summary>
    public record AddWinner(LotteryNumber Winner) : ISlotPageMessage;

    /// <summary>
    /// 抽選をするためにスロットの動作を開始します。
    /// 抽選をするための乱数が必要です。
    /// 数値は、0から1未満の範囲である必要があります。
    /// </summary>
    public record OnStartSlot(double randomNumber) : ISlotPageMessage;

    /// <summary>
    /// 抽選開始ボタンが押されたことを通知します。
    /// </summary>
    public record OnClickStart : ISlotPageMessage;
}
