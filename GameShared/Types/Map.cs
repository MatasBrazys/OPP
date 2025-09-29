using System;

namespace GameShared.Types
{
    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Map()
        {
            Width = 40;
            Height = 40;
        }

        // Add map logic here as needed
    }
}