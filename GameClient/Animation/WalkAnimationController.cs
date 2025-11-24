using System;
using System.Drawing;
using GameClient.Rendering;

namespace GameClient.Animation
{
    /// <summary>
    /// Controls walking animations with 3-frame cycles per direction
    /// Supports 8 directions (4 cardinal + 4 diagonal)
    /// </summary>
    public class WalkAnimationController
    {
        private const float AnimationSpeed = 0.15f; // Time per frame in seconds
        private DateTime _lastFrameChange = DateTime.UtcNow;
        private int _currentFrame = 0; // 0, 1, or 2
        private Direction _currentDirection = Direction.Down;
        private bool _isMoving = false;

        public enum Direction
        {
            Down,
            Up,
            Left,
            Right,
            DownLeft,
            DownRight,
            UpLeft,
            UpRight
        }

        /// <summary>
        /// Updates animation state based on movement
        /// </summary>
        public void Update(float dx, float dy)
        {
            bool wasMoving = _isMoving;
            _isMoving = dx != 0 || dy != 0;

            if (_isMoving)
            {
                // Determine direction from movement vector
                _currentDirection = GetDirectionFromMovement(dx, dy);

                // Advance animation frame
                var elapsed = (DateTime.UtcNow - _lastFrameChange).TotalSeconds;
                if (elapsed >= AnimationSpeed)
                {
                    _currentFrame = (_currentFrame + 1) % 3; // 0 -> 1 -> 2 -> 0
                    _lastFrameChange = DateTime.UtcNow;
                }
            }
            else if (wasMoving)
            {
                // Just stopped moving - reset to idle frame
                _currentFrame = 0;
                _lastFrameChange = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets the sprite name for current animation state
        /// Format: "{role}_{direction}_{frame}"
        /// Example: "Mage_Down_1", "Hunter_UpLeft_2"
        /// </summary>
        public string GetCurrentSpriteName(string roleType)
        {
            return $"{roleType}_{_currentDirection}_{_currentFrame + 1}";
        }

        /// <summary>
        /// Gets direction from movement vector, including diagonals
        /// </summary>
        private Direction GetDirectionFromMovement(float dx, float dy)
        {
            // Normalize to determine primary direction
            float threshold = 0.3f; // Threshold for diagonal detection

            // Pure vertical
            if (Math.Abs(dx) < threshold)
            {
                return dy > 0 ? Direction.Down : Direction.Up;
            }
            // Pure horizontal
            else if (Math.Abs(dy) < threshold)
            {
                return dx > 0 ? Direction.Right : Direction.Left;
            }
            // Diagonals
            else
            {
                if (dx > 0 && dy > 0) return Direction.DownRight;
                if (dx > 0 && dy < 0) return Direction.UpRight;
                if (dx < 0 && dy > 0) return Direction.DownLeft;
                if (dx < 0 && dy < 0) return Direction.UpLeft;
            }

            return _currentDirection; // Fallback
        }

        /// <summary>
        /// Resets animation to idle state
        /// </summary>
        public void Reset()
        {
            _currentFrame = 0;
            _isMoving = false;
            _lastFrameChange = DateTime.UtcNow;
        }

        /// <summary>
        /// For diagonal movements, determines which cardinal sprite to use
        /// This is a fallback if you only have 4 directional sprites
        /// </summary>
        public Direction GetCardinalFallback(Direction diagonal)
        {
            return diagonal switch
            {
                Direction.DownLeft => Direction.Left,
                Direction.DownRight => Direction.Right,
                Direction.UpLeft => Direction.Left,
                Direction.UpRight => Direction.Right,
                _ => diagonal
            };
        }
    }

    /// <summary>
    /// Helper for loading and organizing walk sprites
    /// </summary>
    public static class WalkSpriteLoader
    {
        /// <summary>
        /// Loads all walk sprites for a role
        /// Expected file naming: "role_direction_frame.png"
        /// Example: "mage_down_1.png", "hunter_upright_2.png"
        /// </summary>
        public static void LoadWalkSprites(string roleType, string assetPath)
        {
            var directions = new[]
            {
                "Down", "Up", "Left", "Right",
                "DownLeft", "DownRight", "UpLeft", "UpRight"
            };

            foreach (var direction in directions)
            {
                for (int frame = 1; frame <= 3; frame++)
                {
                    string spriteName = $"{roleType}_{direction}_{frame}";
                    string filePath = $"{assetPath}/{roleType.ToLower()}_{direction.ToLower()}_{frame}.png";

                    try
                    {
                        if (File.Exists(filePath))
                        {
                            var image = Image.FromFile(filePath);
                            SpriteRegistry.Register(spriteName, image);
                            Console.WriteLine($"✅ Loaded: {spriteName}");
                        }
                        else
                        {
                            // Fallback strategy for missing diagonals
                            LoadDiagonalFallback(roleType, direction, frame, assetPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to load {spriteName}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Creates diagonal sprites by reusing cardinal direction sprites
        /// This handles the case where you only have 4-directional sprites
        /// </summary>
        private static void LoadDiagonalFallback(string roleType, string direction, int frame, string assetPath)
        {
            // Map diagonals to their closest cardinal direction
            var fallbackMap = new Dictionary<string, string>
            {
                { "DownLeft", "Left" },
                { "DownRight", "Right" },
                { "UpLeft", "Left" },
                { "UpRight", "Right" }
            };

            if (fallbackMap.TryGetValue(direction, out string cardinalDir))
            {
                string fallbackFile = $"{assetPath}/{roleType.ToLower()}_{cardinalDir.ToLower()}_{frame}.png";
                
                if (File.Exists(fallbackFile))
                {
                    var image = Image.FromFile(fallbackFile);
                    string spriteName = $"{roleType}_{direction}_{frame}";
                    SpriteRegistry.Register(spriteName, image);
                    Console.WriteLine($"⚠️ Using fallback for {spriteName} -> {cardinalDir}");
                }
            }
        }

        /// <summary>
        /// Batch load all roles
        /// </summary>
        public static void LoadAllRoleAnimations(string assetPath = "../assets/animations")
        {
            var roles = new[] { "Mage", "Hunter", "Defender" };
            
            foreach (var role in roles)
            {
                LoadWalkSprites(role, assetPath);
            }
        }
    }
}