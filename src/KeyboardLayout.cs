using System;
using System.Collections.Generic;
using System.Windows;
namespace DuoPet {
public sealed class KeyCap {
 public int Code,Vk; public string Label; public Rect Bounds;
 public Point Center {get{return new Point(Bounds.X+Bounds.Width/2,Bounds.Y+Bounds.Height/2);}}
 public KeyCap(int code,int vk,string label,double unit,double y,double width,double height,double origin){Code=code;Vk=vk;Label=label;Bounds=new Rect(origin+unit*25,y,width*25-3,height*26-3);}
}
public static class KeyboardLayout {
 public static readonly List<KeyCap> All=Build();
 static void Add(List<KeyCap> a,int code,int vk,string label,double unit,double y,double width=1,double height=1,double origin=310){a.Add(new KeyCap(code,vk,label,unit,y,width,height,origin));}
 static List<KeyCap> Build(){var a=new List<KeyCap>();
 Add(a,1,27,"Esc",0,250);for(int i=0;i<12;i++)Add(a,i<10?0x3B+i:0x57+i-10,0x70+i,"F"+(i+1),2+i+(i/4)*.5,250);
 Add(a,0x137,0x2C,"PrtSc",0,250,1,1,700);Add(a,0x46,0x91,"ScrLk",1,250,1,1,700);Add(a,0x45,0x13,"Pause",2,250,1,1,700);
 string[] numbers={"~\n`","!\n1","@\n2","#\n3","$\n4","%\n5","^\n6","&\n7","*\n8","(\n9",")\n0","_\n-","+\n="};
 int[] nvk={0xC0,49,50,51,52,53,54,55,56,57,48,0xBD,0xBB};
 for(int i=0;i<13;i++)Add(a,i==0?0x29:i+1,nvk[i],numbers[i],i,282);Add(a,0x0E,8,"Backspace",13,282,2);
 Add(a,0x0F,9,"Tab",0,308,1.5);string q="QWERTYUIOP";for(int i=0;i<q.Length;i++)Add(a,0x10+i,q[i],q[i].ToString(),1.5+i,308);
 Add(a,0x1A,0xDB,"{\n[",11.5,308);Add(a,0x1B,0xDD,"}\n]",12.5,308);Add(a,0x2B,0xDC,"|\n\\",13.5,308,1.5);
 Add(a,0x3A,0x14,"Caps",0,334,1.75);string h="ASDFGHJKL";for(int i=0;i<h.Length;i++)Add(a,0x1E+i,h[i],h[i].ToString(),1.75+i,334);
 Add(a,0x27,0xBA,":\n;",10.75,334);Add(a,0x28,0xDE,"\"\n'",11.75,334);Add(a,0x1C,13,"Enter",12.75,334,2.25);
 Add(a,0x2A,0xA0,"Shift",0,360,2.25);string z="ZXCVBNM";for(int i=0;i<z.Length;i++)Add(a,0x2C+i,z[i],z[i].ToString(),2.25+i,360);
 Add(a,0x33,0xBC,"<\n,",9.25,360);Add(a,0x34,0xBE,">\n.",10.25,360);Add(a,0x35,0xBF,"?\n/",11.25,360);Add(a,0x36,0xA1,"Shift",12.25,360,2.75);
 Add(a,0x1D,0xA2,"Ctrl",0,386,1.25);Add(a,0x15B,0x5B,"Win",1.25,386,1.25);Add(a,0x38,0xA4,"Alt",2.5,386,1.25);Add(a,0x39,32,"Space",3.75,386,6.25);Add(a,0x138,0xA5,"Alt",10,386,1.25);Add(a,0x15C,0x5C,"Win",11.25,386,1.25);Add(a,0x15D,0x5D,"Menu",12.5,386,1.25);Add(a,0x11D,0xA3,"Ctrl",13.75,386,1.25);
 int[] nc={0x152,0x147,0x149,0x153,0x14F,0x151};int[] nv={0x2D,0x24,0x21,0x2E,0x23,0x22};string[] nl={"Ins","Home","PgUp","Del","End","PgDn"};for(int i=0;i<6;i++)Add(a,nc[i],nv[i],nl[i],i%3,282+(i/3)*26,1,1,700);
 Add(a,0x148,0x26,"↑",1,360,1,1,700);Add(a,0x14B,0x25,"←",0,386,1,1,700);Add(a,0x150,0x28,"↓",1,386,1,1,700);Add(a,0x14D,0x27,"→",2,386,1,1,700);
 Add(a,0x145,0x90,"Num",0,282,1,1,790);Add(a,0x135,0x6F,"/",1,282,1,1,790);Add(a,0x37,0x6A,"*",2,282,1,1,790);Add(a,0x4A,0x6D,"-",3,282,1,1,790);
 int[] codes={0x47,0x48,0x49,0x4B,0x4C,0x4D,0x4F,0x50,0x51};string[] labs={"7\nHome","8\n↑","9\nPgUp","4\n←","5","6\n→","1\nEnd","2\n↓","3\nPgDn"};int[] digs={7,8,9,4,5,6,1,2,3};for(int i=0;i<9;i++)Add(a,codes[i],0x60+digs[i],labs[i],i%3,308+(i/3)*26,1,1,790);
 Add(a,0x4E,0x6B,"+",3,308,1,2,790);Add(a,0x11C,13,"Enter",3,360,1,2,790);Add(a,0x52,0x60,"0  Ins",0,386,2,1,790);Add(a,0x53,0x6E,".\nDel",2,386,1,1,790);
 a.RemoveAll(k=>k.Bounds.X>=690);return a;}
 public static KeyCap Find(int code){return All.Find(k=>k.Code==code);}
 public static int DefaultCode(int vk){if(vk==16)vk=0xA0;if(vk==17)vk=0xA2;if(vk==18)vk=0xA4;var k=All.Find(x=>x.Vk==vk);return k==null?0:k.Code;}
 public static int Normalize(int vk,uint scan,uint flags){if(vk==0x13)return 0x45;if(vk==0x2C)return 0x137;if(vk==0x90)return 0x145;int code=(int)(scan&255)|((flags&1)!=0?256:0);return code==0?DefaultCode(vk):code;}
}
}
