using UnityEngine;

namespace OneLastTime.Interaction
{
    // attach to ground colliders (like crates/props) that ryu can stand and jump on,
    // but should not allow wall sliding or wall jumping on their sides
    [DisallowMultipleComponent]
    public class NoWallSlide : MonoBehaviour
    {
    }
}

