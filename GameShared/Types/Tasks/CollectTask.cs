namespace GameShared.Types.Tasks
{
    /// <summary>
    /// A task that requires collecting or defeating a certain number of enemies
    /// </summary>
    public class CollectTask : ITask
    {
        public int Id { get; private set; }
        public string Description { get; private set; }
        public bool IsCompleted { get; private set; }

        private int _requiredCount;
        private int _currentCount;
        private string _itemType;

        public CollectTask(int id, int requiredCount, string itemType = "Enemy")
        {
            Id = id;
            _requiredCount = requiredCount;
            _itemType = itemType;
            _currentCount = 0;
            IsCompleted = false;
            Description = $"Defeat {_requiredCount} {_itemType}";
        }

        public int GetProgress()
        {
            return _currentCount;
        }

        public int GetRequired()
        {
            return _requiredCount;
        }

        public void OnCollect()
        {
            if (!IsCompleted && _currentCount < _requiredCount)
            {
                _currentCount++;
                DisplayProgress();

                if (_currentCount >= _requiredCount)
                {
                    IsCompleted = true;
                }
            }
        }

        public void OnUpdate()
        {
            // No continuous updates needed for collect tasks
        }

        private void DisplayProgress()
        {
            var progressBar = GenerateProgressBar(_currentCount, _requiredCount);
            Console.WriteLine($"\n[COLLECT] ? Collected item! Progress: {_currentCount}/{_requiredCount}\n");
        }

        private static string GenerateProgressBar(int current, int total)
        {
            const int barLength = 15;
            int filledLength = (int)((double)current / total * barLength);
            string bar = new string('?', filledLength) + new string('?', barLength - filledLength);
            return $"[{bar}]";
        }

        public override string ToString()
        {
            return $"Task[ID:{Id}, {Description}, Progress: {_currentCount}/{_requiredCount}, Completed: {IsCompleted}]";
        }
    }
}
