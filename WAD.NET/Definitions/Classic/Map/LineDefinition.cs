namespace WAD.NET.Definitions.Classic.Map
{
    public class LineDefinition
    {
        public short SourceVertex { get; set; }
        public short TargetVertex { get; set; }
        public short Flags { get; set; }
        public short Types { get; set; }
        public short Tag { get; set; }
        public short RightSideDefinitionId { get; set; }
        public short LeftSideDefinitionId { get; set; }
    }
}