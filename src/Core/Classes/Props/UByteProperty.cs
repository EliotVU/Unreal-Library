using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Byte Property
    /// </summary>
    [UnrealRegisterClass]
    public class UByteProperty : UProperty
    {
        #region Serialized Members
        public UEnum EnumObject;
        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UByteProperty class.
        /// </summary>
        public UByteProperty()
        {
            Type = PropertyType.ByteProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            int enumIndex = _Buffer.ReadObjectIndex();
            EnumObject = (UEnum)GetIndexObject( enumIndex );
        }

        /// <inheritdoc/>
        public override void InitializeImports()
        {
            base.InitializeImports();
            ImportObject();
        }

        // Import the enum of e.g. Actor.Role and LevelInfo.NetMode.
        private void ImportObject()
        {
            // Already imported...
            if( EnumObject != null )
            {
                return;
            }

            var pkg = LoadImportPackage();
            if( pkg != null )
            {
                if( pkg.Objects == null )
                {
                    pkg.RegisterClass( "ByteProperty", typeof(UByteProperty) );
                    pkg.RegisterClass( "Enum", typeof(UEnum) );
                    pkg.InitializeExportObjects();
                }
                var b = (UByteProperty)pkg.FindObject( Name, typeof(UByteProperty) );
                if( b != null )
                {
                    EnumObject = b.EnumObject;
                }
            }
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return EnumObject != null ? EnumObject.GetOuterGroup() : "byte";
        }
    }
}