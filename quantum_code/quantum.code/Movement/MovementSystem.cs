using Photon.Deterministic;
using Quantum.Player;

namespace Quantum.Movement
{
    public unsafe class MovementSystem : SystemMainThreadFilter<PlayerSystem.Filter>
    {
        public const int LerpRotationSpeed = 15;
        
        public override void Update(Frame frame, ref PlayerSystem.Filter filter)
        {
            var input = frame.GetPlayerInput(filter.Link->Player);
            var inputVector = new FPVector2((FP)input->X / 10, (FP)input->Y / 10);

            if (inputVector.SqrMagnitude > 1)
                inputVector = inputVector.Normalized;
            
            filter.CharacterController->Move(frame, filter.Entity, inputVector.XOY);
            
            if (input->Jump.WasPressed)
                filter.CharacterController->Jump(frame);

            if (inputVector.SqrMagnitude != default)
                filter.Transform->Rotation = FPQuaternion.Lerp(filter.Transform->Rotation, FPQuaternion.LookRotation(inputVector.XOY), frame.DeltaTime * LerpRotationSpeed);
        }
    }
}
