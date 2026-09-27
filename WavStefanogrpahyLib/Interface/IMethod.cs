using WavSteganographyLib.Base;
using WavSteganographyLib.Form;

namespace WavSteganographyLib.Interface
{
    public interface IMethod
    {
        MethodsType Type { get; }

        short[] Embed(short[] samples, StenagraphyData data);

        StenagraphyData Extract(short[] samples);
    }
}
