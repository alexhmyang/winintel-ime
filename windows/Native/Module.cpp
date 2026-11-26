// SPDX-License-Identifier: GPL-3.0-or-later
#include "Core.h"
HMODULE module=nullptr;long objects=0;
const CLSID ServiceId={0x1b6f3bd2,0x51a0,0x4935,{0xb2,0xf9,0x31,0xca,0xf0,0x34,0x5e,0x71}};
const GUID ProfileId={0xdf4864ae,0x309a,0x4fa1,{0x9a,0x71,0x37,0xfa,0xc4,0x32,0x89,0x23}};
std::wstring Root(){wchar_t path[MAX_PATH];GetModuleFileNameW(module,path,MAX_PATH);std::wstring root=path;root.resize(root.find_last_of(L"\\/"));root.resize(root.find_last_of(L"\\/"));return root;}
class Factory:public IClassFactory {
  long refs=1;
public:
  Factory(){InterlockedIncrement(&objects);}~Factory(){InterlockedDecrement(&objects);}
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID i,void** p)override{*p=nullptr;if(i!=IID_IUnknown&&i!=IID_IClassFactory)return E_NOINTERFACE;*p=this;AddRef();return S_OK;}
  ULONG STDMETHODCALLTYPE AddRef()override{return InterlockedIncrement(&refs);}
  ULONG STDMETHODCALLTYPE Release()override{auto r=InterlockedDecrement(&refs);if(!r)delete this;return r;}
  HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer,REFIID i,void** p)override{return outer?CLASS_E_NOAGGREGATION:CreateService(i,p);}
  HRESULT STDMETHODCALLTYPE LockServer(BOOL lock)override{if(lock)InterlockedIncrement(&objects);else InterlockedDecrement(&objects);return S_OK;}
};
BOOL WINAPI DllMain(HINSTANCE h,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){module=h;DisableThreadLibraryCalls(h);}return TRUE;}
extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID c,REFIID i,void** p){if(c!=ServiceId)return CLASS_E_CLASSNOTAVAILABLE;auto f=new Factory;auto hr=f->QueryInterface(i,p);f->Release();return hr;}
extern "C" HRESULT WINAPI DllCanUnloadNow(){return objects?S_FALSE:S_OK;}
static const wchar_t* key=L"Software\\Classes\\CLSID\\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}";
extern "C" HRESULT WINAPI DllRegisterServer(){
  wchar_t path[MAX_PATH];GetModuleFileNameW(module,path,MAX_PATH);HKEY h;
  RegDeleteTreeW(HKEY_CURRENT_USER,key);
  LONG err=RegCreateKeyExW(HKEY_LOCAL_MACHINE,(std::wstring(key)+L"\\InprocServer32").c_str(),0,nullptr,0,KEY_WRITE,nullptr,&h,nullptr);if(err)return HRESULT_FROM_WIN32(err);
  RegSetValueExW(h,nullptr,0,REG_SZ,(BYTE*)path,(DWORD)((wcslen(path)+1)*2));const wchar_t* model=L"Apartment";
  RegSetValueExW(h,L"ThreadingModel",0,REG_SZ,(BYTE*)model,20);RegCloseKey(h);
  CoInitialize(nullptr);ITfInputProcessorProfiles* profiles=nullptr;HRESULT hr=CoCreateInstance(CLSID_TF_InputProcessorProfiles,nullptr,CLSCTX_INPROC_SERVER,IID_ITfInputProcessorProfiles,(void**)&profiles);
  if(SUCCEEDED(hr)){hr=profiles->Register(ServiceId);if(SUCCEEDED(hr))hr=profiles->AddLanguageProfile(ServiceId,0x804,ProfileId,L"数萌 AI 输入法",9,path,(ULONG)wcslen(path),0);if(SUCCEEDED(hr))profiles->EnableLanguageProfile(ServiceId,0x804,ProfileId,TRUE);profiles->Release();}
  ITfCategoryMgr* category=nullptr;if(SUCCEEDED(CoCreateInstance(CLSID_TF_CategoryMgr,nullptr,CLSCTX_INPROC_SERVER,IID_ITfCategoryMgr,(void**)&category))){const GUID* cats[]={&GUID_TFCAT_TIP_KEYBOARD,&GUID_TFCAT_TIPCAP_IMMERSIVESUPPORT,&GUID_TFCAT_TIPCAP_SYSTRAYSUPPORT};for(auto c:cats)category->RegisterCategory(ServiceId,*c,ServiceId);category->Release();}
  CoUninitialize();return hr;
}
extern "C" HRESULT WINAPI DllUnregisterServer(){
  CoInitialize(nullptr);ITfInputProcessorProfiles* p=nullptr;if(SUCCEEDED(CoCreateInstance(CLSID_TF_InputProcessorProfiles,nullptr,CLSCTX_INPROC_SERVER,IID_ITfInputProcessorProfiles,(void**)&p))){p->Unregister(ServiceId);p->Release();}
  ITfCategoryMgr* c=nullptr;if(SUCCEEDED(CoCreateInstance(CLSID_TF_CategoryMgr,nullptr,CLSCTX_INPROC_SERVER,IID_ITfCategoryMgr,(void**)&c))){c->UnregisterCategory(ServiceId,GUID_TFCAT_TIP_KEYBOARD,ServiceId);c->UnregisterCategory(ServiceId,GUID_TFCAT_TIPCAP_IMMERSIVESUPPORT,ServiceId);c->UnregisterCategory(ServiceId,GUID_TFCAT_TIPCAP_SYSTRAYSUPPORT,ServiceId);c->Release();}
  RegDeleteTreeW(HKEY_CURRENT_USER,key);RegDeleteTreeW(HKEY_LOCAL_MACHINE,key);CoUninitialize();return S_OK;
}
