namespace UELib
{
    public struct UGenerationTableItem : IUnrealSerializableClass
    {
        public int ExportsCount;
        public int NamesCount;
        public int NetObjectsCount;

        private const int VNetObjectsCount = 322;

        public void Serialize( IUnrealStream stream )
        {
            stream.Write( ExportsCount );
            stream.Write( NamesCount );
            if( stream.Version >= VNetObjectsCount )
            {
                stream.Write( NetObjectsCount );
            }
        }

        public void Deserialize( IUnrealStream stream )
        {
#if APB
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.APB && stream.Package.LicenseeVersion >= 32 )
            {
                stream.Skip( 16 );
            }
#endif
            ExportsCount = stream.ReadInt32();
            NamesCount = stream.ReadInt32();

#if UE4
            if( stream.Package.UE4Version >= 186 )
            {
                return;
            }
#endif

            if( stream.Version >= VNetObjectsCount )
            {
                NetObjectsCount = stream.ReadInt32();
            }
        }
    }
}