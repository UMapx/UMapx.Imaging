using System.Drawing;
using System.Runtime.Versioning;
using UMapx.Core;
using UMapx.Imaging;
using Xunit;
using static UMapx.Tests.NumericAssert;

namespace UMapx.Tests;

[Trait("Category","Geometry")]
[SupportedOSPlatform("windows")]
public class GeometryAndRenderingAuditTests
{
    [Theory] [InlineData(0,0,10,20,5,10,20,30)] [InlineData(-10,-10,7,8,3,4,10,10)] [InlineData(0,0,50000,50000,0,0,50000,50000)]
    [InlineData(0,0,46340,46340,0,0,46340,46340)]
    [InlineData(0,0,46341,46341,0,0,46341,46341)]
    [InlineData(0,0,65536,32768,0,0,65536,32768)]
    [InlineData(0,0,65536,65536,0,0,65536,65536)]
    [InlineData(0,0,70000,70000,10000,10000,70000,70000)]
    public void RectangleOverlapUsesGeometricAreaWithoutIntegerOverflow(int ax,int ay,int aw,int ah,int bx,int by,int bw,int bh)
    {
        var a=new Rectangle(ax,ay,aw,ah);var b=new Rectangle(bx,by,bw,bh);
        double area=(double)Math.Max(0,Math.Min(a.Right,b.Right)-Math.Max(a.Left,b.Left))*Math.Max(0,Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Top,b.Top));
        Close(area/((double)aw*ah+(double)bw*bh-area),a.IoU(b));
        if(a==b)Assert.Equal(1f,a.IoU(b));
    }
    [Fact]
    public void RectangleAndPointTranslationsAndBoundingBoxesMatchCoordinates()
    {
        var a=new Rectangle(3,-7,20,14);var delta=new Point(-5,9);Assert.Equal(a,Rectangles.Sub(Rectangles.Add(a,delta),delta));
        var points=new[]{new Point(-7,4),new Point(3,-2),new Point(5,10)};var moved=UMapx.Imaging.Points.Add(points,delta);Assert.Equal(points,UMapx.Imaging.Points.Sub(moved,delta));
        Assert.Equal(new Rectangle(-7,-2,12,12),UMapx.Imaging.Points.GetRectangle(points));Assert.Equal(new Point(0,4),UMapx.Imaging.Points.GetMeanPoint(points));
        foreach(float angle in new[]{0d,90d,180d,-90d}){var p=UMapx.Imaging.Points.Rotate(new Point(13,7),new Point(3,7),angle);Close(3+10*Math.Cos(angle*Math.PI/180),p.X,1,0);Close(7+10*Math.Sin(angle*Math.PI/180),p.Y,1,0);}
        Assert.Equal(new Rectangle(5,-7,5,10),a.Clamp(new Rectangle(5,-10,5,13)));
        var ranges=new RangeInt(-3,5);var rangef=new RangeFloat(-.5f,1.5f);foreach(int x in Enumerable.Range(-10,21))Assert.Equal(x>=-3&&x<=5,ranges.IsOnRange(x));foreach(float x in new[]{-1f,-.5f,0f,1.5f,2f})Assert.Equal(x>=-.5f&&x<=1.5f,rangef.IsOnRange(x));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void HomographiesMapAllFourCornersAndInvertInteriorPoints(bool perspective)
    {
        var type=typeof(BitmapMatrix).Assembly.GetType("UMapx.Imaging.Float3x3")!;var source=new Rectangle(3,5,20,10);
        var target=perspective?new[]{new PointFloat(2,3),new PointFloat(28,7),new PointFloat(20,21),new PointFloat(-2,14)}:new[]{new PointFloat(2,3),new PointFloat(32,3),new PointFloat(32,18),new PointFloat(2,18)};
        // Match the public PerspectiveWarp order: top-left, top-right, bottom-left, bottom-right.
        var matrix=type.GetMethod("Perspective")!.Invoke(null,new object[]{source,target[0],target[1],target[3],target[2]})!;var inverse=type.GetMethod("Invert")!.Invoke(matrix,null)!;
        var corners=new[]{new PointFloat(3,5),new PointFloat(23,5),new PointFloat(23,15),new PointFloat(3,15)};PointFloat Apply(object m,PointFloat p)=>(PointFloat)type.GetMethod("TransformPoint")!.Invoke(m,new object[]{p})!;
        for(int i=0;i<4;i++){var actual=Apply(matrix,corners[i]);Close(target[i].X,actual.X,1e-4);Close(target[i].Y,actual.Y,1e-4);}
        foreach(var p in new[]{new PointFloat(7,8),new PointFloat(16,12)}){var restored=Apply(inverse,Apply(matrix,p));Close(p.X,restored.X,1e-4);Close(p.Y,restored.Y,1e-4);}
    }
    [Theory] [InlineData("Shift")] [InlineData("Crop")] [InlineData("Flip")] [InlineData("Rotate")] [InlineData("Merge")]
    public void RectangularDepthTransformsPreserveCoordinateMeaning(string operation)
    {
        var source=new ushort[5,7];for(int y=0;y<5;y++)for(int x=0;x<7;x++)source[y,x]=(ushort)(100+y*100+x*7);
        if(operation=="Shift"){var r=DepthTransform.Shift(source,2,-1);for(int y=0;y<5;y++)for(int x=0;x<7;x++)Assert.Equal(source[(y+1)%5,(x+5)%7],r[y,x]);}
        else if(operation=="Crop"){var r=DepthTransform.Crop(source,new Rectangle(2,1,3,2));Assert.Equal(2,r.GetLength(0));Assert.Equal(3,r.GetLength(1));for(int y=0;y<2;y++)for(int x=0;x<3;x++)Assert.Equal(source[y+1,x+2],r[y,x]);}
        else if(operation=="Flip"){foreach(var d in Enum.GetValues<Direction>()){var r=DepthTransform.Flip(source,d);for(int y=0;y<5;y++)for(int x=0;x<7;x++)Assert.Equal(source[d==Direction.Horizontal?y:4-y,d==Direction.Vertical?x:6-x],r[y,x]);}}
        else if(operation=="Rotate"){var r=DepthTransform.Rotate(source,RotationMode.R90);Assert.Equal(7,r.GetLength(0));Assert.Equal(5,r.GetLength(1));for(int y=0;y<5;y++)for(int x=0;x<7;x++)Assert.Equal(source[y,x],r[x,4-y]);}
        else{ushort[,] block={{1000,1000},{1000,1000}};var r=(ushort[,])source.Clone();DepthTransform.Merge(r,block,new Rectangle(2,1,2,2));for(int y=0;y<5;y++)for(int x=0;x<7;x++)Assert.Equal(y>=1&&y<3&&x>=2&&x<4?(ushort)1000:source[y,x],r[y,x]);}
    }
}
