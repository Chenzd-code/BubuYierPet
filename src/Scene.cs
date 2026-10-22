using System;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace DuoPet {
public sealed class InputState {
 public bool LeftKey,RightKey,MouseLeft,MouseRight,Paused;public double MouseX,MouseY,Time;public int KeyCount,MoveCount,LeftClicks,RightClicks;public double LeftPulse,RightPulse,MouseLPulse,MouseRPulse,WheelUntil;public bool[] Keys=new bool[256];
 public bool[] Physical=new bool[512];public double[] KeyPulse=new double[512];public int[] KeyOrder=new int[512];public int LastPhysical=0;public double HitStarted=-10,BodyX=540;
 public bool L{get{return !Paused&&(LeftKey||Time<LeftPulse);}}public bool R{get{return !Paused&&(RightKey||Time<RightPulse);}}public bool ML{get{return !Paused&&(MouseLeft||Time<MouseLPulse);}}public bool MR{get{return !Paused&&(MouseRight||Time<MouseRPulse);}}
 public bool Active(int code){return !Paused&&(Physical[code]||Time<KeyPulse[code]);}
 public KeyCap ActiveTarget{get{KeyCap best=null;int order=-1;foreach(var k in KeyboardLayout.All)if(Active(k.Code)&&KeyOrder[k.Code]>order){best=k;order=KeyOrder[k.Code];}return best;}}
 public void Clear(){Array.Clear(Keys,0,Keys.Length);Array.Clear(Physical,0,Physical.Length);Array.Clear(KeyPulse,0,KeyPulse.Length);Array.Clear(KeyOrder,0,KeyOrder.Length);LeftKey=RightKey=MouseLeft=MouseRight=false;LeftPulse=RightPulse=MouseLPulse=MouseRPulse=WheelUntil=0;LastPhysical=0;HitStarted=-10;}
}
public class Scene:FrameworkElement {
 public const double ViewWidth=500,ViewHeight=350,CharacterWidth=220,SourceWidth=230;
 public InputState State;BitmapSource yier,bubu,paddle;public bool Hint=true;
 public Scene(InputState s){State=s;yier=Load("yier.png");bubu=Load("bubu.png");paddle=Load("paddle.png");RenderOptions.SetBitmapScalingMode(this,BitmapScalingMode.HighQuality);}
 static BitmapSource Load(string f){var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.UriSource=new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",f));b.EndInit();b.Freeze();return b;}
 static Brush B(string c){var b=(SolidColorBrush)new BrushConverter().ConvertFromString(c);b.Freeze();return b;}
 static Pen P(string c,double n){var p=new Pen(B(c),n){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round};p.Freeze();return p;}
 static void R(DrawingContext d,string fill,string stroke,double sw,double x,double y,double w,double h,double radius){if(surface){double rr=Math.Min(radius,Math.Min(w,h)/2);var pp=new Point[24];for(int j=0;j<4;j++){double cx=j==0||j==3?x+rr:x+w-rr,cy=j<2?y+rr:y+h-rr;for(int n=0;n<6;n++){double a=(180+j*90+n*18)*Math.PI/180;pp[j*6+n]=new Point(cx+rr*Math.Cos(a),cy+rr*Math.Sin(a));}}Polygon(d,B(fill),stroke==null?null:P(stroke,sw*.7),pp);return;}d.DrawRoundedRectangle(B(fill),stroke==null?null:P(stroke,sw),new Rect(x,y,w,h),radius,radius);}
 static void E(DrawingContext d,string fill,string stroke,double sw,double x,double y,double rx,double ry){if(surface){var pts=new Point[32];for(int i=0;i<32;i++){double a=i*Math.PI/16;pts[i]=new Point(x+rx*Math.Cos(a),y+ry*Math.Sin(a));}Polygon(d,B(fill),stroke==null?null:P(stroke,sw*.7),pts);return;}d.DrawEllipse(B(fill),stroke==null?null:P(stroke,sw),new Point(x,y),rx,ry);}
 static void Line(DrawingContext d,string c,double width,Point a,Point b){d.DrawLine(P(c,surface?width*.7:width),surface?SurfacePoint(a):a,surface?SurfacePoint(b):b);}
 static void Text(DrawingContext d,string text,double size,string color,Rect box,bool centered=true){if(surface){var o=SurfacePoint(box.TopLeft);var ex=SurfacePoint(new Point(box.X+1,box.Y))-o;var ey=SurfacePoint(new Point(box.X,box.Y+1))-o;d.PushTransform(new MatrixTransform(new Matrix(ex.X,ex.Y,ey.X,ey.Y,o.X,o.Y)));box=new Rect(0,0,box.Width,box.Height);}
var f=new FormattedText(text,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),size,B(color));f.TextAlignment=centered?TextAlignment.Center:TextAlignment.Left;f.LineHeight=size+1;d.DrawText(f,new Point(centered?box.X+box.Width/2:box.X,box.Y+(box.Height-f.Height)/2));if(surface)d.Pop();}
 static void Arm(DrawingContext d,Point shoulder,Point elbow,Point grip,string fill,double width){
 Vector v=grip-shoulder;double length=v.Length;if(length<1)return;Vector u=v/length,n=new Vector(-u.Y,u.X);double root=width*.62,tip=width*.38,bend=Math.Min(7,length*.12);Point c1=shoulder+v*.30+n*bend,c2=shoulder+v*.72+n*bend;
 var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(shoulder+n*root,true,true);c.BezierTo(c1+n*root,c2+n*tip,grip+n*tip,true,false);c.BezierTo(grip+n*tip+u*tip,grip-n*tip+u*tip,grip-n*tip,true,false);c.BezierTo(c2-n*tip,c1-n*root,shoulder-n*root,true,false);}g.Freeze();d.DrawGeometry(B(fill),P("#654137",2.2),g);
 }
 static void Paw(DrawingContext d,Point p,string fill,bool white){
 d.PushTransform(new RotateTransform(-12,p.X,p.Y));E(d,fill,"#654137",1.8,p.X,p.Y,10,7.5);
 var crease=white?"#CDAF9F":"#A97C65";Line(d,crease,.9,new Point(p.X+2,p.Y+2),new Point(p.X+2,p.Y+4));Line(d,crease,.9,new Point(p.X+6,p.Y+1),new Point(p.X+6,p.Y+3));d.Pop();
 }
 static Geometry DeskShape(){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(OnDesk(new Point(20,210)),true,true);c.LineTo(OnDesk(new Point(710,210)),true,false);c.LineTo(OnDesk(new Point(710,440)),true,false);c.LineTo(OnDesk(new Point(20,440)),true,false);}g.Freeze();return g;}
 [ThreadStatic] static bool surface;
 [ThreadStatic] static bool keyboardSurface;
 public static Point KeyboardPoint(Point p){return new Point(993-p.X,651-p.Y);}
 static Point SurfacePoint(Point p){return OnDesk(keyboardSurface?KeyboardPoint(p):p);}
 public static Rect KeyboardBounds(Rect r){return DeskBounds(new Rect(KeyboardPoint(r.BottomRight),KeyboardPoint(r.TopLeft)));}
 public static Point OnDesk(Point p){double x=p.X-460,y=p.Y-320,q=1-.00045*x-.0006*y;return new Point(280+(.48*x-.35*y)/q,220+(.10*x+.40*y)/q);}
 public static Rect DeskBounds(Rect r){var a=OnDesk(r.TopLeft);var b=OnDesk(r.TopRight);var c=OnDesk(r.BottomLeft);var e=OnDesk(r.BottomRight);return new Rect(new Point(Math.Min(Math.Min(a.X,b.X),Math.Min(c.X,e.X)),Math.Min(Math.Min(a.Y,b.Y),Math.Min(c.Y,e.Y))),new Point(Math.Max(Math.Max(a.X,b.X),Math.Max(c.X,e.X)),Math.Max(Math.Max(a.Y,b.Y),Math.Max(c.Y,e.Y))));}
 static void Polygon(DrawingContext d,Brush fill,Pen pen,Point[] points){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(SurfacePoint(points[0]),true,true);for(int i=1;i<points.Length;i++)c.LineTo(SurfacePoint(points[i]),true,false);}g.Freeze();d.DrawGeometry(fill,pen,g);}
 public static Point StrikePoint(KeyCap cap){return OnDesk(KeyboardPoint(cap.Center));}
 static void Desk(DrawingContext d){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(OnDesk(new Point(20,210)),true,true);c.LineTo(OnDesk(new Point(710,210)),true,false);c.LineTo(OnDesk(new Point(710,440)),true,false);c.LineTo(OnDesk(new Point(20,440)),true,false);}g.Freeze();d.PushTransform(new TranslateTransform(0,6));d.DrawGeometry(B("#C9AA8C"),P("#502A21",2),g);d.Pop();d.DrawGeometry(B("#FFF4E3"),P("#502A21",2),g);}
 protected override void OnRender(DrawingContext d){base.OnRender(d);d.PushTransform(new ScaleTransform(ActualWidth/ViewWidth,ActualHeight/ViewHeight));Draw(d);d.Pop();}
 public void Draw(DrawingContext d){
 var s=State;double breath=s.Paused?0:Math.Sin(s.Time*2)*.6;double bx=70,by=5,yy=25+breath,yx=245;double characterScale=220.0/180;
 var cap=s.ActiveTarget;bool tapping=cap!=null;Point shoulder=new Point(yx+56*characterScale,yy+120*characterScale);Point head;
 if(tapping){var c=StrikePoint(cap);double age=Math.Max(0,s.Time-s.HitStarted);double lift=age<.055?18*(1-age/.055):age<.105?0:8*Math.Min(1,(age-.105)/.06);head=new Point(c.X,c.Y-lift);}
 else head=new Point(yx+31*characterScale,yy+116*characterScale);
 Vector toward=shoulder-head;if(toward.Length<.001)toward=new Vector(0,-1);double distance=toward.Length;toward.Normalize();double handleLength=tapping?Math.Min(68,Math.Max(12,distance*.65)):38;Point grip=head+toward*handleLength;
 double mx=181+s.MouseX,my=296+s.MouseY;Point localMouseGrip=new Point(mx,my-20+(s.ML||s.MR?7:0));Point mouseGrip=OnDesk(localMouseGrip);
 // Characters stand behind the desk. Foreground furniture occludes their lower bodies.
 d.DrawImage(bubu,new Rect(bx,by,220,234*220.0/230));d.DrawImage(yier,new Rect(yx,yy,220,234*220.0/230));
 Desk(d);
 surface=true;
 R(d,"#D6E7DE","#8CAE9C",1.6,85,245,180,125,24);
 // Compact main keyboard; removed navigation and numpad keys have no target. Every highlight is keyed by physical scan code.
 R(d,"#D5AFA4","#502A21",2,299,247,395,165,9);R(d,"#F5D6CE","#502A21",2,299,243,395,165,9);
 keyboardSurface=true;
 foreach(var k in KeyboardLayout.All){bool active=s.Active(k.Code);var r=k.Bounds;R(d,active?"#EAB56B":"#C5AA98",null,0,r.X,r.Y+2,r.Width,r.Height,3);R(d,active?"#FFD28B":"#FFFDF8",active?"#B57727":"#B89D8D",active?1.6:.75,r.X,r.Y+(active?2:0),r.Width,r.Height-2,3);double fs=k.Label.Contains("\n")?7.7:k.Label.Length==1?10:k.Label.Length>5?7.4:8.1;Text(d,k.Label,fs,active?"#6A3C13":"#65483C",new Rect(r.X,r.Y+(active?2:0),r.Width,r.Height-2));}
 keyboardSurface=false;
 surface=false;
 Arm(d,new Point(bx+56*characterScale,by+120*characterScale),new Point(mouseGrip.X+12,mouseGrip.Y-25),mouseGrip,"#D1A68F",16);
 Arm(d,shoulder,new Point(shoulder.X+(grip.X-shoulder.X)*.25,Math.Max(shoulder.Y+35,grip.Y-20)),grip,"#FFFCF7",16);
 // Hide shoulder seams under the original silhouette, preserving furniture occlusion.
 d.PushClip(new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,ViewWidth,ViewHeight)),DeskShape()));
 d.DrawImage(bubu,new Rect(bx,by,220,234*220.0/230));d.DrawImage(yier,new Rect(yx,yy,220,234*220.0/230));d.Pop();

 // Mirrored Bubu: anatomical left carries paddle, anatomical right controls mouse.
 surface=true;
 E(d,"#92B9A6",null,0,mx+2,my+18,35,30);R(d,"#FFFDF8","#502A21",2.4,mx-33,my-27,66,74,25);
 if(s.ML)R(d,"#F1A1AF",null,0,mx-28,my+13,26,23,8);if(s.MR)R(d,"#F4C371",null,0,mx+2,my+13,26,23,8);
 Line(d,"#967562",1.2,new Point(mx,my-19),new Point(mx,my+40));R(d,"#B3D4C0",null,0,mx-4,my-10,8,17,4);

 if(s.ML||s.MR){string c=s.ML?"#D87990":"#B68130";Line(d,c,2,new Point(mx+28,my),new Point(mx+38,my-5));Line(d,c,2,new Point(mx+29,my+9),new Point(mx+40,my+11));}
 if(s.Time<s.WheelUntil)Text(d,"↕",20,"#568A72",new Rect(mx+24,my-20,25,25));
 surface=false;
 Paw(d,mouseGrip,"#D1A68F",false);
 // Connected tapered arm and softly outlined gripping paw.
 Line(d,"#654137",9,head,grip+toward*3);Line(d,"#D8B493",5.5,head,grip+toward*3);
 Paw(d,grip,"#FFFCF7",true);
 double pw=44,ph=69*44.0/76;d.PushTransform(new RotateTransform(tapping?-8:0,head.X,head.Y));d.DrawImage(paddle,new Rect(head.X-pw/2,head.Y-ph/2,pw,ph));d.Pop();
 if(tapping&&s.Time-s.HitStarted<.14){Line(d,"#D89840",1.8,new Point(head.X-24,head.Y-3),new Point(head.X-30,head.Y-8));Line(d,"#D89840",1.8,new Point(head.X+24,head.Y-3),new Point(head.X+30,head.Y-8));}
 // Small unobstructed echo of the active cap; the original key remains outlined under the striking head.
 if(tapping){R(d,"#FFF9EC","#C69D68",.8,head.X-30,head.Y-51,60,25,5);Text(d,cap.Label.Replace("\n"," "),cap.Label.Length>4?11:15,"#6D482F",new Rect(head.X-30,head.Y-51,60,25));}
 if(Hint)Text(d,"拖动移动  ·  右键设置  ·  Ctrl+Alt+B 隐藏 / 显示",10,"#886A55",new Rect(0,332,500,16));
 }
 public static void Render(string path,InputState s){var v=new DrawingVisual();var scene=new Scene(s);using(var d=v.RenderOpen())scene.Draw(d);var bmp=new RenderTargetBitmap(750,525,144,144,PixelFormats.Pbgra32);bmp.Render(v);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var fs=File.Create(path))enc.Save(fs);}
}
}
