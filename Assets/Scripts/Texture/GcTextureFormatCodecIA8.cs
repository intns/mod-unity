using System;

namespace LibGC.Texture
{
    /// <summary>IA4 (4 bits intensity / 4 bits alpha) pixel format.</summary>
    class GcTextureFormatCodecIA8 : GcTextureFormatCodec
    {
        public override int TileWidth
        {
            get { return 4; }
        }

        public override int TileHeight
        {
            get { return 4; }
        }

        public override int BitsPerPixel
        {
            get { return 16; }
        }

        public override int PaletteCount
        {
            get { return 0; }
        }

        public override bool IsSupported
        {
            get { return true; }
        }

        protected override void DecodeTile(
            byte[] dst,
            int dstPos,
            int stride,
            byte[] src,
            int srcPos
        )
        {
            int srcCurrentPos = srcPos,
                dstCurrentPos = dstPos;
            for (int ty = 0; ty < TileHeight; ty++, dstCurrentPos += stride - TileWidth * 4)
            {
                for (int tx = 0; tx < TileWidth; tx++, dstCurrentPos += 4)
                {
                    ushort ia8 = BitConverter.ToUInt16(src, srcCurrentPos);
                    ColorConversion.IA8ToColor(ia8).Write(dst, dstCurrentPos);
                    srcCurrentPos += 2;
                }
            }
        }

        protected override bool EncodingWantsGrayscale
        {
            get { throw new NotImplementedException(); }
        }

        protected override bool EncodingWantsDithering
        {
            get { throw new NotImplementedException(); }
        }

        protected override ColorRGBA TrimColor(ColorRGBA color)
        {
            throw new NotImplementedException();
        }

        protected override void EncodeTile(
            byte[] src,
            int srcPos,
            int stride,
            byte[] dst,
            int dstPos
        )
        {
            throw new NotImplementedException();
        }
    }
}
