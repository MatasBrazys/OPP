using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Mediator
{
    /// <summary>
    /// Implemented by any component that must subscribe to the mediator
    /// before interacting with it. Mediator will call OnMediatorAttached
    /// when the participant is registered and OnMediatorDetached when removed.
    /// </summary>
    public interface IMediatorParticipant
    {
        void OnMediatorAttached(IGameMediator mediator);
        void OnMediatorDetached();
    }
}
