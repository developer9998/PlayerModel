using PlayerModel.Behaviours;
using UnityEngine;

namespace PlayerModel.Models
{
    public interface IModel
    {
        string FilePath { get; }
        ModelDescriptor Descriptor { get; }
        GameObject Template { get; }
    }
}
