namespace GameShared.Messages
{
    /// <summary>
    /// Message sent to client when a task is completed
    /// </summary>
    public class TaskCompletedMessage : GameMessage
    {
        public int TaskId { get; set; }
        public string Description { get; set; } = string.Empty;

        public TaskCompletedMessage()
        {
            Type = "task_completed";
        }
    }
}
