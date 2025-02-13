namespace EfCorePlus.Utils
{
    public class ObjectDisposeAcitonWrapper<T> : IDisposable
    {
        private Action? _action;

        public ObjectDisposeAcitonWrapper(T obj, Action? action)
        {
            Object = obj;
            _action = action;
        }

        public T Object { get; private set; }

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _action, null);
            action?.Invoke();
        }
    }
}
