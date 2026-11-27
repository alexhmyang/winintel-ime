// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
#include "Core.h"
#include <windowsx.h>
namespace {
int Scale(int value,int dpi){return MulDiv(value,dpi,96);}
HFONT Font(int size,int dpi,int weight=FW_NORMAL){return CreateFontW(-Scale(size,dpi),0,0,0,weight,0,0,0,DEFAULT_CHARSET,0,0,CLEARTYPE_QUALITY,0,L"Microsoft YaHei UI");}
void FillRound(HDC dc,const RECT& r,COLORREF color,int radius){
  HBRUSH brush=CreateSolidBrush(color);auto oldBrush=SelectObject(dc,brush);auto oldPen=SelectObject(dc,GetStockObject(NULL_PEN));
  RoundRect(dc,r.left,r.top,r.right,r.bottom,radius,radius);
  SelectObject(dc,oldPen);SelectObject(dc,oldBrush);DeleteObject(brush);
}
// Ask the host's TSF text store for the actual insertion rectangle.
class CaretSession:public ITfEditSession {
  long refs=1;ITfContext* context;
public:
  RECT rect={};bool valid=false;
  explicit CaretSession(ITfContext* c):context(c){context->AddRef();}
  ~CaretSession(){context->Release();}
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id,void** out)override{if(!out)return E_POINTER;*out=nullptr;if(id!=IID_IUnknown&&id!=IID_ITfEditSession)return E_NOINTERFACE;*out=(ITfEditSession*)this;AddRef();return S_OK;}
  ULONG STDMETHODCALLTYPE AddRef()override{return InterlockedIncrement(&refs);}
  ULONG STDMETHODCALLTYPE Release()override{auto n=InterlockedDecrement(&refs);if(!n)delete this;return n;}
  HRESULT STDMETHODCALLTYPE DoEditSession(TfEditCookie cookie)override{
    TF_SELECTION selection={};ULONG fetched=0;ITfContextView* view=nullptr;BOOL clipped=FALSE;
    HRESULT hr=context->GetSelection(cookie,TF_DEFAULT_SELECTION,1,&selection,&fetched);
    if(SUCCEEDED(hr)&&fetched&&selection.range){
      selection.range->Collapse(cookie,TF_ANCHOR_END);
      if(SUCCEEDED(context->GetActiveView(&view))&&view){hr=view->GetTextExt(cookie,selection.range,&rect,&clipped);valid=SUCCEEDED(hr)&&!clipped&&rect.bottom>rect.top;view->Release();}
      selection.range->Release();
    }
    return valid?S_OK:E_FAIL;
  }
};
bool Anchor(ITfContext* context,TfClientId client,RECT& rect){
  auto session=new CaretSession(context);HRESULT result=E_FAIL;
  HRESULT hr=context->RequestEditSession(client,session,TF_ES_SYNC|TF_ES_READ,&result);
  bool valid=SUCCEEDED(hr)&&SUCCEEDED(result)&&session->valid;if(valid)rect=session->rect;session->Release();
  if(valid)return true;
  GUITHREADINFO gi={sizeof(gi)};
  if(GetGUIThreadInfo(0,&gi)&&gi.hwndCaret){rect=gi.rcCaret;POINT p={rect.left,rect.top};ClientToScreen(gi.hwndCaret,&p);OffsetRect(&rect,p.x-rect.left,p.y-rect.top);return true;}
  // Some applications don't expose a caret. Keep the fallback inside that app.
  if(gi.hwndFocus&&GetWindowRect(gi.hwndFocus,&rect)){rect.right=rect.left+1;rect.top+=8;rect.bottom=rect.top+20;return true;}
  return false;
}
}
LRESULT CALLBACK CandidateView::Procedure(HWND h,UINT m,WPARAM w,LPARAM l) {
  auto self=(CandidateView*)GetWindowLongPtrW(h,GWLP_USERDATA);
  if(m==WM_NCCREATE){self=(CandidateView*)((CREATESTRUCTW*)l)->lpCreateParams;SetWindowLongPtrW(h,GWLP_USERDATA,(LONG_PTR)self);}
  if(m==WM_MOUSEACTIVATE)return MA_NOACTIVATE;
  if(m==WM_MOUSEMOVE&&self){
    POINT point={GET_X_LPARAM(l),GET_Y_LPARAM(l)};int hit=-1;
    for(unsigned i=0;i<self->cells.size();i++)if(PtInRect(&self->cells[i],point)){hit=(int)i;break;}
    if(hit!=self->hover){self->hover=hit;InvalidateRect(h,nullptr,FALSE);}
    TRACKMOUSEEVENT track={sizeof(track),TME_LEAVE,h,0};TrackMouseEvent(&track);return 0;
  }
  if(m==WM_MOUSELEAVE&&self){self->hover=-1;InvalidateRect(h,nullptr,FALSE);return 0;}
  if(m==WM_LBUTTONUP&&self){
    POINT point={GET_X_LPARAM(l),GET_Y_LPARAM(l)};
    if(PtInRect(&self->header,point)&&self->MoveCaret&&!self->advances.empty()){
      int x=point.x-self->header.left+self->scroll;unsigned position=0;
      while(position+1<self->advances.size()&&x>(self->advances[position]+self->advances[position+1])/2)position++;
      self->MoveCaret(position);return 0;
    }
    for(unsigned i=0;i<self->cells.size();i++)if(PtInRect(&self->cells[i],point)){if(self->Choose)self->Choose(i);return 0;}
    if(self->Page){if(PtInRect(&self->previous,point)&&self->page>0)self->Page(-1);else if(PtInRect(&self->next,point)&&self->page+1<self->pages)self->Page(1);}
    return 0;
  }
  if(m==WM_MOUSEWHEEL&&self&&self->Page){self->Page(GET_WHEEL_DELTA_WPARAM(w)>0?-1:1);return 0;}
  if(m==WM_PAINT&&self){
    PAINTSTRUCT ps;HDC target=BeginPaint(h,&ps);RECT r;GetClientRect(h,&r);
    HDC dc=CreateCompatibleDC(target);HBITMAP bitmap=CreateCompatibleBitmap(target,r.right,r.bottom);auto oldBitmap=SelectObject(dc,bitmap);
    HBRUSH bg=CreateSolidBrush(RGB(255,255,255));FillRect(dc,&r,bg);DeleteObject(bg);
    HFONT font=Font(20,self->dpi);
    auto old=SelectObject(dc,font);SetTextColor(dc,RGB(26,115,232));SetBkMode(dc,TRANSPARENT);
    int pad=Scale(12,self->dpi);
    self->header={pad,Scale(5,self->dpi),r.right-Scale(112,self->dpi),Scale(33,self->dpi)};
    self->advances.assign(self->content.size()+1,0);
    for(size_t i=1;i<self->advances.size();i++){SIZE size={};GetTextExtentPoint32W(dc,self->content.c_str(),(int)i,&size);self->advances[i]=size.cx;}
    int caretX=self->advances[self->caret],available=self->header.right-self->header.left-Scale(4,self->dpi);
    self->scroll=std::max(0,std::min(self->scroll,std::max(0,self->advances.back()-available)));
    if(caretX<self->scroll)self->scroll=caretX;
    if(caretX>self->scroll+available)self->scroll=caretX-available;
    int saved=SaveDC(dc);IntersectClipRect(dc,self->header.left,self->header.top,self->header.right,self->header.bottom);
    TEXTMETRICW metrics={};GetTextMetricsW(dc,&metrics);int top=self->header.top+(self->header.bottom-self->header.top-metrics.tmHeight)/2;
    TextOutW(dc,self->header.left-self->scroll,top,self->content.c_str(),(int)self->content.size());
    RECT cursor={self->header.left+caretX-self->scroll,top,self->header.left+caretX-self->scroll+Scale(2,self->dpi),top+metrics.tmHeight};
    HBRUSH blue=CreateSolidBrush(RGB(26,115,232));FillRect(dc,&cursor,blue);DeleteObject(blue);RestoreDC(dc,saved);
    HFONT fontSmall=Font(12,self->dpi);SelectObject(dc,fontSmall);SetTextColor(dc,RGB(95,99,104));
    RECT brand={r.right-Scale(104,self->dpi),Scale(5,self->dpi),r.right-pad,Scale(33,self->dpi)};
    DrawTextW(dc,L"数萌 AI",-1,&brand,DT_RIGHT|DT_SINGLELINE|DT_VCENTER|DT_NOPREFIX);
    SelectObject(dc,font);
    for(unsigned i=0;i<self->visible.size();i++){
      RECT line=self->cells[i];
      if(i==self->selected||i==(unsigned)self->hover)FillRound(dc,line,i==self->selected?RGB(0,122,78):RGB(227,243,234),Scale(12,self->dpi));
      RECT number=line;number.left+=Scale(9,self->dpi);number.right=number.left+Scale(17,self->dpi);
      SelectObject(dc,fontSmall);SetTextColor(dc,i==self->selected?RGB(255,255,255):RGB(128,134,139));
      auto digit=self->english?L"·":std::to_wstring(i+1);DrawTextW(dc,digit.c_str(),-1,&number,DT_LEFT|DT_SINGLELINE|DT_VCENTER|DT_NOPREFIX);
      line.left=number.right+Scale(3,self->dpi);line.right-=Scale(9,self->dpi);
      SelectObject(dc,font);SetTextColor(dc,i==self->selected?RGB(255,255,255):RGB(32,33,36));
      DrawTextW(dc,self->visible[i].c_str(),-1,&line,DT_LEFT|DT_SINGLELINE|DT_VCENTER|DT_END_ELLIPSIS|DT_NOPREFIX);
    }
    SelectObject(dc,fontSmall);SetTextColor(dc,RGB(128,134,139));
    RECT footer={pad,r.bottom-Scale(29,self->dpi),r.right-Scale(120,self->dpi),r.bottom-Scale(3,self->dpi)};
    auto hint=self->english?std::wstring(L"↑ ↓ 选择 · Tab 补全 · 空格原词"):L"← → 修改 · "+std::to_wstring(self->caret)+L"/"+std::to_wstring(self->content.size());
    DrawTextW(dc,hint.c_str(),-1,&footer,DT_LEFT|DT_SINGLELINE|DT_VCENTER|DT_NOPREFIX);
    auto page=std::to_wstring(self->page+1)+L" / "+std::to_wstring(self->pages);
    RECT pageRect={r.right-Scale(112,self->dpi),footer.top,r.right-Scale(70,self->dpi),footer.bottom};
    DrawTextW(dc,page.c_str(),-1,&pageRect,DT_RIGHT|DT_SINGLELINE|DT_VCENTER|DT_NOPREFIX);
    SetTextColor(dc,self->page?RGB(95,99,104):RGB(218,220,224));DrawTextW(dc,L"‹",-1,&self->previous,DT_CENTER|DT_SINGLELINE|DT_VCENTER);
    SetTextColor(dc,self->page+1<self->pages?RGB(95,99,104):RGB(218,220,224));DrawTextW(dc,L"›",-1,&self->next,DT_CENTER|DT_SINGLELINE|DT_VCENTER);
    SelectObject(dc,old);DeleteObject(font);DeleteObject(fontSmall);BitBlt(target,0,0,r.right,r.bottom,dc,0,0,SRCCOPY);
    SelectObject(dc,oldBitmap);DeleteObject(bitmap);DeleteDC(dc);EndPaint(h,&ps);return 0;
  }
  if(m==WM_ERASEBKGND)return 1;
  return DefWindowProcW(h,m,w,l);
}
CandidateView::~CandidateView(){if(window)DestroyWindow(window);}
void CandidateView::Hide(){if(window)ShowWindow(window,SW_HIDE);}
void CandidateView::Show(ITfContext* context,TfClientId client,const std::string& code,const std::vector<Candidate>& rows,unsigned selected,size_t position,bool english){
  if(rows.empty()||code.empty()){Hide();return;}
  RECT anchor={};if(!Anchor(context,client,anchor)){Hide();return;}
  if(!window){WNDCLASSW wc={};wc.style=CS_DROPSHADOW;wc.lpfnWndProc=Procedure;wc.hInstance=module;wc.lpszClassName=L"winintel.Candidates.052";wc.hCursor=LoadCursorW(nullptr,MAKEINTRESOURCEW(32512));RegisterClassW(&wc);
    window=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE|WS_EX_TOPMOST,wc.lpszClassName,L"数萌 AI",WS_POPUP,0,0,520,110,nullptr,nullptr,module,this);}
  this->english=english;content=std::wstring(code.begin(),code.end());caret=(unsigned)std::min(position,code.size());hover=-1;
  unsigned start=CandidatePaging::start(selected);this->selected=selected-start;
  page=start/CandidatePaging::size;pages=CandidatePaging::pages((unsigned)rows.size());visible.clear();
  for(unsigned i=start;i<std::min<unsigned>((unsigned)rows.size(),start+CandidatePaging::size);i++)visible.push_back(rows[i].text);
  POINT p={anchor.left,anchor.bottom+6};
  MONITORINFO monitor={sizeof(monitor)};GetMonitorInfoW(MonitorFromPoint(p,MONITOR_DEFAULTTONEAREST),&monitor);RECT work=monitor.rcWork;
  typedef UINT(WINAPI* DpiFunction)(HWND);auto getDpi=(DpiFunction)GetProcAddress(GetModuleHandleW(L"user32.dll"),"GetDpiForWindow");
  GUITHREADINFO gi={sizeof(gi)};GetGUIThreadInfo(0,&gi);dpi=getDpi&&gi.hwndFocus?(int)getDpi(gi.hwndFocus):96;if(!dpi)dpi=96;
  HDC dc=GetDC(window);HFONT font=Font(20,dpi);auto old=SelectObject(dc,font);
  int pad=Scale(8,dpi),gap=Scale(5,dpi),cellHeight=Scale(44,dpi),limit=std::min<int>(Scale(740,dpi),work.right-work.left),width=Scale(380,dpi);
  std::vector<int> widths;int natural=pad*2;
  for(auto& text:visible){SIZE size={};GetTextExtentPoint32W(dc,text.c_str(),(int)text.size(),&size);int w=size.cx+Scale(42,dpi);widths.push_back(w);natural+=w+gap;}
  width=std::min(limit,std::max(width,natural-gap));
  int available=width-2*pad;
  // At least three cells fit each row, so all nine stay within three rows.
  int maxCell=(available-2*gap)/3;
  int x=pad,y=Scale(36,dpi);cells.clear();
  for(int w:widths){w=std::min(w,maxCell);if(x+w>width-pad){x=pad;y+=cellHeight+gap;}cells.push_back({x,y,x+w,y+cellHeight});x+=w+gap;}
  SelectObject(dc,old);DeleteObject(font);ReleaseDC(window,dc);
  int height=y+cellHeight+Scale(33,dpi);
  previous={width-Scale(62,dpi),height-Scale(29,dpi),width-Scale(34,dpi),height-Scale(3,dpi)};
  next={width-Scale(34,dpi),height-Scale(29,dpi),width-Scale(6,dpi),height-Scale(3,dpi)};
  p.y=anchor.bottom+Scale(6,dpi);
  if(p.y+height>work.bottom)p.y=anchor.top-height-Scale(6,dpi);
  p.x=std::max(work.left,std::min(p.x,work.right-width));p.y=std::max(work.top,std::min(p.y,work.bottom-height));
  SetWindowRgn(window,CreateRoundRectRgn(0,0,width+1,height+1,Scale(16,dpi),Scale(16,dpi)),FALSE);
  SetWindowPos(window,HWND_TOPMOST,p.x,p.y,width,height,SWP_NOACTIVATE|SWP_SHOWWINDOW);InvalidateRect(window,nullptr,TRUE);
}
class WriteSession : public ITfEditSession {
  long refs=1;ITfContext* context;std::wstring text;
public:
  WriteSession(ITfContext* c,std::wstring t):context(c),text(std::move(t)){context->AddRef();}
  ~WriteSession(){context->Release();}
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID i,void** p)override{*p=nullptr;if(i==IID_IUnknown||i==IID_ITfEditSession){*p=this;AddRef();return S_OK;}return E_NOINTERFACE;}
  ULONG STDMETHODCALLTYPE AddRef()override{return InterlockedIncrement(&refs);}
  ULONG STDMETHODCALLTYPE Release()override{auto r=InterlockedDecrement(&refs);if(!r)delete this;return r;}
  HRESULT STDMETHODCALLTYPE DoEditSession(TfEditCookie cookie)override{
    ITfInsertAtSelection* insert=nullptr;HRESULT hr=context->QueryInterface(IID_ITfInsertAtSelection,(void**)&insert);if(FAILED(hr))return hr;
    ITfRange* range=nullptr;hr=insert->InsertTextAtSelection(cookie,0,text.c_str(),(LONG)text.size(),&range);
    if(SUCCEEDED(hr)&&range){range->Collapse(cookie,TF_ANCHOR_END);TF_SELECTION s={range,{TF_AE_NONE,FALSE}};hr=context->SetSelection(cookie,1,&s);}
    if(range)range->Release();insert->Release();return hr;
  }
};
HRESULT Insert(ITfContext* c,TfClientId id,const std::wstring& text){
  auto edit=new WriteSession(c,text);HRESULT result=E_FAIL;
  HRESULT hr=c->RequestEditSession(id,edit,TF_ES_SYNC|TF_ES_READWRITE,&result);edit->Release();return FAILED(hr)?hr:result;
}
