//  ./GameShared/Strategies/LeftRightRoam.cs
using GameShared.Types.Enemies; // or wherever Enemy base class is


namespace GameShared.Strategies
{
    public class LeftRightRoam
    {
        private readonly int _leftBound;
        private readonly int _rightBound;
        private int _direction = 1; // 1 = right, -1 = left
        private readonly int _speed;

        public LeftRightRoam(int startX, int distance = 200, int speed = 2)
        {
            _leftBound = startX - distance;
            _rightBound = startX + distance;
            _speed = speed;
        }

        public void Update(Enemy enemy)
        {
            int nextX = enemy.X + (_direction * _speed);

            if (nextX > _rightBound)
            {
                nextX = _rightBound;
                _direction = -1;
            }
            else if (nextX < _leftBound)
            {
                nextX = _leftBound;
                _direction = 1;
            }

            enemy.X = nextX;
        }
    }
}
