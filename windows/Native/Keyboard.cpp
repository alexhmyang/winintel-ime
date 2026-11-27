// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#include "Core.h"
#include "Composition.h"
#include "../../standalone/EnglishLexicon.h"
#include <unordered_set>
class Keyboard;
static Keyboard* active=nullptr;
#ifdef WININTEL_TEST_STORE
static ITfContext* TestContext();
#endif
class Keyboard : public ITfTextInputProcessorEx,public ITfKeyEventSink,public ITfThreadMgrEventSink {
  long refs=1;ITfThreadMgr* manager=nullptr;TfClientId client=TF_CLIENTID_NULL;DWORD focusCookie=TF_INVALID_COOKIE;bool advised=false;
  Engine engine;EnglishLexicon english;CandidateView view;std::string code;Composition edit{code};std::vector<Candidate> rows;unsigned selection=0;
  HWND bridge=nullptr;HANDLE modeMap=nullptr;LONG* sharedMode=nullptr;int localMode=0;bool shiftDown=false,shiftUsed=false,raw=false;
  bool probeKey=false;
  ITfContext* englishContext=nullptr;
#ifdef WININTEL_TEST_STORE
  bool probeControl=false;
#endif
  SHORT State(int key)const{
#ifdef WININTEL_TEST_STORE
    if(probeKey)return (key==VK_SHIFT&&shiftDown)||(key==VK_CONTROL&&probeControl)?(SHORT)0x8000:0;
#endif
    return GetKeyState(key);
  }
  int Mode()const{return sharedMode?(int)InterlockedCompareExchange(sharedMode,0,0):localMode;}
  static bool Shift(WPARAM k){return k==VK_SHIFT||k==VK_LSHIFT||k==VK_RSHIFT;}
  void SetMode(int value){if(sharedMode)InterlockedExchange(sharedMode,value);else localMode=value;}
  bool EnglishWord()const{
    static const std::unordered_set<std::string> words={"hello","world","windows","linux","python","github","google","server","please","thanks","thank","email","meeting","project","update","version","release","download","upload","install","offline","online","translate","translation","english","computer","customer","confirm","requirements","deepseek","winintel","http","https","www","cuda","nvlink","openai","chatgpt","docker","ubuntu","pytorch","android","microsoft","office","password","username","support","hardware","software","network","model","token","benchmark","performance","memory","intel","nvidia"};
    return words.count(code)>0;
  }
  bool Eats(WPARAM k){
    if(State(VK_MENU)<0||State(VK_LWIN)<0||State(VK_RWIN)<0)return false;
    if(State(VK_CONTROL)<0)return Mode()!=2&&!code.empty()&&(k=='Z'||k=='Y');
    if(Shift(k))return true;
    if(Mode()==2)return (k>='A'&&k<='Z')||(!code.empty()&&(k==VK_TAB||k==VK_SPACE||k==VK_BACK||k==VK_DELETE||k==VK_LEFT||k==VK_RIGHT||k==VK_HOME||k==VK_END||k==VK_ESCAPE||k==VK_UP||k==VK_DOWN));
    return (k>='A'&&k<='Z')||(!code.empty()&&(k==VK_SPACE||k==VK_RETURN||k==VK_BACK||k==VK_DELETE||k==VK_LEFT||k==VK_RIGHT||k==VK_HOME||k==VK_END||k==VK_ESCAPE||k==VK_UP||k==VK_DOWN||k==VK_PRIOR||k==VK_NEXT||k==VK_OEM_MINUS||k==VK_OEM_PLUS||k==VK_OEM_PERIOD||k==VK_OEM_2||(k>='0'&&k<='9')));
  }
  HRESULT Flush(ITfContext* context,bool original){if(code.empty())return S_OK;HRESULT hr=Insert(context,client,original||rows.empty()?std::wstring(code.begin(),code.end()):rows[selection].text);if(SUCCEEDED(hr))Clear();return hr;}
  HRESULT FocusFlush(){ITfDocumentMgr* doc=nullptr;ITfContext* c=nullptr;HRESULT hr=E_PENDING;if(manager&&SUCCEEDED(manager->GetFocus(&doc))&&doc){doc->GetTop(&c);if(c){hr=Flush(c,true);c->Release();}doc->Release();}return hr;}
  static LRESULT CALLBACK BridgeProc(HWND h,UINT m,WPARAM w,LPARAM l){
    auto self=(Keyboard*)GetWindowLongPtr(h,GWLP_USERDATA);
    if(m==WM_NCCREATE){self=(Keyboard*)((CREATESTRUCT*)l)->lpCreateParams;SetWindowLongPtr(h,GWLP_USERDATA,(LONG_PTR)self);}
    if(self&&m==WM_APP+54){if(w){if(!self->code.empty()&&FAILED(self->FocusFlush()))return -1;self->SetMode(w==4?0:w==3?1:self->Mode()==2?1:2);}return self->Mode();}
    if(self&&m==WM_APP+55){if(self->code.empty())return 0;return SUCCEEDED(self->FocusFlush())?0:-2;}
    return DefWindowProc(h,m,w,l);
  }
  void StartBridge(){
    if(bridge)return;
#ifdef WININTEL_TEST_STORE
    std::wstring modeName=L"Local\\winintel-ime-language054-probe-"+std::to_wstring(GetCurrentProcessId());
    modeMap=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(LONG),modeName.c_str());
#else
    modeMap=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(LONG),L"Local\\winintel-ime-language054");
#endif
    if(modeMap)sharedMode=(LONG*)MapViewOfFile(modeMap,FILE_MAP_ALL_ACCESS,0,0,sizeof(LONG));
    WNDCLASSW wc={};wc.lpfnWndProc=BridgeProc;wc.hInstance=module;wc.lpszClassName=L"winintel.Language.054";RegisterClassW(&wc);
    bridge=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE,wc.lpszClassName,L"",WS_POPUP,0,0,0,0,nullptr,nullptr,module,this);
  }
  void Clear(){edit.Clear();rows.clear();selection=0;raw=false;view.Hide();if(englishContext){englishContext->Release();englishContext=nullptr;}}
  void Blur(){if(Mode()==2&&englishContext&&!code.empty())Flush(englishContext,true);Clear();shiftDown=false;}
  void Refresh(ITfContext* context){
    if(code.empty()){Clear();return;}
    if(Mode()==2&&englishContext!=context){if(englishContext)englishContext->Release();englishContext=context;englishContext->AddRef();}
    raw=code.find_first_not_of("abcdefghijklmnopqrstuvwxyz")!=std::string::npos||(Mode()==0&&EnglishWord());
    rows=Mode()==2?english.query(code):engine.query(code);
    Candidate original{std::wstring(code.begin(),code.end()),0};
    if(Mode()!=2&&raw&&(rows.empty()||rows[0].text!=original.text))rows.insert(rows.begin(),original);
    selection=0;view.Show(context,client,code,rows,selection,edit.caret,Mode()==2);
  }
  void Learn(const std::wstring& text){if(!text.empty()&&text[0]>=0x3400)engine.learn(code,text);}
  void ShowFocused(){
    ITfDocumentMgr* doc=nullptr;ITfContext* context=nullptr;
    if(manager&&SUCCEEDED(manager->GetFocus(&doc))&&doc){doc->GetTop(&context);if(context){view.Show(context,client,code,rows,selection,edit.caret,Mode()==2);context->Release();}doc->Release();}
  }
  void ChangePage(int direction){if(code.empty())return;selection=CandidatePaging::move(selection,(unsigned)rows.size(),direction);ShowFocused();}
  void Choose(unsigned offset){
    unsigned index=CandidatePaging::start(selection)+offset;
    if(code.empty()||index>=rows.size()||!manager)return;
    ITfDocumentMgr* doc=nullptr;ITfContext* context=nullptr;
    if(SUCCEEDED(manager->GetFocus(&doc))&&doc){
      if(SUCCEEDED(doc->GetTop(&context))&&context){
        if(SUCCEEDED(Insert(context,client,rows[index].text))){Learn(rows[index].text);Clear();}
        context->Release();
      }doc->Release();
    }
  }
public:
  Keyboard(){InterlockedIncrement(&objects);view.Page=[this](int direction){ChangePage(direction);};view.Choose=[this](unsigned offset){Choose(offset);};view.MoveCaret=[this](unsigned position){edit.Move(position);ShowFocused();};}
  ~Keyboard(){Deactivate();InterlockedDecrement(&objects);}
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID i,void** p)override{
    if(!p)return E_POINTER;*p=nullptr;
    if(i==IID_IUnknown||i==IID_ITfTextInputProcessor||i==IID_ITfTextInputProcessorEx)*p=(ITfTextInputProcessorEx*)this;
    else if(i==IID_ITfKeyEventSink)*p=(ITfKeyEventSink*)this;
    else if(i==IID_ITfThreadMgrEventSink)*p=(ITfThreadMgrEventSink*)this;
    if(!*p)return E_NOINTERFACE;AddRef();return S_OK;
  }
  ULONG STDMETHODCALLTYPE AddRef()override{return InterlockedIncrement(&refs);}
  ULONG STDMETHODCALLTYPE Release()override{auto r=InterlockedDecrement(&refs);if(!r)delete this;return r;}
  HRESULT STDMETHODCALLTYPE Activate(ITfThreadMgr* m,TfClientId c)override{return ActivateEx(m,c,0);}
  HRESULT STDMETHODCALLTYPE ActivateEx(ITfThreadMgr* m,TfClientId c,DWORD)override{
    if(manager)return S_OK;if(!engine.open(Root()+L"\\dictionary.bin"))return E_FAIL;english.open(Root());
    manager=m;manager->AddRef();client=c;ITfKeystrokeMgr* keys=nullptr;
    HRESULT hr=m->QueryInterface(IID_ITfKeystrokeMgr,(void**)&keys);
    if(SUCCEEDED(hr)){hr=keys->AdviseKeyEventSink(client,this,TRUE);keys->Release();}
    if(FAILED(hr)){manager->Release();manager=nullptr;return hr;}
    advised=true;
    ITfSource* source=nullptr;if(SUCCEEDED(m->QueryInterface(IID_ITfSource,(void**)&source))){source->AdviseSink(IID_ITfThreadMgrEventSink,(ITfThreadMgrEventSink*)this,&focusCookie);source->Release();}
    active=this;StartBridge();return S_OK;
  }
  HRESULT STDMETHODCALLTYPE Deactivate()override{
    Clear();shiftDown=false;if(bridge){DestroyWindow(bridge);bridge=nullptr;}if(sharedMode){UnmapViewOfFile(sharedMode);sharedMode=nullptr;}if(modeMap){CloseHandle(modeMap);modeMap=nullptr;}if(active==this)active=nullptr;if(manager){
      ITfKeystrokeMgr* keys=nullptr;if(advised&&SUCCEEDED(manager->QueryInterface(IID_ITfKeystrokeMgr,(void**)&keys))){keys->UnadviseKeyEventSink(client);keys->Release();}advised=false;
      ITfSource* s=nullptr;if(focusCookie!=TF_INVALID_COOKIE&&SUCCEEDED(manager->QueryInterface(IID_ITfSource,(void**)&s))){s->UnadviseSink(focusCookie);s->Release();}
      manager->Release();manager=nullptr;focusCookie=TF_INVALID_COOKIE;}return S_OK;
  }
  HRESULT STDMETHODCALLTYPE OnSetFocus(BOOL f)override{if(!f)Blur();return S_OK;}
  HRESULT STDMETHODCALLTYPE OnTestKeyDown(ITfContext* c,WPARAM k,LPARAM,BOOL* eaten)override{if(shiftDown&&!Shift(k))shiftUsed=true;*eaten=Eats(k);if(!*eaten&&Mode()==2&&!code.empty()){HRESULT hr=Flush(c,true);if(FAILED(hr)){*eaten=TRUE;return hr;}}return S_OK;}
  HRESULT STDMETHODCALLTYPE OnTestKeyUp(ITfContext*,WPARAM k,LPARAM,BOOL* eaten)override{*eaten=Shift(k)&&shiftDown;return S_OK;}
  HRESULT STDMETHODCALLTYPE OnKeyUp(ITfContext* c,WPARAM k,LPARAM,BOOL* eaten)override{
    *eaten=Shift(k)&&shiftDown;if(!*eaten)return S_OK;
    bool toggle=!shiftUsed&&State(VK_CONTROL)>=0&&State(VK_MENU)>=0&&State(VK_LWIN)>=0&&State(VK_RWIN)>=0;
    shiftDown=false;if(toggle){HRESULT hr=Flush(c,true);if(FAILED(hr))return hr;SetMode(Mode()==2?1:2);}return S_OK;
  }
  HRESULT STDMETHODCALLTYPE OnPreservedKey(ITfContext*,REFGUID,BOOL* eaten)override{*eaten=FALSE;return S_OK;}
  HRESULT STDMETHODCALLTYPE OnKeyDown(ITfContext* context,WPARAM key,LPARAM flags,BOOL* eaten)override{
    if(shiftDown&&!Shift(key))shiftUsed=true;
    if(Shift(key)){
      *eaten=Eats(key);if(*eaten&&!shiftDown&&!(flags&(1LL<<30))){shiftDown=true;shiftUsed=false;}return S_OK;
    }
    *eaten=Eats(key);if(!*eaten){if(Mode()==2&&!code.empty())return Flush(context,true);return S_OK;}
    if(Mode()==2){
      if(key>='A'&&key<='Z'){bool upper=(State(VK_SHIFT)<0)!=((State(VK_CAPITAL)&1)!=0);edit.Insert((char)(upper?key:key-'A'+'a'));Refresh(context);return S_OK;}
      if(key==VK_SPACE){HRESULT hr=Insert(context,client,std::wstring(code.begin(),code.end())+L" ");if(SUCCEEDED(hr))Clear();return hr;}
      if(key==VK_TAB)return Flush(context,false);
    }
    if(State(VK_CONTROL)<0){edit.Undo(key=='Y'||State(VK_SHIFT)<0);Refresh(context);return S_OK;}
    if(key==VK_LEFT||key==VK_RIGHT||key==VK_HOME||key==VK_END){
      edit.Move(key==VK_HOME?0:key==VK_END?code.size():key==VK_LEFT?(edit.caret?edit.caret-1:0):edit.caret+1);
      view.Show(context,client,code,rows,selection,edit.caret,Mode()==2);return S_OK;
    }
    if(!code.empty()&&key=='2'&&State(VK_SHIFT)<0){edit.Insert('@');Refresh(context);return S_OK;}
    if(key>='A'&&key<='Z'&&(State(VK_SHIFT)<0||State(VK_CAPITAL)&1)){HRESULT hr=Flush(context,true);if(FAILED(hr))return hr;return Insert(context,client,std::wstring(1,(wchar_t)key));}
    if(key==VK_ESCAPE){Clear();return S_OK;}
    if(key==VK_BACK){edit.Back();}
    else if(key==VK_DELETE){edit.Delete();}
    else if(key==VK_OEM_PERIOD||key==VK_OEM_2||key=='0'||(raw&&key>='1'&&key<='9')){
      edit.Insert((char)(key==VK_OEM_PERIOD?'.':key==VK_OEM_2?'/':key));
    }
    else if(key==VK_PRIOR||key==VK_NEXT||key==VK_OEM_MINUS||key==VK_OEM_PLUS){ChangePage(key==VK_PRIOR||key==VK_OEM_MINUS?-1:1);return S_OK;}
    else if(key==VK_UP||key==VK_DOWN){if(!rows.empty())selection=(selection+rows.size()+(key==VK_UP?-1:1))%rows.size();view.Show(context,client,code,rows,selection,edit.caret,Mode()==2);return S_OK;}
    else if(key==VK_SPACE||key==VK_RETURN||(key>='1'&&key<='9')){
      unsigned index=key>='1'&&key<='9'?CandidatePaging::digit(selection,(unsigned)(key-'1')):selection;if(index>=rows.size())return S_OK;
      HRESULT hr=Insert(context,client,rows[index].text);if(SUCCEEDED(hr)){Learn(rows[index].text);Clear();}return hr;
    }else {edit.Insert((char)(key-'A'+'a'));}
    Refresh(context);return S_OK;
  }
  HRESULT STDMETHODCALLTYPE OnInitDocumentMgr(ITfDocumentMgr*)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE OnUninitDocumentMgr(ITfDocumentMgr*)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE OnSetFocus(ITfDocumentMgr*,ITfDocumentMgr*)override{Blur();return S_OK;}
  HRESULT STDMETHODCALLTYPE OnPushContext(ITfContext*)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE OnPopContext(ITfContext*)override{Blur();return S_OK;}
  HRESULT Probe(UINT key,bool up=false,LPARAM flags=0){
    if(!manager)return E_PENDING;ITfDocumentMgr* doc=nullptr;ITfContext* c=nullptr;
    HRESULT hr=manager->GetFocus(&doc);if(SUCCEEDED(hr)&&doc)hr=doc->GetTop(&c);
#ifdef WININTEL_TEST_STORE
    if(c)c->Release();c=TestContext();hr=c?S_OK:E_PENDING;
#endif
    if(!c){if(doc)doc->Release();return E_PENDING;}BOOL eaten=FALSE;probeKey=true;hr=up?OnKeyUp(c,key,flags,&eaten):OnKeyDown(c,key,flags,&eaten);probeKey=false;c->Release();if(doc)doc->Release();return SUCCEEDED(hr)&&!eaten?S_FALSE:hr;
  }
  int ProbeMode(int value){if(value>=0)SetMode(value);return Mode();}
#ifdef WININTEL_TEST_STORE
  void ProbeControl(bool value){probeControl=value;}
#endif
  // The probe dispatches keys directly and must not claim the real keyboard sink.
  HRESULT AttachProbe(ITfThreadMgr* m,TfClientId id){
    if(manager)return S_OK;if(!engine.open(Root()+L"\\dictionary.bin"))return E_FAIL;english.open(Root());
    manager=m;manager->AddRef();client=id;
    ITfSource* source=nullptr;if(SUCCEEDED(m->QueryInterface(IID_ITfSource,(void**)&source))){source->AdviseSink(IID_ITfThreadMgrEventSink,(ITfThreadMgrEventSink*)this,&focusCookie);source->Release();}
    active=this;StartBridge();return S_OK;
  }
};
HRESULT CreateService(REFIID i,void** p){auto service=new Keyboard;HRESULT hr=service->QueryInterface(i,p);service->Release();return hr;}
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestKey(UINT key){return active?active->Probe(key):E_PENDING;}
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestKeyUp(UINT key){return active?active->Probe(key,true):E_PENDING;}
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestRepeat(UINT key){return active?active->Probe(key,false,1LL<<30):E_PENDING;}
extern "C" __declspec(dllexport) int WINAPI WinintelTestMode(int value){return active?active->ProbeMode(value):-1;}
static ITfThreadMgr* probeManager=nullptr;static ITfTextInputProcessor* probeService=nullptr;
static TfClientId probeClient=TF_CLIENTID_NULL;
#ifdef WININTEL_TEST_STORE
#include "../Verification/TextStoreProbe.h"
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestControl(UINT down){if(!active)return E_PENDING;active->ProbeControl(down!=0);return S_OK;}
static ITfDocumentMgr* probeDocument=nullptr;
static ITfContext* TestContext(){ITfContext* c=nullptr;if(probeDocument)probeDocument->GetTop(&c);return c;}
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestFocus(HWND window){
  DWORD owner=0;GetWindowThreadProcessId(window,&owner);
  if(owner!=GetCurrentProcessId()||!probeManager)return E_INVALIDARG;
  if(probeDocument){probeDocument->Pop(TF_POPF_ALL);probeDocument->Release();probeDocument=nullptr;}
  HRESULT hr=probeManager->CreateDocumentMgr(&probeDocument);if(FAILED(hr))return hr;
  auto store=new ProbeTextStore(window);TfEditCookie cookie;ITfContext* context=nullptr;
  hr=probeDocument->CreateContext(probeClient,0,store,&context,&cookie);store->Release();
  if(SUCCEEDED(hr)){hr=probeDocument->Push(context);context->Release();}
  if(SUCCEEDED(hr))hr=probeManager->SetFocus(probeDocument);
  return hr;
}
#endif
extern "C" __declspec(dllexport) HRESULT WINAPI WinintelTestAttach(){
  if(active)return S_OK;
  HRESULT hr=CoCreateInstance(CLSID_TF_ThreadMgr,nullptr,CLSCTX_INPROC_SERVER,IID_ITfThreadMgr,(void**)&probeManager);if(FAILED(hr))return hr;
  TfClientId id;hr=probeManager->Activate(&id);if(FAILED(hr))return hr;
  probeClient=id;
  auto service=new Keyboard;probeService=service;
  return service->AttachProbe(probeManager,id);
}
extern "C" __declspec(dllexport) void WINAPI WinintelTestDetach(){
  if(probeService){probeService->Deactivate();probeService->Release();probeService=nullptr;}
#ifdef WININTEL_TEST_STORE
  if(probeDocument){probeDocument->Pop(TF_POPF_ALL);probeDocument->Release();probeDocument=nullptr;}
#endif
  if(probeManager){probeManager->Deactivate();probeManager->Release();probeManager=nullptr;}
}
