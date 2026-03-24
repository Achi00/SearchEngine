namespace Search.Domain.Entity.TextExtraction
{
    // region describes where texts are located on image
    public class TextRegion
    {
        public string Text { get; set; } = string.Empty;
        // quad box - 4 points (x1,y1,x2,y2,x3,y3,x4,y4) normalized 0-1
        public float[] QuadBox { get; set; } = new float[8];
    }
}
