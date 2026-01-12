using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.HUD.DuelField
{
    public static class GameLifecycle
    {
        public static GameState State { get; private set; } = GameState.Boot;
        public static bool IsReady => State == GameState.Ready || State == GameState.Running;

        public static void Set(GameState state)
        {
            State = state;
        }
    }

}
