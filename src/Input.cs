using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace DuoPet {
public static class Native {
 public delegate IntPtr HookProc(int code,IntPtr wp,IntPtr lp);
 [DllImport("user32.dll",SetLastError=true)]public static extern IntPtr SetWindowsHookEx(int id,HookProc fn,IntPtr module,uint thread);
 [DllImport("user32.dll")]public static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]public static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr wp,IntPtr lp);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]public static extern IntPtr GetModuleHandle(string name);
 [DllImport("user32.dll")]public static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")]public static extern bool GetCursorPos(out POINT p);
 [DllImport("user32.dll")]public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]public static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint vk);
 [DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr hwnd,int id);
 [DllImport("user32.dll",EntryPoint="GetWindowLong")]public static extern int GetWindowLong(IntPtr h,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLong")]public static extern int SetWindowLong(IntPtr h,int index,int value);
 [DllImport("user32.dll",SetLastError=true)]public static extern uint SendInput(uint n,INPUT[] items,int size);
 [DllImport("user32.dll")]public static extern bool SetProcessDPIAware();
 [StructLayout(LayoutKind.Sequential)]public struct POINT{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]public struct KBD{public uint vk,scan,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]public struct MOUSE{public POINT pt;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]public struct INPUT{public uint type;public UNION data;}
 [StructLayout(LayoutKind.Explicit)]public struct UNION{[FieldOffset(0)]public MI mouse;[FieldOffset(0)]public KI key;}
 [StructLayout(LayoutKind.Sequential)]public struct MI{public int dx,dy;public uint data,flags,time;public UIntPtr extra;}
 [StructLayout(LayoutKind.Sequential)]public struct KI{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
 public static void Key(ushort vk,bool down){var i=new INPUT{type=1,data=new UNION{key=new KI{vk=vk,flags=down?0u:2u}}};if(SendInput(1,new[]{i},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Win32Exception();}
 public static void Scan(int code,bool down){uint flags=8u|((code&256)!=0?1u:0u)|(down?0u:2u);var i=new INPUT{type=1,data=new UNION{key=new KI{scan=(ushort)(code&255),flags=flags}}};if(SendInput(1,new[]{i},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Win32Exception();}
 public static void Move(int dx,int dy){var i=new INPUT{type=0,data=new UNION{mouse=new MI{dx=dx,dy=dy,flags=1}}};if(SendInput(1,new[]{i},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Win32Exception();}
 public static void Mouse(uint flags){var i=new INPUT{type=0,data=new UNION{mouse=new MI{flags=flags}}};if(SendInput(1,new[]{i},Marshal.SizeOf(typeof(INPUT)))!=1)throw new Win32Exception();}
}
public class InputEngine:IDisposable {
 public readonly InputState State; public readonly Stopwatch Clock=Stopwatch.StartNew();
 bool[] observed=new bool[256];int[] physicalVks=new int[512];bool[] observedPhysical=new bool[512];bool[] physicalHeld=new bool[512];int[] eventOrder=new int[512];int eventSerial=0; IntPtr keyboard,mouse;Native.HookProc keyFn,mouseFn;double targetX,targetY,lastReconcile;public bool Installed {get{return keyboard!=IntPtr.Zero&&mouse!=IntPtr.Zero;}}
 public InputEngine(InputState s){State=s;}
 public static bool IsRight(int v){return "YUIOPHJKLNM".IndexOf((char)v)>=0 || (v>=0x36&&v<=0x39)||v==0x30 || v==0x0D||v==0x08 || v>=0xBA || (v>=0x21&&v<=0x28)||(v>=0x60&&v<=0x6F)||v==0xA1||v==0xA3||v==0xA5;}
 public void Start(){keyFn=OnKey;mouseFn=OnMouse;var mod=Native.GetModuleHandle(null);keyboard=Native.SetWindowsHookEx(13,keyFn,mod,0);mouse=Native.SetWindowsHookEx(14,mouseFn,mod,0);if(!Installed){int error=Marshal.GetLastWin32Error();Dispose();throw new Win32Exception(error,"Cannot install desktop input hooks");}}
 public void FeedKey(int v,bool down){FeedPhysical(v,KeyboardLayout.DefaultCode(v),down);}
 public void FeedPhysical(int v,int code,bool down){if(v<8||v>=256)return;observed[v]=true;if(code>0&&code<512){physicalVks[code]=v;observedPhysical[code]=true;physicalHeld[code]=down;if(down)eventOrder[code]=++eventSerial;}if(State.Paused)return;State.Time=Clock.Elapsed.TotalSeconds;State.Keys[v]=down;if(code>0&&code<512){State.Physical[code]=down;if(down&&KeyboardLayout.Find(code)!=null){State.KeyPulse[code]=State.Time+.14;if(State.LastPhysical!=code||State.Time-State.HitStarted>.12)State.HitStarted=State.Time;State.LastPhysical=code;State.KeyOrder[code]=eventOrder[code];}}
 if(down){State.KeyCount++;if(IsRight(v))State.RightPulse=State.Time+.11;else State.LeftPulse=State.Time+.11;}Recompute();}
 void Recompute(){bool l=false,r=false;for(int k=8;k<256;k++)if(State.Keys[k]){if(IsRight(k))r=true;else l=true;}State.LeftKey=l;State.RightKey=r;}
 public void FeedMouse(int message,int x,int y){if(State.Paused)return;State.Time=Clock.Elapsed.TotalSeconds;if(message==0x200){State.MoveCount++;var bounds=Screen.FromPoint(new System.Drawing.Point(x,y)).Bounds;targetX=-Math.Max(-23,Math.Min(23,((x-bounds.Left)/(double)Math.Max(1,bounds.Width)-.5)*46));targetY=-Math.Max(-12,Math.Min(12,((y-bounds.Top)/(double)Math.Max(1,bounds.Height)-.5)*24));}else if(message==0x201){State.MouseLeft=true;State.MouseLPulse=State.Time+.13;State.LeftClicks++;}else if(message==0x202)State.MouseLeft=false;else if(message==0x204){State.MouseRight=true;State.MouseRPulse=State.Time+.13;State.RightClicks++;}else if(message==0x205)State.MouseRight=false;else if(message==0x20A||message==0x20E)State.WheelUntil=State.Time+.25;}
 IntPtr OnKey(int code,IntPtr wp,IntPtr lp){if(code>=0){int m=wp.ToInt32();if(m==0x100||m==0x104||m==0x101||m==0x105){var k=(Native.KBD)Marshal.PtrToStructure(lp,typeof(Native.KBD));FeedPhysical((int)k.vk,KeyboardLayout.Normalize((int)k.vk,k.scan,k.flags),m==0x100||m==0x104);}}return Native.CallNextHookEx(keyboard,code,wp,lp);}
 IntPtr OnMouse(int code,IntPtr wp,IntPtr lp){if(code>=0){var m=(Native.MOUSE)Marshal.PtrToStructure(lp,typeof(Native.MOUSE));FeedMouse(wp.ToInt32(),m.pt.X,m.pt.Y);}return Native.CallNextHookEx(mouse,code,wp,lp);}
 public void Tick(){State.Time=Clock.Elapsed.TotalSeconds;if(State.Paused)return;State.BodyX=540;State.MouseX+=(targetX-State.MouseX)*.3;State.MouseY+=(targetY-State.MouseY)*.3;if(State.Time-lastReconcile>.5){lastReconcile=State.Time;for(int i=8;i<256;i++)if(State.Keys[i]&&(Native.GetAsyncKeyState(i)&0x8000)==0)State.Keys[i]=false;for(int c=1;c<512;c++)if(State.Physical[c]&&(Native.GetAsyncKeyState(physicalVks[c])&0x8000)==0){State.Physical[c]=false;physicalHeld[c]=false;}Recompute();if((Native.GetAsyncKeyState(1)&0x8000)==0)State.MouseLeft=false;if((Native.GetAsyncKeyState(2)&0x8000)==0)State.MouseRight=false;}}
 public void Pause(bool pause){State.Clear();State.Paused=pause;if(!pause){for(int i=8;i<256;i++)State.Keys[i]=observed[i]&&!(i>=0xA6&&i<0xBA)&&(Native.GetAsyncKeyState(i)&0x8000)!=0;for(int c=1;c<512;c++)if(observedPhysical[c]&&physicalHeld[c]&&State.Keys[physicalVks[c]]){State.Physical[c]=true;State.KeyOrder[c]=eventOrder[c];}State.MouseLeft=(Native.GetAsyncKeyState(1)&0x8000)!=0;State.MouseRight=(Native.GetAsyncKeyState(2)&0x8000)!=0;Recompute();}}
 public void Dispose(){if(keyboard!=IntPtr.Zero){Native.UnhookWindowsHookEx(keyboard);keyboard=IntPtr.Zero;}if(mouse!=IntPtr.Zero){Native.UnhookWindowsHookEx(mouse);mouse=IntPtr.Zero;}State.Clear();}
}
}
