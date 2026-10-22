using System;
using System.IO;
using System.Threading;
using System.Windows;
namespace DuoPet {
class Program {
 [STAThread] static void Main(string[] args){try{
  Native.SetProcessDPIAware();
  if(args.Length>0&&args[0]=="--render") {Directory.CreateDirectory(args[1]);Scene.Render(Path.Combine(args[1],"idle.png"),new InputState());foreach(int code in new[]{0x1E,0x24,0x39,0x1C,0x01,0x58}){var s=KeyboardTests.Preview(code);Scene.Render(Path.Combine(args[1],"key-"+code+".png"),s);}var combined=KeyboardTests.Preview(0x1E);combined.MouseLeft=true;combined.MouseX=15;Scene.Render(Path.Combine(args[1],"typing.png"),combined);Scene.Render(Path.Combine(args[1],"mouse.png"),new InputState{MouseRight=true,MouseX=-18,MouseY=8});File.WriteAllText(Path.Combine(args[1],"render.ok"),"source cutouts and specific key targets rendered");return;}
  if(args.Length>0&&args[0]=="--keyboard-test"){KeyboardTests.Unit(args[1]);return;}
  if(args.Length>0&&args[0]=="--scan-test"){KeyboardTests.Integration(args[1]);return;}
  if(args.Length>0&&args[0]=="--self-test"){Tests.Unit(args[1]);return;}
  if(args.Length>0&&args[0]=="--integration"){Tests.Integration(args[1]);return;}
  if(args.Length>0&&args[0]=="--shell-test"){PetWindow.ShellTest(args[1]);return;}
  bool fresh;using(var mutex=new Mutex(true,"Local\\BubuYierPet.Instance",out fresh)){if(!fresh){try{using(var ev=EventWaitHandle.OpenExisting("Local\\BubuYierPet.Show"))ev.Set();}catch(WaitHandleCannotBeOpenedException){}return;}try{new Application().Run(new PetWindow(false));}finally{mutex.ReleaseMutex();}}
 }catch(Exception ex){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log"),ex.ToString());}catch{}Environment.ExitCode=1;if(args.Length==0)MessageBox.Show("启动失败："+ex.Message,"双人桌宠");}}
 }
}
