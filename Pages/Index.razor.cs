using Microsoft.AspNetCore.Components;
using Roulette.Messages.AppMessage;
using Roulette.Models;
using TEA;
using TEA.Holder;

namespace Roulette.Pages {

    public partial class Index : ComponentBase, IDispacherHolder<AppModel, IAppMessage> {

        [ParameterAttribute]
        public IDispatcher<IAppMessage> Dispatcher { get; set; } = new BufferDispatcher<IAppMessage>();

        [ParameterAttribute]
        public AppModel State { get; set; } = AppModel.Default;

        [Inject]
        ITEASetupper<IAppMessage, AppModel>? setupper { get; set; }

        protected override void OnInitialized() {
            setupper?.SetHolder(this, this.StateHasChanged);
        }
    }
}
