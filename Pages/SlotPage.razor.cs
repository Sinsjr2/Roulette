using Microsoft.AspNetCore.Components;
using Roulette.Messages.SlotPageMessage;
using Roulette.Models;
using TEA;

namespace Roulette.Pages {

    public partial class SlotPage {

        [ParameterAttribute]
        public IDispatcher<ISlotPageMessage> Dispatcher { get; set; } = new BufferDispatcher<ISlotPageMessage>();

        [ParameterAttribute]
        public SlotPageModel State { get; set; } = SlotPageModel.Default;
    }
}
