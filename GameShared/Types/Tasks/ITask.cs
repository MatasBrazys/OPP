namespace GameShared.Types.Tasks
{
    public interface ITask
    {
        int Id { get; }
        string Description { get; }
        bool IsCompleted { get; }
        void OnUpdate();
    }
}
