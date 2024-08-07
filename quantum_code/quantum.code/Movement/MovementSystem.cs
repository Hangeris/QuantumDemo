using Photon.Deterministic;

namespace Quantum
{
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public CharacterController3D* CharacterController;
            public Transform3D* Transform;
        }
        
        public override void Update(Frame frame, ref Filter filter)
        {
            var moveDirection = FPVector3.Forward;
            filter.CharacterController->Move(frame, filter.Entity, moveDirection);
        }
    }
}
