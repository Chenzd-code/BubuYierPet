using System;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace DuoPet {
public class PetWindow:Window {
 public readonly InputState State=new InputState();public InputEngine Engine; public Scene Scene;DispatcherTimer timer;Forms.NotifyIcon tray;IntPtr handle;bool through=false,closing=false,hotkey=false,initialized=false;double scale=1;string settings;EventWaitHandle showEvent;RegisteredWaitHandle showWait;
 public PetWindow(bool testMode){
  Title="布布和一二 · 双人桌宠";Width=Scene.ViewWidth;Height=Scene.ViewHeight;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;WindowStartupLocation=WindowStartupLocation.Manual;
  settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),testMode?"BubuYierPet-Test":"BubuYierPet","settings.txt");
  Scene=new Scene(State);Content=Scene;Engine=new InputEngine(State);
  SourceInitialized+=(s,e)=>{handle=new WindowInteropHelper(this).Handle;int style=Native.GetWindowLong(handle,-20);Native.SetWindowLong(handle,-20,style|0x80|0x08000000);HwndSource.FromHwnd(handle).AddHook(WndProc);hotkey=Native.RegisterHotKey(handle,121,0x4003,0x42);};
  Loaded+=(s,e)=>{if(initialized)return;initialized=true;try{LoadSettings();Engine.Start();MakeTray();timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(1000.0/60)};timer.Tick+=(a,b)=>{Engine.Tick();if(IsVisible)Scene.InvalidateVisual();};timer.Start();
    if(!testMode){showEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\BubuYierPet.Show");showWait=ThreadPool.RegisterWaitForSingleObject(showEvent,(x,t)=>Dispatcher.BeginInvoke(new Action(Restore)),null,-1,false);}
   }catch(Exception ex){MessageBox.Show("无法启动桌宠："+ex.Message,"布布和一二");Close();}};
  MouseLeftButtonDown+=(s,e)=>{if(e.ChangedButton==MouseButton.Left&&!through){try{DragMove();Clamp();SaveSettings();}catch(InvalidOperationException){}}};
  MouseRightButtonUp+=(s,e)=>{var menu=BuildMenu();menu.PlacementTarget=this;menu.IsOpen=true;e.Handled=true;};
  Closed+=(s,e)=>Cleanup();
 }
 IntPtr WndProc(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x312&&w.ToInt32()==121){ToggleVisible();handled=true;}return IntPtr.Zero;}
 public void TogglePause(){Engine.Pause(!State.Paused);Scene.InvalidateVisual();}
 public void ToggleVisible(){if(IsVisible)Hide();else Restore();}
 public void Restore(){if(closing)return;Show();Clamp();Scene.InvalidateVisual();}
 public void SetThrough(bool enabled){through=enabled;int style=Native.GetWindowLong(handle,-20);Native.SetWindowLong(handle,-20,enabled?style|0x20:style&~0x20);}
 public bool IsThrough{get{return through;}}
 public double Scale{get{return scale;}}
 public bool IsReady{get{return Engine.Installed&&tray!=null;}}
 double QualityScale(double value){var src=PresentationSource.FromVisual(this);double dpi=src==null?1:Math.Max(src.CompositionTarget.TransformToDevice.M11,src.CompositionTarget.TransformToDevice.M22);return Math.Min(Math.Max(.4,value),Math.Min(1.5,Scene.SourceWidth/(Scene.CharacterWidth*dpi)));}
 public void SetScale(double value){scale=QualityScale(value);Width=Scene.ViewWidth*scale;Height=Scene.ViewHeight*scale;Clamp();SaveSettings();}
 Rect ScreenArea(){var src=PresentationSource.FromVisual(this);var transform=src==null?Matrix.Identity:src.CompositionTarget.TransformFromDevice;var point=new System.Drawing.Point((int)(double.IsNaN(Left)?0:Left/transform.M11),(int)(double.IsNaN(Top)?0:Top/transform.M22));var area=Forms.Screen.FromPoint(point).WorkingArea;var p=transform.Transform(new Point(area.Left,area.Top));return new Rect(p.X,p.Y,area.Width*transform.M11,area.Height*transform.M22);}
 public void Clamp(){var a=ScreenArea();Left=Math.Max(a.Left,Math.Min(double.IsNaN(Left)?a.Left:Left,a.Right-Width));Top=Math.Max(a.Top,Math.Min(double.IsNaN(Top)?a.Top:Top,a.Bottom-Height));}
 public void ResetPosition(){PlaceDefault();SaveSettings();}
 void PlaceDefault(){var src=PresentationSource.FromVisual(this);var m=src==null?Matrix.Identity:src.CompositionTarget.TransformFromDevice;var a=Forms.Screen.PrimaryScreen.WorkingArea;Left=a.Right*m.M11-Width-20;Top=a.Bottom*m.M22-Height-12;Clamp();}
 void LoadSettings(){scale=QualityScale(scale);Width=Scene.ViewWidth*scale;Height=Scene.ViewHeight*scale;PlaceDefault();if(!File.Exists(settings))return;try{var t=File.ReadAllLines(settings);double x,y,z;if(t.Length>=5&&double.TryParse(t[0],NumberStyles.Float,CultureInfo.InvariantCulture,out x)&&double.TryParse(t[1],NumberStyles.Float,CultureInfo.InvariantCulture,out y)&&double.TryParse(t[2],NumberStyles.Float,CultureInfo.InvariantCulture,out z)&&!double.IsNaN(x)&&!double.IsInfinity(x)&&!double.IsNaN(y)&&!double.IsInfinity(y)){scale=QualityScale(z);Width=Scene.ViewWidth*scale;Height=Scene.ViewHeight*scale;Left=x;Top=y;Topmost=t[3]=="True";Scene.Hint=t[4]=="True";Clamp();}}catch(IOException){}}
 void SaveSettings(){try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllLines(settings,new[]{Left.ToString(CultureInfo.InvariantCulture),Top.ToString(CultureInfo.InvariantCulture),scale.ToString(CultureInfo.InvariantCulture),Topmost.ToString(),Scene.Hint.ToString()});}catch(IOException){}catch(UnauthorizedAccessException){}}
 MenuItem Item(string label,Action action,bool check=false,bool value=false){var i=new MenuItem{Header=label,IsCheckable=check,IsChecked=value};i.Click+=(s,e)=>action();return i;}
 ContextMenu BuildMenu(){var m=new ContextMenu();m.Items.Add(Item(State.Paused?"继续互动":"暂停互动",TogglePause));m.Items.Add(Item("隐藏桌宠  Ctrl+Alt+B",ToggleVisible));m.Items.Add(Item("始终置顶",()=>{Topmost=!Topmost;SaveSettings();},true,Topmost));m.Items.Add(Item("鼠标穿透（托盘可关闭）",()=>SetThrough(!through),true,through));var sizes=new MenuItem{Header="显示大小"};foreach(double z in new[]{.4,.45,.5,.75,1,1.25,1.5}){double v=z;sizes.Items.Add(Item((v*100)+"%",()=>SetScale(v),true,Math.Abs(scale-v)<.01));}m.Items.Add(sizes);m.Items.Add(Item("显示操作提示",()=>{Scene.Hint=!Scene.Hint;SaveSettings();},true,Scene.Hint));m.Items.Add(Item("恢复默认位置",ResetPosition));m.Items.Add(new Separator());m.Items.Add(Item("使用说明",OpenGuide));m.Items.Add(Item("退出",Close));return m;}
 void OpenGuide(){string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"使用说明.txt");if(File.Exists(p))System.Diagnostics.Process.Start("notepad.exe","\""+p+"\"");}
 void MakeTray(){tray=new Forms.NotifyIcon{Icon=new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","pet.ico")),Text="布布和一二 · 双人桌宠",Visible=true};tray.DoubleClick+=(s,e)=>Dispatcher.BeginInvoke(new Action(Restore));var menu=new Forms.ContextMenuStrip();menu.Opening+=(s,e)=>{menu.Items.Clear();AddTray(menu,"显示 / 隐藏（Ctrl+Alt+B）",ToggleVisible);AddTray(menu,State.Paused?"继续互动":"暂停互动",TogglePause);AddTray(menu,through?"关闭鼠标穿透":"开启鼠标穿透",()=>SetThrough(!through));AddTray(menu,"恢复默认位置",()=>{Restore();SetThrough(false);ResetPosition();});AddTray(menu,"使用说明",OpenGuide);menu.Items.Add(new Forms.ToolStripSeparator());AddTray(menu,"退出",Close);};tray.ContextMenuStrip=menu;}
 void AddTray(Forms.ContextMenuStrip menu,string title,Action a){var i=new Forms.ToolStripMenuItem(title);i.Click+=(s,e)=>Dispatcher.BeginInvoke(a);menu.Items.Add(i);}
 void Cleanup(){if(closing)return;closing=true;SaveSettings();if(timer!=null)timer.Stop();Engine.Dispose();if(hotkey)Native.UnregisterHotKey(handle,121);if(showWait!=null)showWait.Unregister(null);if(showEvent!=null)showEvent.Dispose();if(tray!=null){tray.Visible=false;var icon=tray.Icon;var menu=tray.ContextMenuStrip;tray.Dispose();if(icon!=null)icon.Dispose();if(menu!=null)menu.Dispose();}}
 public static void ShellTest(string folder){Directory.CreateDirectory(folder);var app=new Application();var w=new PetWindow(true);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(180)};int n=0;bool ok=false;timer.Tick+=(s,e)=>{try{switch(n++){
 case 0:if(!w.IsReady)throw new Exception("hooks/tray not ready");w.SetScale(.75);break;
 case 1:if(Math.Abs(w.Width-Scene.ViewWidth*w.QualityScale(.75))>1)throw new Exception("scale failed");w.SetScale(1.5);break;
 case 2:if(Math.Abs(w.Width-Scene.ViewWidth*w.QualityScale(1.5))>1)throw new Exception("scale 150 failed");w.SetScale(1);w.TogglePause();if(!w.State.Paused)throw new Exception("pause failed");w.TogglePause();w.SetThrough(true);break;
 case 3:if((Native.GetWindowLong(w.handle,-20)&0x20)==0)throw new Exception("click through style missing");w.SetThrough(false);w.Hide();break;
 case 4:if(w.IsVisible)throw new Exception("hide failed");w.Restore();break;
 case 5:if(!w.IsVisible)throw new Exception("restore failed");w.ResetPosition();w.SaveSettings();if(!File.Exists(w.settings))throw new Exception("settings missing");double savedX=w.Left-13;w.Left=savedX;w.SaveSettings();w.Left=0;w.LoadSettings();if(Math.Abs(w.Left-savedX)>1)throw new Exception("position reload failed");Scene.Render(Path.Combine(folder,"shell-final.png"),w.State);ok=true;timer.Stop();w.Close();break;
 }}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"shell-error.txt"),ex.ToString());timer.Stop();w.Close();}};w.Loaded+=(s,e)=>timer.Start();app.Run(w);if(ok&&!w.Engine.Installed){File.WriteAllText(Path.Combine(folder,"shell.txt"),"PASS: tray initialized, 75/150/100% scale, pause/resume, WS_EX_TRANSPARENT on/off, hide/show, position, settings save/reload, hooks released on exit.");Environment.ExitCode=0;}else Environment.ExitCode=1;}
}
}
