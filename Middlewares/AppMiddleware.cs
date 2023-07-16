using Roulette.Messages.AppMessage;
using Roulette.Messages.SlotPageMessage;
using Roulette.Models;
using TEA;

namespace Roulette.Middlewares {

    public class AppMiddleware : IDispatcher<IAppMessage> {
        readonly ITEA<AppModel, IAppMessage> tea;
        readonly IDispatcher<ISlotPageMessage> slotPageDispatcher;
        readonly Random random;

        public AppMiddleware(ITEA<AppModel, IAppMessage> tea, Random random) {
            this.tea = tea;
            this.slotPageDispatcher = tea.Wrap<ISlotPageMessage, IAppMessage>(msg => new SlotPageMessageInApp(msg));
            this.random = random;
        }

        async ValueTask StartSlot() {
            slotPageDispatcher.Dispatch(new OnStartSlot(random.NextDouble()));
            while (tea.Current.SlotPage.IsRunningSlot) {
                await Task.Delay(TimeSpan.FromSeconds(1.0));
                slotPageDispatcher.Dispatch(Singleton<OnStopSlot>.Instance);
            }
        }

        public void Dispatch(IAppMessage msg) {
            switch (msg) {
                case SlotPageMessageInApp(var msg2) when msg2 is OnClickStart:
                    _ = StartSlot();
                    break;
                default:
                    tea.Dispatch(msg);
                    break;
            }
        }

    }
}
