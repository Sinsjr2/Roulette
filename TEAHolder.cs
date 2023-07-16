namespace TEA.Holder {

    public interface IDispacherHolder<TState, TMessage> {
        IDispatcher<TMessage> Dispatcher { get; set; }
        TState State { get; set; }
    }

    public interface ITEASetupper<TMessage, TState> {
        void SetHolder(IDispacherHolder<TState, TMessage> holder, Action onChangedState);
    }

    public class TEASetupper<TMessage, TState> : ITEASetupper<TMessage, TState>, IRender<TState> {
        IDispacherHolder<TState, TMessage>? holder;
        Action? onChangedState;
        IDispatcher<TMessage>? dispatcher;
        TState? latestState;

        public void Render(TState state) {
            if (holder is null) {
                latestState = state;
                return;
            }
            holder.State = state;
            onChangedState?.Invoke();
        }

        public void Setup(IDispatcher<TMessage> dispatcher) {
            this.dispatcher = dispatcher;
            ApplyDispatcher(dispatcher);
        }

        public void SetHolder(IDispacherHolder<TState, TMessage> holder, Action onChangedState) {
            this.holder = holder;
            this.onChangedState = onChangedState;
            ApplyDispatcher(this.dispatcher);
        }

        void ApplyDispatcher(IDispatcher<TMessage>? dispatcher) {
            if (holder is null || dispatcher is null) {
                return;
            }
            holder.Dispatcher = dispatcher;
            if (latestState is not null) {
                holder.State = latestState;
            }
            latestState = default;
            onChangedState?.Invoke();
        }
    }

}
