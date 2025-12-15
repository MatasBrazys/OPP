namespace GameShared.Types.Tasks
{
    /// <summary>
    /// A task that requires planting and harvesting a certain number of plants
    /// </summary>
    public class PlantTask : ITask
    {
        public int Id { get; private set; }
        public string Description { get; private set; }
        public bool IsCompleted { get; private set; }

        private int _requiredCount;
        private int _currentCount;
        private string _plantType;

        public PlantTask(int id, int requiredCount, string plantType = "Plant")
        {
            Id = id;
            _requiredCount = requiredCount;
            _plantType = plantType;
            _currentCount = 0;
            IsCompleted = false;
            Description = $"Plant and harvest {_requiredCount} {_plantType}";
        }

        public int GetProgress()
        {
            return _currentCount;
        }

        public int GetRequired()
        {
            return _requiredCount;
        }

        /// <summary>
        /// Called when a plant seed is planted
        /// </summary>
        public void OnPlantSeed()
        {
            // Increment progress when plant is seeded
            if (!IsCompleted && _currentCount < _requiredCount)
            {
                // Note: Progress increments on harvest, not on planting
            }
        }

        /// <summary>
        /// Called when a plant is harvested
        /// </summary>
        public void OnHarvestPlant()
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
            // No continuous updates needed for plant tasks
        }

        private void DisplayProgress()
        {
            var progressBar = GenerateProgressBar(_currentCount, _requiredCount);
            Console.WriteLine($"\n[PLANT] ? Harvested plant! Progress: {_currentCount}/{_requiredCount}\n");
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
