using System.Drawing;
using UELib.JsonDecompiler.Core;

namespace UELib.JsonDecompiler.Engine
{
    [UnrealRegisterClass]
    public class UPalette : UObject, IUnrealViewable
    {
        private Color[] _ColorPalette;

        public UPalette()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            int count = _Buffer.ReadIndex();
            if( count > 0 )
            {
                _ColorPalette = new Color[count];
                for( int i = 0; i < count; ++ i )
                {
                    _ColorPalette[i] = Color.FromArgb( _Buffer.ReadInt32() );
                }
            }
        }
    }
}