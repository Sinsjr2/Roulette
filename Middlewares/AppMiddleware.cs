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

        void StartSlot() {
            slotPageDispatcher.Dispatch(new OnStartSlot(random.NextDouble()));
        }

        public void Dispatch(IAppMessage msg) {
            switch (msg) {
                case SlotPageMessageInApp(var msg2) when msg2 is OnClickStart:
                    StartSlot();
                    break;
                default:
                    tea.Dispatch(msg);
                    break;
            }
        }

    }
}
