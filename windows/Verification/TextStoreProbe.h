// SPDX-License-Identifier: GPL-3.0-or-later
// Test-only TSF text store, bound strictly to a textbox in this process.
#pragma once
#include <textstor.h>
#include <windowsx.h>
class ProbeTextStore : public ITextStoreACP {
  long refs=1;HWND window;ITextStoreACPSink* sink=nullptr;bool locked=false;
  std::wstring Text(){int count=GetWindowTextLengthW(window);std::wstring text(count+1,0);GetWindowTextW(window,&text[0],count+1);text.resize(count);return text;}
public:
  explicit ProbeTextStore(HWND value):window(value){}
  ~ProbeTextStore(){if(sink)sink->Release();}
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id,void** out)override{if(!out)return E_POINTER;*out=nullptr;if(id!=IID_IUnknown&&id!=IID_ITextStoreACP)return E_NOINTERFACE;*out=(ITextStoreACP*)this;AddRef();return S_OK;}
  ULONG STDMETHODCALLTYPE AddRef()override{return InterlockedIncrement(&refs);}
  ULONG STDMETHODCALLTYPE Release()override{auto value=InterlockedDecrement(&refs);if(!value)delete this;return value;}
  HRESULT STDMETHODCALLTYPE AdviseSink(REFIID id,IUnknown* value,DWORD)override{if(id!=IID_ITextStoreACPSink)return E_NOINTERFACE;if(sink)return E_UNEXPECTED;return value->QueryInterface(id,(void**)&sink);}
  HRESULT STDMETHODCALLTYPE UnadviseSink(IUnknown*)override{if(sink){sink->Release();sink=nullptr;}return S_OK;}
  HRESULT STDMETHODCALLTYPE RequestLock(DWORD,HRESULT* result)override{if(!result||!sink)return E_UNEXPECTED;if(locked){*result=TS_E_SYNCHRONOUS;return S_OK;}locked=true;*result=sink->OnLockGranted(TS_LF_READWRITE);locked=false;return S_OK;}
  HRESULT STDMETHODCALLTYPE GetStatus(TS_STATUS* status)override{status->dwDynamicFlags=0;status->dwStaticFlags=TS_SS_NOHIDDENTEXT;return S_OK;}
  HRESULT STDMETHODCALLTYPE QueryInsert(LONG start,LONG end,ULONG,LONG* outStart,LONG* outEnd)override{*outStart=start;*outEnd=end;return S_OK;}
  HRESULT STDMETHODCALLTYPE GetSelection(ULONG,ULONG count,TS_SELECTION_ACP* selection,ULONG* fetched)override{
    *fetched=0;if(!count)return S_OK;DWORD start,end;SendMessageW(window,EM_GETSEL,(WPARAM)&start,(LPARAM)&end);
    selection[0]={LONG(start),LONG(end),{TS_AE_END,FALSE}};*fetched=1;return S_OK;
  }
  HRESULT STDMETHODCALLTYPE SetSelection(ULONG count,const TS_SELECTION_ACP* selection)override{if(count)SendMessageW(window,EM_SETSEL,selection[0].acpStart,selection[0].acpEnd);return S_OK;}
  HRESULT STDMETHODCALLTYPE GetText(LONG start,LONG end,WCHAR* plain,ULONG requested,ULONG* returned,TS_RUNINFO* runs,ULONG runCount,ULONG* runReturned,LONG* next)override{
    auto text=Text();if(end<0)end=(LONG)text.size();if(start<0||end<start||end>(LONG)text.size())return TS_E_INVALIDPOS;
    ULONG copied=std::min<ULONG>(requested,end-start);if(copied)memcpy(plain,text.data()+start,copied*sizeof(wchar_t));
    *returned=copied;*runReturned=0;if(runCount&&copied){runs[0]={copied,TS_RT_PLAIN};*runReturned=1;}*next=start+copied;return S_OK;
  }
  HRESULT STDMETHODCALLTYPE SetText(DWORD,LONG start,LONG end,const WCHAR* value,ULONG count,TS_TEXTCHANGE* change)override{
    auto text=Text();if(start<0||end<start||end>(LONG)text.size())return TS_E_INVALIDPOS;
    text.replace(start,end-start,value,count);SetWindowTextW(window,text.c_str());SendMessageW(window,EM_SETSEL,start+count,start+count);
    *change={start,end,start+(LONG)count};return S_OK;
  }
  HRESULT STDMETHODCALLTYPE GetFormattedText(LONG,LONG,IDataObject**)override{return E_NOTIMPL;}
  HRESULT STDMETHODCALLTYPE GetEmbedded(LONG,REFGUID,REFIID,IUnknown**)override{return E_NOTIMPL;}
  HRESULT STDMETHODCALLTYPE QueryInsertEmbedded(const GUID*,const FORMATETC*,BOOL* insertable)override{*insertable=FALSE;return S_OK;}
  HRESULT STDMETHODCALLTYPE InsertEmbedded(DWORD,LONG,LONG,IDataObject*,TS_TEXTCHANGE*)override{return E_NOTIMPL;}
  HRESULT STDMETHODCALLTYPE InsertTextAtSelection(DWORD flags,const WCHAR* text,ULONG count,LONG* start,LONG* end,TS_TEXTCHANGE* change)override{
    TS_SELECTION_ACP selection;ULONG fetched;GetSelection(0,1,&selection,&fetched);
    if(start)*start=selection.acpStart;if(end)*end=selection.acpStart+(LONG)count;
    if(flags&TS_IAS_QUERYONLY)return S_OK;
    return SetText(0,selection.acpStart,selection.acpEnd,text,count,change);
  }
  HRESULT STDMETHODCALLTYPE InsertEmbeddedAtSelection(DWORD,IDataObject*,LONG*,LONG*,TS_TEXTCHANGE*)override{return E_NOTIMPL;}
  HRESULT STDMETHODCALLTYPE RequestSupportedAttrs(DWORD,ULONG,const TS_ATTRID*)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE RequestAttrsAtPosition(LONG,ULONG,const TS_ATTRID*,DWORD)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE RequestAttrsTransitioningAtPosition(LONG,ULONG,const TS_ATTRID*,DWORD)override{return S_OK;}
  HRESULT STDMETHODCALLTYPE FindNextAttrTransition(LONG,LONG halt,ULONG,const TS_ATTRID*,DWORD,LONG* next,BOOL* found,LONG* offset)override{*next=halt;*found=FALSE;*offset=0;return S_OK;}
  HRESULT STDMETHODCALLTYPE RetrieveRequestedAttrs(ULONG,TS_ATTRVAL*,ULONG* fetched)override{*fetched=0;return S_OK;}
  HRESULT STDMETHODCALLTYPE GetEndACP(LONG* end)override{*end=GetWindowTextLengthW(window);return S_OK;}
  HRESULT STDMETHODCALLTYPE GetActiveView(TsViewCookie* view)override{*view=1;return S_OK;}
  HRESULT STDMETHODCALLTYPE GetACPFromPoint(TsViewCookie,const POINT*,DWORD,LONG* position)override{*position=0;return S_OK;}
  HRESULT STDMETHODCALLTYPE GetTextExt(TsViewCookie,LONG start,LONG,RECT* rectangle,BOOL* clipped)override{
    LRESULT position=SendMessageW(window,EM_POSFROMCHAR,start,0);
    POINT point={position==-1?3:GET_X_LPARAM(position),position==-1?3:GET_Y_LPARAM(position)};ClientToScreen(window,&point);
    HDC dc=GetDC(window);auto old=SelectObject(dc,(HFONT)SendMessageW(window,WM_GETFONT,0,0));TEXTMETRICW metrics={};GetTextMetricsW(dc,&metrics);SelectObject(dc,old);ReleaseDC(window,dc);
    *rectangle={point.x,point.y,point.x+1,point.y+metrics.tmHeight};*clipped=FALSE;return S_OK;
  }
  HRESULT STDMETHODCALLTYPE GetScreenExt(TsViewCookie,RECT* rectangle)override{GetWindowRect(window,rectangle);return S_OK;}
  HRESULT STDMETHODCALLTYPE GetWnd(TsViewCookie,HWND* out)override{*out=window;return S_OK;}
};
