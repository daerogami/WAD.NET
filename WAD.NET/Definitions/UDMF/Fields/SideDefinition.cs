using System;
using WAD.NET.Concrete;

namespace WAD.NET.UDMF.Fields
{
    /// <summary>
    /// Side Definition
    /// </summary>
    /// <remarks>
    /// As defined by UDMF 1.1 and zDoom extentions v1.15
    /// <see cref="https://github.com/coelckers/gzdoom/blob/master/specs/udmf.txt"/>
    /// <seealso cref="https://github.com/doomtech/slade/blob/trunk/udmf_zdoom.txt"/>
    /// </remarks>
    struct SideDefinition
    {
        /// <summary>
        /// X Offset. Default = 0.
        /// </summary>
        int offsetx;

        /// <summary>
        /// Y Offset. Default = 0.
        /// </summary>
        int offsety;

        /// <summary>
        /// Upper texture. Default = "-".
        /// </summary>
        string texturetop;

        /// <summary>
        /// Lower texture. Default = "-".
        /// </summary>
        string texturebottom;

        /// <summary>
        /// Middle texture. Default = "-".
        /// </summary>
        string texturemiddle;

        /// <summary>
        /// Sector index. No valid default.
        /// </summary>
        int sector;

        /// <summary>
        /// A comment. Implementors should attach no special semantic meaning to this field.
        /// </summary>
        string comment;

        /// <summary>
        /// // X scale for upper texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scalex_top;

        /// <summary>
        /// y scale for upper texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scaley_top;

        /// <summary>
        /// X scale for mid texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scalex_mid;

        /// <summary>
        /// y scale for mid texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scaley_mid;

        /// <summary>
        /// X scale for lower texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scalex_bottom;

        /// <summary>
        /// y scale for lower texture, Default = 1.0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float scaley_bottom;

        /// <summary>
        /// X offset for upper texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsetx_top;

        /// <summary>
        /// y offset for upper texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsety_top;

        /// <summary>
        /// X offset for mid texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsetx_mid;

        /// <summary>
        /// y offset for mid texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsety_mid;

        /// <summary>
        /// X offset for lower texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsetx_bottom;

        /// <summary>
        /// y offset for lower texture, Default = 0.0.
        /// </summary>
        /// <remarks>
        /// When global texture offsets are used they will be added on top of these values.
        /// </remarks>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        float offsety_bottom;

        /// <summary>
        /// This side's light level. Default is 0.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        int light;

        /// <summary>
        /// true = 'light' is an absolute value. Default is relative to the owning sector's light level.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool lightabsolute;

        /// <summary>
        /// true = This side's relative lighting is used even in foggy sectors. Default is to disable relative lighting in foggy sectors.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool lightfog;

        /// <summary>
        /// Disables use of fake contrast on this sidedef.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool nofakecontrast;

        /// <summary>
        /// Use smooth fake contrast.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool smoothlighting;

        /// <summary>
        /// Side's mid textures are clipped to floor and ceiling.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool clipmidtex;

        /// <summary>
        /// Side's mid textures are wrapped.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool wrapmidtex;

        /// <summary>
        /// Disables decals on the sidedef.
        /// </summary>
        [FieldSpecification(Specification.UDMF_ZDOOM, 1, 15)]
        bool nodecals;
    }
}
