using UnityEngine;

namespace PlayerModel.Models
{
    public enum EIKType
    {
        [Tooltip("Traditional IK solver used by Gorilla Tag without using poles")]
        GorillaIK,
        [Tooltip("Popular IK solver using poles that are placed behind the player, prone to detachment issues")]
        FastIK
    }
}
