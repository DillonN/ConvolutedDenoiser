namespace ConvolutedDenoiser.Image.Enums 
{
    /// <summary>
    /// Represents format of image read
    /// <para>Values are the two ASCII characters that represent the format, with the first shifted left</para>
    /// </summary>
    public enum ImageFileType : ushort
    {
        Pgm = (80 << 8) + 53  // P5
    }
}
