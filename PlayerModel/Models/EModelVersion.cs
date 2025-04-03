namespace PlayerModel.Models
{
    public enum EModelVersion
    {
        /// <summary>
        /// PlayerModel is made with pre-release version of editor using Unity 2019
        /// </summary>
        Legacy1,
        /// <summary>
        /// PlayerModel is made with public release version of editor using Unity 2019
        /// </summary>
        Legacy2,
        /// <summary>
        /// PlayerModel is made with 1.2.0 version of editor using Unity 2019
        /// </summary>
        Legacy3,
        /// <summary>
        /// PlayerModel is made with the latest version of editor using Unity 2022
        /// </summary>
        Current
    }
}