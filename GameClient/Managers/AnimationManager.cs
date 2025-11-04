// ./GameClient/Managers/AnimationManager.cs
using System.Collections.Generic;
using System.Drawing;
using GameClient.Rendering;
using GameShared.Messages;

namespace GameClient.Managers
{
    public class AnimationManager
    {
        private readonly List<SlashEffect> _slashes = new();
        private readonly List<MageFireballEffect> _fireballs = new();
        private readonly List<ArrowEffect> _arrows = new();

        public void HandleAttackAnimation(AttackAnimationMessage anim)
        {
            // AnimX/AnimY are pixel coords now (per our refactor).
            float animX = anim.AnimX;
            float animY = anim.AnimY;
            float rotation = 0f;
            float.TryParse(anim.Direction, out rotation);
            float radius = anim.Radius;

            switch (anim.AttackType)
            {
                case "slash":
                    lock (_slashes) { _slashes.Add(new SlashEffect(animX, animY, radius, rotation)); }
                    break;
                case "fireball":
                    lock (_fireballs) { _fireballs.Add(new MageFireballEffect(animX, animY)); }
                    break;
                case "arrow":
                    // arrows require start pos; the server included PlayerId - the client will create arrow with player's renderer
                    // The GameClientForm will translate PlayerId->pos and call AddArrow instead (so this method accepts only AnimX for target).
                    // For safety leave a placeholder; actual arrow creation is done by form after mapping player->pos
                    break;
            }
        }

        public void AddArrow(float startX, float startY, float targetX, float targetY, float rotation)
        {
            lock (_arrows) { _arrows.Add(new ArrowEffect(startX, startY, targetX, targetY, rotation)); }
        }

        public void DrawAll(Graphics g)
        {
            lock (_slashes)
            {
                for (int i = _slashes.Count - 1; i >= 0; i--)
                {
                    _slashes[i].Draw(g);
                    if (_slashes[i].IsFinished) _slashes.RemoveAt(i);
                }
            }

            lock (_fireballs)
            {
                for (int i = _fireballs.Count - 1; i >= 0; i--)
                {
                    _fireballs[i].Draw(g);
                    if (_fireballs[i].IsFinished) _fireballs.RemoveAt(i);
                }
            }

            lock (_arrows)
            {
                for (int i = _arrows.Count - 1; i >= 0; i--)
                {
                    _arrows[i].Draw(g);
                    if (_arrows[i].IsFinished) _arrows.RemoveAt(i);
                }
            }
        }
    }
}
