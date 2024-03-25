namespace UELib.Core
{
    /// <summary>
    ///     Implements UPrimitive/Engine.Primitive
    /// </summary>
    [UnrealRegisterClass]
    public class UPrimitive : UObject
    {
        public UBox BoundingBox;
        public USphere BoundingSphere;
        
        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.ReadStruct(out BoundingBox);
            Record(nameof(BoundingBox), BoundingBox);
            
            _Buffer.ReadStruct(out BoundingSphere);
            Record(nameof(BoundingSphere), BoundingSphere);
        }
    }
}
