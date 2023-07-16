using Roulette.Messages.AppMessage;
using TEA;

namespace Roulette.Models {

    public record AppModel(
        SlotPageModel SlotPage
    ) : IUpdate<AppModel, IAppMessage> {

        public static readonly AppModel Default = new(SlotPageModel.Default);

        public AppModel Update(IAppMessage message) {
            return message switch {
                SlotPageMessageInApp(var msg) => this with { SlotPage = SlotPage.Update(msg) },
                _ => throw new ArgumentOutOfRangeException(nameof(message), message?.ToString())
            };
        }
    }
}
