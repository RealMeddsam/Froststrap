namespace Bloxstrap.Enums.FlagPresets
{
    public enum AnimationSmoothing
    {
        [EnumName(FromTranslation = "Common.Default")]
        Default,

        [EnumName(StaticName = "Back")]
        Back,

        [EnumName(StaticName = "Sine")]
        Sine,

        [EnumName(StaticName = "Quad")]
        Quad,

        [EnumName(StaticName = "Cubic")]
        Cubic,

        [EnumName(StaticName = "Quint")]
        Quint,

        [EnumName(StaticName = "Quart")]
        Quart,

        [EnumName(StaticName = "Linear")]
        Linear,

        [EnumName(StaticName = "Bounce")]
        Bounce,

        [EnumName(StaticName = "Elastic")]
        Elastic,

        [EnumName(StaticName = "Constant")]
        Constant,

        [EnumName(StaticName = "Circular")]
        Circular,

        [EnumName(StaticName = "Polynomia")]
        Polynomia,

        [EnumName(StaticName = "Exponential")]
        Exponential,

        [EnumName(StaticName = "Extrapolation")]
        Extrapolation
    }
}