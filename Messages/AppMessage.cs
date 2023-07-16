using Roulette.Messages.SlotPageMessage;

namespace Roulette.Messages.AppMessage {

    public interface IAppMessage {}

    public record SlotPageMessageInApp(ISlotPageMessage Message) : IAppMessage;
}
