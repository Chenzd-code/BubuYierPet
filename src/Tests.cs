using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace DuoPet {
class Tests {
 static void Check(bool b,string text){if(!b)throw new Exception(text);}
 public static void Unit(string path){var s=new InputState();using(var e=new InputEngine(s)){
 e.FeedKey(65,true);Check(s.L&&!s.R,"left keyboard isolation");e.FeedKey(74,true);Check(s.L&&s.R,"simultaneous keyboard");e.FeedKey(65,false);e.FeedKey(74,false);s.Time+=1;Check(!s.L&&!s.R,"key release");
 e.FeedMouse(0x201,0,0);Check(s.ML&&!s.MR&&!s.LeftKey,"mouse left isolation");e.FeedMouse(0x204,0,0);Check(s.ML&&s.MR,"simultaneous mouse");e.FeedMouse(0x202,0,0);e.FeedMouse(0x205,0,0);s.Time+=1;Check(!s.ML&&!s.MR,"mouse release");
 e.Pause(true);int count=s.KeyCount;e.FeedKey(65,true);e.FeedMouse(0x201,0,0);Check(s.KeyCount==count&&!s.ML&&!s.L,"pause clears and ignores");e.Pause(false);e.FeedKey(65,true);e.FeedKey(65,false);Check(s.L,"short taps visible");s.Time+=1;Check(!s.L,"pulse expires");e.FeedMouse(0x200,-100000,100000);for(int i=0;i<100;i++)e.Tick();Check(Math.Abs(s.MouseX)<=23&&Math.Abs(s.MouseY)<=12,"motion bounded");
 }File.WriteAllText(path,"PASS: keyboard isolation, simultaneous input, release, left/right mouse, pause/resume, short-tap visibility, bounded movement");}
 public static void Integration(string folder){Directory.CreateDirectory(folder);var app=new Application();var s=new InputState();var engine=new InputEngine(s);var w=new Window{Title="双人桌宠 · 输入验证",Width=360,Height=150,Background=Brushes.White,WindowStartupLocation=WindowStartupLocation.CenterScreen,Topmost=true};w.Content=new System.Windows.Controls.TextBlock{Text="正在验证键鼠响应，请稍候…",Margin=new Thickness(24),FontSize=16};var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(240)};int step=0;bool success=false;Native.POINT original;Native.GetCursorPos(out original);IntPtr hwnd=IntPtr.Zero;
 Action done=()=>{timer.Stop();Native.Key(65,false);Native.Key(74,false);Native.Mouse(4|16);Native.SetCursorPos(original.X,original.Y);engine.Dispose();w.Close();};
 timer.Tick+=(sender,ev)=>{try{
  engine.Tick();if(Native.GetForegroundWindow()!=hwnd)throw new Exception("Test window lost focus at step "+step+"; injection aborted");
  switch(step++){
   case 0:Native.Key(65,true);break;
   case 1:Check(s.LeftKey&&!s.RightKey,"global A down not received");Native.Key(74,true);break;
   case 2:Check(s.LeftKey&&s.RightKey,"global simultaneous keys");Scene.Render(Path.Combine(folder,"typing-global.png"),s);Native.Key(65,false);Native.Key(74,false);break;
   case 3:Check(!s.L&&!s.R,"global key release");var p=w.PointToScreen(new Point(150,85));Check(Native.SetCursorPos((int)p.X,(int)p.Y),"SetCursorPos failed"); Native.Move(8,4);break;
   case 4:Check(s.MoveCount>0,"global movement");Native.Mouse(2);break;
   case 5:Check(s.MouseLeft&&!s.MouseRight,"global left click");Scene.Render(Path.Combine(folder,"left-global.png"),s);Native.Mouse(4);break;
   case 6:Check(!s.ML,"global left release");Native.Mouse(8);break;
   case 7:Check(s.MouseRight&&!s.MouseLeft,"global right click");Scene.Render(Path.Combine(folder,"right-global.png"),s);Native.Mouse(16);break;
   case 8:Check(!s.MR,"global right release");Native.Key(65,true);Native.Mouse(2);break;
   case 9:Check(s.LeftKey&&s.MouseLeft,"keyboard+mouse independence");Scene.Render(Path.Combine(folder,"combined-global.png"),s);Native.Key(65,false);Native.Mouse(4);engine.Pause(true);Native.Key(74,true);break;
   case 10:Check(!s.L&&!s.R&&!s.ML&&!s.MR,"paused input");Native.Key(74,false);engine.Pause(false);Native.Key(74,true);break;
   case 11:Check(s.RightKey,"resume hook");Native.Key(74,false);break;
   case 12:Check(!s.R,"final release");engine.Pause(true);Native.Key(65,true);Native.Mouse(2);break;
   case 13:engine.Pause(false);Check(s.LeftKey&&s.MouseLeft,"held input resync on resume");Native.Key(65,false);Native.Mouse(4);break;
   case 14:Check(!s.L&&!s.ML,"resumed hold release");success=true;File.WriteAllText(Path.Combine(folder,"integration.txt"),"PASS: real WH_KEYBOARD_LL + WH_MOUSE_LL hooks; SendInput key down/up, independent hands, pointer movement, left/right click down/up, combined keyboard+mouse, pause/resume including held-state resynchronization. Hook counts: keys="+s.KeyCount+", moves="+s.MoveCount+", left="+s.LeftClicks+", right="+s.RightClicks);done();break;
  }
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"integration-error.txt"),ex.ToString());done();}};
 w.Loaded+=(sender,ev)=>{try{hwnd=new WindowInteropHelper(w).Handle;w.Activate();w.Focus();engine.Start();timer.Start();}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"integration-error.txt"),ex.ToString());done();}};
 app.Run(w);Environment.ExitCode=success?0:1;
 }
}
}
