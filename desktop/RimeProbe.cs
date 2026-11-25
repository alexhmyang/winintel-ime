// GPL-3.0-or-later. Isolated native Rime deployment and candidate smoke test.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
class RimeProbe {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Ansi)]
    struct Traits {
        public int size;
        [MarshalAs(UnmanagedType.LPStr)] public string shared,user,name,code,version,app;
        public IntPtr modules; public int level;
        [MarshalAs(UnmanagedType.LPStr)] public string log,prebuilt,staging;
    }
    [StructLayout(LayoutKind.Sequential)] struct Composition {public int length,cursor,start,end;public IntPtr text;}
    [StructLayout(LayoutKind.Sequential)] struct Menu {public int size,page,last,highlight,count;public IntPtr candidates,keys;}
    [StructLayout(LayoutKind.Sequential)] struct Context {public int size;public Composition composition;public Menu menu;public IntPtr preview,labels;}
    [StructLayout(LayoutKind.Sequential)] struct Candidate {public IntPtr text,comment,reserved;}
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Init(ref Traits traits);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Void();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Maintenance(int full);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate UIntPtr Session();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Select(UIntPtr id,[MarshalAs(UnmanagedType.LPStr)] string schema);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Key(UIntPtr id,int key,int mask);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Clear(UIntPtr id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Get(UIntPtr id,ref Context ctx);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Free(ref Context ctx);
    [DllImport("rime.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr rime_get_api();
    static IntPtr api;
    static T Function<T>(int index) where T:class {return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(api,IntPtr.Size*(index+1)),typeof(T)) as T;}
    static string Utf8(IntPtr pointer) {
        if(pointer==IntPtr.Zero) return "";
        int size=0;while(Marshal.ReadByte(pointer,size)!=0) size++;
        byte[] data=new byte[size];Marshal.Copy(pointer,data,0,size);return Encoding.UTF8.GetString(data);
    }
    static int Main(string[] args) {
        try {
            api=rime_get_api();
            Traits traits=new Traits {size=Marshal.SizeOf(typeof(Traits))-4,shared=args[0],user=args[1],name="winintel AI",code="winintel",version="0.1.0",app="rime.winintel-probe",log=args[1],level=2};
            Function<Init>(0)(ref traits);Function<Init>(2)(ref traits);
            try {
                Function<Maintenance>(4)(1);Function<Void>(6)();
                UIntPtr id=Function<Session>(13)();
                if(id==UIntPtr.Zero || Function<Select>(34)(id,"winintel")==0) throw new Exception("Cannot activate winintel schema");
                foreach(string input in new [] {"nihao","rgzn","hello","zhongguo"}) {
                    Function<Clear>(20)(id);
                    foreach(char character in input) Function<Key>(18)(id,character,0);
                    Context context=new Context {size=Marshal.SizeOf(typeof(Context))-4};
                    if(Function<Get>(23)(id,ref context)==0) throw new Exception("Cannot get context");
                    try {
                        string candidates="";
                        for(int i=0;i<context.menu.count;i++) {
                            Candidate row=(Candidate)Marshal.PtrToStructure(IntPtr.Add(context.menu.candidates,i*Marshal.SizeOf(typeof(Candidate))),typeof(Candidate));
                            candidates+=Utf8(row.text)+" | ";
                        }
                        Console.WriteLine(input+": "+candidates);
                        string expected=input=="nihao"?"你好":input=="rgzn"?"人工智能":input=="hello"?"hello":"中国";
                        if(!candidates.Contains(expected)) throw new Exception("Missing expected native candidate: "+expected);
                    } finally {Function<Free>(24)(ref context);}
                }
                Console.WriteLine("PASS: native deployment, Lua filter, Chinese, abbreviation and English candidates");
            } finally {Function<Void>(3)();}
            return 0;
        } catch(Exception e) {Console.WriteLine(e);return 1;}
    }
}
