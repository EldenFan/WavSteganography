using WavSteganographyLib.Interface;
using WavSteganographyLib.Methods;

namespace WavSteganographyLib.Factory
{
    public static class MethodFactory
    {
        public static IMethod Create(string type)
        {
            return type.ToLower() switch
            {
                "lsb" => new LsbMethod(),
                "echo" => new EchoHidingMethod(),
                _ => throw new Exception($"Неизвестный метод: {type}")
            };
        }
    }
}
