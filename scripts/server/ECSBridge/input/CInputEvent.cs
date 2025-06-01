using System;
using Friflo.Engine.ECS;
using Godot;

namespace game.scripts.server.ECSBridge.input;

public struct CInputEvent: IComponent, IEquatable<CInputEvent> {
    public bool MoveForward;
    public bool MoveBackward;
    public bool MoveLeft;
    public bool MoveRight;
    public bool Jump;
    public bool Crouch;
    public Vector2 ForwardVector;
    public bool Digging;
    public bool Placing;
    public Vector2 MouseClickPosition;

    public static bool operator ==(CInputEvent left, CInputEvent right)
    {
        return left.MoveForward == right.MoveForward
            && left.MoveBackward == right.MoveBackward
            && left.MoveLeft == right.MoveLeft
            && left.MoveRight == right.MoveRight
            && left.Jump == right.Jump
            && left.Crouch == right.Crouch
            && left.ForwardVector == right.ForwardVector
            && left.Digging == right.Digging
            && left.Placing == right.Placing
            && left.MouseClickPosition == right.MouseClickPosition;
    }

    public static bool operator !=(CInputEvent left, CInputEvent right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        return obj is CInputEvent other && this == other;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + MoveForward.GetHashCode();
            hash = hash * 23 + MoveBackward.GetHashCode();
            hash = hash * 23 + MoveLeft.GetHashCode();
            hash = hash * 23 + MoveRight.GetHashCode();
            hash = hash * 23 + Jump.GetHashCode();
            hash = hash * 23 + Crouch.GetHashCode();
            hash = hash * 23 + ForwardVector.GetHashCode();
            hash = hash * 23 + Digging.GetHashCode();
            hash = hash * 23 + Placing.GetHashCode();
            hash = hash * 23 + MouseClickPosition.GetHashCode();
            return hash;
        }
    }

    public bool Equals(CInputEvent other) {
        return MoveForward == other.MoveForward && MoveBackward == other.MoveBackward && MoveLeft == other.MoveLeft && MoveRight == other.MoveRight && Jump == other.Jump && Crouch == other.Crouch && ForwardVector.Equals(other.ForwardVector) && Digging == other.Digging && Placing == other.Placing && MouseClickPosition.Equals(other.MouseClickPosition);
    }
}
