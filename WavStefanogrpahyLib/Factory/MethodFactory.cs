using WavSteganographyLib.Interface;
using WavSteganographyLib.Methods;
using WavSteganographyLib.Methods.EchoHiding;

namespace WavSteganographyLib.Factory
{
    public static class MethodFactory
    {
        public static IMethod Create(string type)
        {
            return type.ToLower() switch
            {
                "lsb" => new LsbMethod(),
                "echo" => new EchoHidingMethod(EchoHidingVariant.Hamming),
                "echo-naive" => new EchoHidingMethod(EchoHidingVariant.Naive), 
                "echo-window" => new EchoHidingMethod(EchoHidingVariant.Windowed), 
                "echo-repeat" => new EchoHidingMethod(EchoHidingVariant.Repeat), 
                "echo-repeat-spread" => new EchoHidingMethod(EchoHidingVariant.RepeatSpread),
                "echo-hamming" => new EchoHidingMethod(EchoHidingVariant.Hamming),
                _ => throw new Exception($"Неизвестный метод: {type}")
            };
        }
    }
}
